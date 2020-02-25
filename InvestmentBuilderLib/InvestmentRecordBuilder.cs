using System;
using System.Collections.Generic;
using System.Linq;
using MarketDataServices;
using NLog;
using InvestmentBuilderCore;
using System.Diagnostics.Contracts;

namespace InvestmentBuilderLib
{
    internal enum GetPriceResult
    {
        NoPrice,
        FoundPrice,
        UseOverride
    }

    [ContractClass(typeof(InvestmentRecordDataManagerContract))]
    public interface IInvestmentRecordDataManager
    {
        bool UpdateInvestmentRecords(UserAccountToken userToken, AccountModel account, Trades trades, CashAccountData cashData, DateTime valuationDate, ManualPrices manualPrices, DateTime? dtPreviousValuation, ProgressCounter progress);
        IEnumerable<CompanyData> GetInvestmentRecords(UserAccountToken userToken, AccountModel account, DateTime dtValuationDate, DateTime? dtPreviousValuationDate, ManualPrices manualPrices, bool bSnapshot);
        IEnumerable<CompanyData> GetInvestmentRecordSnapshot(UserAccountToken userToken, AccountModel account, ManualPrices manualPrices);
        DateTime? GetLatestRecordValuationDate(UserAccountToken userToken);
    }
        
    //class generates the current investment record for each stock for the current month. sets and sold stocks to inactive
    //and adds any new stocks to a new sheet
    public class InvestmentRecordBuilder : IInvestmentRecordDataManager
    {
        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        public InvestmentRecordBuilder(IMarketDataService marketDataService, IDataLayer datalayer, BrokerManager brokerManager)
        {
            _investmentRecordData = datalayer.InvestmentRecordData;
            _marketDataService = marketDataService;
            _brokerManager = brokerManager;
            Log = LogManager.GetLogger(GetType().FullName);
        }

        /// <summary>
        /// refactored method for updating investment record
        /// this method rolls all the investments in the investment record
        /// table and updates the trades
        /// </summary>
        public bool UpdateInvestmentRecords(UserAccountToken userToken, AccountModel account, Trades trades, CashAccountData cashData, DateTime valuationDate, ManualPrices manualPrices, DateTime? dtPreviousValuation, ProgressCounter progress)
        {
            Log.Log(LogLevel.Info, "building investment records...");
            //Console.WriteLine("building investment records...");

            var aggregatedBuys = trades != null ? trades.Buys.AggregateStocks().ToList() : Enumerable.Empty<Stock>();
            var aggregatedSells = trades != null ? trades.Sells.AggregateStocks().ToList() : Enumerable.Empty<Stock>();

            var previousUpdate = _investmentRecordData.GetLatestRecordInvestmentValuationDate(userToken);
            //
            var enInvestments = _GetInvestments(userToken, previousUpdate.HasValue ? previousUpdate.Value : valuationDate).ToList();

            progress.Initialise("updating investment records", enInvestments.Count + 1);

            bool bValidationFailed = false;
            //validate each investment before we proceed with updating
            foreach (var investment in enInvestments)
            {
                double dPrice = 0d;
                var companyInfo = investment.CompanyData;
                var priceResult = _tryGetClosingPrice(companyInfo.Symbol, companyInfo.Exchange, investment.Name, companyInfo.Currency, account.ReportingCurrency, companyInfo.ScalingFactor, manualPrices, out dPrice);
                if (companyInfo != null && priceResult == GetPriceResult.FoundPrice)
                {
                    //validation, compare price with last known price for this investment, if price change> 90%
                    //flag this as an error
                    double dMargin = investment.Price - (investment.Price / 10d);
                    if (Math.Abs(investment.Price - dPrice) > dMargin)
                    {
                        Log.Error("invalid price for {0}. excessive price movement. price = {1}: previous = {2}",
                                    investment.Name, dPrice, investment.Price);
                        bValidationFailed = true;
                        break;
                    }
                }
            }

            if (bValidationFailed == true)
            {
                return false;
            }

            foreach (var investment in enInvestments)
            {
                if (previousUpdate.HasValue == false)
                {
                    throw new ApplicationException(string.Format("BuildInvestmentRecords: no previous valuation date for {0}. please investigate!!!", account));
                }

                var dtPrevious = previousUpdate.Value;
                var company = investment.Name;
                //Console.WriteLine("updating company {0}", company);
                //now copy the last row into a new row and update
                investment.UpdateRow(valuationDate, dtPrevious);
                var sellTrade = aggregatedSells.FirstOrDefault(x => company.Equals(x.Name, StringComparison.CurrentCultureIgnoreCase));
                if (sellTrade != null)
                {
                    //trade sold, set to inactive. todo do this properly
                    Log.Log(LogLevel.Info, string.Format("company {0} sold {1} shares", company, sellTrade.Quantity));
                    //Console.WriteLine("company {0} sold", company);
                    //investment.DeactivateInvestment();
                    investment.SellShares(valuationDate, sellTrade);
                }

                //update share number if it has changed
                var trade = trades != null ? trades.Changed != null ? trades.Changed.FirstOrDefault(x => company.Equals(x.Name, StringComparison.CurrentCultureIgnoreCase)) : null : null;
                if (trade != null)
                {
                    Log.Log(LogLevel.Info, string.Format("company share number changed {0}", company));
                    //Console.WriteLine("company share number changed {0}", company);
                    investment.ChangeShareHolding(valuationDate, trade.Quantity);
                }

                //now update this stock if more shres have been brought
                trade = aggregatedBuys.FirstOrDefault(x => company.Equals(x.Name, StringComparison.CurrentCultureIgnoreCase));
                if (trade != null)
                {
                    investment.AddNewShares(valuationDate, trade);
                    //remove the trade from the trade buys
                    aggregatedBuys = aggregatedBuys.Where(x => x != trade).ToList();
                }

                //update any dividend. do not add dividend if just rerunning current valuation because
                //the divididend field is accumulative
                UpdateDividend(valuationDate, dtPreviousValuation, investment, cashData);
                //if we have the correct comapy data then calculate the closing price forthis company
                double dPrice = 0d;
                var companyInfo = investment.CompanyData;
                var priceResult = _tryGetClosingPrice(companyInfo.Symbol, companyInfo.Exchange, investment.Name, companyInfo.Currency, account.ReportingCurrency, companyInfo.ScalingFactor, manualPrices, out dPrice);
                if (companyInfo != null && priceResult != GetPriceResult.NoPrice)
                {
                    investment.UpdateClosingPrice(valuationDate, dPrice);
                }

                progress.Increment();
            }

            foreach (var newTrade in aggregatedBuys)
            {
                Log.Log(LogLevel.Info, string.Format("adding new trade {0}", newTrade.Name));
                //Console.WriteLine("adding new trade {0}", newTrade.Name);
                //new trade to add to investment record
                double dClosing;
                _tryGetClosingPrice(newTrade.Symbol,
                                    newTrade.Exchange,
                                    newTrade.Name,
                                    newTrade.Currency,
                                    account.ReportingCurrency,
                                    newTrade.ScalingFactor,
                                    manualPrices,
                                    out dClosing);

                //_marketDataService.TryGetClosingPrice(newTrade.Symbol, newTrade.Exchange, newTrade.Name, newTrade.Currency, account.Currency, newTrade.ScalingFactor, out dClosing);
                _CreateNewInvestment(userToken, newTrade, valuationDate, dClosing);
            }

            //add any transactions to the transaction history table
            if (trades != null)
            {
                _investmentRecordData.AddTradeTransactions(trades.Buys, TradeType.BUY, userToken, valuationDate);
                _investmentRecordData.AddTradeTransactions(trades.Sells, TradeType.SELL, userToken, valuationDate);
                _investmentRecordData.AddTradeTransactions(trades.Changed, TradeType.MODIFY, userToken, valuationDate);
            }

            if (progress != null)
            {
                progress.IncrementCounter();
            }

            return true;
        }

        /// <summary>
        /// this method returns the current investment records from persistence layer
        /// </summary>
        public IEnumerable<CompanyData> GetInvestmentRecords(UserAccountToken userToken, AccountModel account, DateTime dtValuationDate, DateTime? dtPreviousValuationDate, ManualPrices manualPrices, bool bSnapshot)
        {
            //dtPreviousValuationDate parameteris the previous valuation date.we need to extract the previous record
            //valuation date from this to retrieve the correct previous record data from the database
            DateTime? dtNewPreviousRecordValuationDate = null;
            if (dtPreviousValuationDate.HasValue)
            {
                if (_investmentRecordData.IsExistingRecordValuationDate(userToken, dtPreviousValuationDate.Value))
                {
                    dtNewPreviousRecordValuationDate = dtPreviousValuationDate;
                }
                else
                {
                    dtNewPreviousRecordValuationDate = _investmentRecordData.GetPreviousRecordInvestmentValuationDate(userToken, dtPreviousValuationDate.Value);
                }
            }

            return _GetInvestmentRecordsImpl(userToken, account, dtValuationDate, dtNewPreviousRecordValuationDate, bSnapshot, manualPrices);
        }

        /// <summary>
        /// This method builds a snapshot of the current investment records updating the current prices
        /// but NOT persisting to the database. the report is generated from the last known valuation date
        /// as this snapshot does notyet exist in the database
        /// </summary>
        public IEnumerable<CompanyData> GetInvestmentRecordSnapshot(UserAccountToken userToken, AccountModel account, ManualPrices manualPrices)
        {
            DateTime? dtPreviousValuationDate = _investmentRecordData.GetLatestRecordInvestmentValuationDate(userToken);
            if (dtPreviousValuationDate.HasValue) //
                return _GetInvestmentRecordsImpl(userToken, account, dtPreviousValuationDate.Value, dtPreviousValuationDate, true, manualPrices);

            //if there is no last known valuation date for this account then just return an empty report
            return new List<CompanyData>();
        }

        public DateTime? GetLatestRecordValuationDate(UserAccountToken userToken)
        {
            return _investmentRecordData.GetLatestRecordInvestmentValuationDate(userToken);
        }

        #endregion

        #region Private Methods

        private IEnumerable<IInvestmentRecordData> _GetInvestments(UserAccountToken userToken, DateTime dtValuationDate)
        {
            var companies = _investmentRecordData.GetInvestments(userToken, dtValuationDate).ToList(); 
            return companies.Select(c => new InvestmentData(userToken, c.Key, c.Value, _investmentRecordData));
        }

        private void _CreateNewInvestment(UserAccountToken userToken, Stock newTrade, DateTime valuationDate, double dClosing)
        {
            _investmentRecordData.CreateNewInvestment(userToken, newTrade.Name, newTrade.Symbol, newTrade.Currency,
                                                      newTrade.Quantity, newTrade.ScalingFactor, newTrade.TotalCost, dClosing, newTrade.Exchange,
                                                      valuationDate);
        }

        private IEnumerable<CompanyData> _GetCompanyDataImpl(UserAccountToken userToken, AccountModel account, DateTime dtValuationDate, bool bUpdatePrice, ManualPrices manualPrices )
        {
            var investments = _investmentRecordData.GetInvestmentRecordData(userToken, dtValuationDate).ToList();
            foreach (var investment in investments)
            {
                if (bUpdatePrice == true)
                {
                    double dClosing;
                    var companyData = _investmentRecordData.GetInvestmentDetails(investment.Name);
                    var priceResult = _tryGetClosingPrice(companyData.Symbol,
                                           companyData.Exchange,
                                           investment.Name,
                                           companyData.Currency,
                                           account.ReportingCurrency,
                                           companyData.ScalingFactor,
                                           manualPrices,
                                           out dClosing);
                    if(priceResult != GetPriceResult.NoPrice)
                    {
                        investment.SharePrice = dClosing;
                        if (priceResult == GetPriceResult.UseOverride)
                        {
                            investment.ManualPrice = dClosing.ToString("#0.000");
                        }
                    }
                }

                investment.NetSellingValue = _brokerManager.GetNetSellingValue(account.Broker, investment.Quantity, investment.SharePrice);
            }
            return investments;
        }

        /// <summary>
        /// Update the monthly data stats for a single investment.
        /// </summary>
        private void _updateMonthlyData(CompanyData currentData, CompanyData previousData, Trades trades)
        {
            //difference between current selling value and previous selling value, must also
            //include any transactions that have been completed this month
            double dMonthChange = currentData.NetSellingValue;
            if (trades != null)
            {
                dMonthChange -= trades.Buys.Where(x => x.Name == currentData.Name).Sum(x => x.TotalCost);
                dMonthChange += trades.Sells.Where(x => x.Name == currentData.Name).Sum(x => x.TotalCost);
            }
            dMonthChange -= previousData.NetSellingValue;
            currentData.MonthChange = dMonthChange;
            currentData.MonthChangeRatio = currentData.MonthChange / previousData.NetSellingValue * 100;
        }

        private void _DeactivateInvestment(string investment, UserAccountToken userToken)
        {
            _investmentRecordData.DeactivateInvestment(userToken, investment);
        }

        private GetPriceResult _tryGetClosingPrice(string symbol, string exchange, string name, string currency, string accountCurrency, double scalingFactor, ManualPrices manualPrices, out double dPrice)
        {
            double? dManualPrice = null;
            if ((manualPrices != null) && (manualPrices.ContainsKey(name) == true))
            {
                dManualPrice = manualPrices[name];
            }
            if (_marketDataService.TryGetClosingPrice(symbol, exchange, null, name, currency, accountCurrency, dManualPrice, out dPrice) == true)
            {
                return dManualPrice == null ? GetPriceResult.FoundPrice : GetPriceResult.UseOverride;
            }
            return GetPriceResult.NoPrice;
        }


        /// <summary>
        /// Update dividend for an investment.
        /// </summary>
        private void UpdateDividend(DateTime valuationDate, DateTime? previousValulation, IInvestmentRecordData investment,  CashAccountData cashData )
        {
            //update any dividend. do not add dividend if just rerunning current valuation because
            //the divididend field is accumulative
            if((previousValulation.HasValue == true) && (valuationDate <= previousValulation.Value))
            {
                return;
            }

            double dDividend;
            if (cashData != null && cashData.Dividends.TryGetValue(investment.Name, out dDividend))
            {
                investment.UpdateDividend(valuationDate, dDividend);
            }
        }

        /// <summary>
        /// Returns the valuations for each investment in the account.
        /// </summary>
        private IEnumerable<CompanyData> _GetInvestmentRecordsImpl(UserAccountToken userToken, AccountModel account, DateTime dtValuationDate, DateTime? dtPreviousValuationDate, bool bSnapshot, ManualPrices manualPrices)
        {
            var lstCurrentData = _GetCompanyDataImpl(userToken, account, dtValuationDate, bSnapshot, manualPrices).ToList();
            var lstPreviousData = dtPreviousValuationDate.HasValue ? _GetCompanyDataImpl(userToken, account, dtPreviousValuationDate.Value, false, null).ToList() : new List<CompanyData>();
            //get the list of all trade transactions between these two dates as this affects the monthly change  
            var trades = dtPreviousValuationDate.HasValue ? _investmentRecordData.GetHistoricalTransactions(dtPreviousValuationDate.Value.AddDays(1), dtValuationDate, userToken) : null;
            
            foreach (var company in lstCurrentData)
            {
                var previousData = lstPreviousData.Find(c => c.Name == company.Name);
                if (previousData != null)
                {
                    _updateMonthlyData(company, previousData, trades);
                }

                company.ProfitLoss = company.NetSellingValue - company.TotalCost;
                company.TotalReturn = ((company.ProfitLoss + company.Dividend) / company.TotalCost) * 100;

                if (bSnapshot == false && company.Quantity == 0)
                {
                    _DeactivateInvestment(company.Name, userToken);
                }
            }
            return lstCurrentData;
        }

        #endregion

        #region Private Data

        protected Logger Log { get; private set; }
        private readonly IMarketDataService _marketDataService;
        private readonly IInvestmentRecordInterface _investmentRecordData;
        private readonly BrokerManager _brokerManager;

        #endregion

    }
}
