using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using InvestmentBuilderCore;
using System.Diagnostics.Contracts;

namespace InvestmentBuilderLib
{
    public sealed class InvestmentBuilder
    {
        #region Public Methods

        public InvestmentBuilder(IConfigurationSettings settings,
                                 IDataLayer dataLayer,
                                 CashAccountTransactionManager cashAccountManager,
                                 IInvestmentReportWriter reportWriter,
                                 IInvestmentRecordDataManager recordBuilder)
        {
            _settings = settings;
            _dataLayer = dataLayer;
            _userAccountData = _dataLayer.UserAccountData;
            _cashAccountData = _dataLayer.CashAccountData;
            _clientData = _dataLayer.ClientData;
            _investmentRecordData = _dataLayer.InvestmentRecordData;
            _cashAccountManager = cashAccountManager;
            _reportWriter = reportWriter;
            _recordBuilder = recordBuilder;
        }

        /// <summary>
        /// Generate asset report.
        /// </summary>
        /// <param name="accountName">club /account name</param>
        /// <param name="valuationDate">valuation date.date for this asset report</param>
        /// /// <param name="snapshotDate">date to report in investmentrecord table. should be same day as
        /// valuation date but a later time
        /// </param>
        /// <param name="bUpdate">flag to save report to db and spreadsheet.if this flag is false then this
        /// method just returns the asset report forthe specified valuation date
        /// </param>
        /// <returns></returns>
        public AssetReport BuildAssetReport(UserAccountToken userToken, DateTime valuationDate, bool bUpdate, ManualPrices manualPrices, ProgressCounter progress)
        {
            logger.Log(userToken,LogLevel.Info, string.Format("Begin BuildAssetSheet"));
            //logger.Log(LogLevel.Info,string.Format("trade file: {0}", _settings.GetTradeFile(accountName)));
            logger.Log(userToken,LogLevel.Info, string.Format("path: {0}", _settings.OutputFolder));
            logger.Log(userToken,LogLevel.Info, string.Format("datasource: {0}", _settings.DatasourceString));
            logger.Log(userToken,LogLevel.Info, string.Format("update: {0}", bUpdate));
            logger.Log(userToken,LogLevel.Info, string.Format("valuation date: {0}", valuationDate.ToShortDateString()));

            //do not allow an account to be rebuilt more than once a day as this corrupts
            //the dividend amount and the allocated units as transactions are counted for 
            //the current day. In this case just return current report
            var dtLatestValution = _clientData.GetLatestValuationDate(userToken);
            if(bUpdate && dtLatestValution.HasValue)
            {
                var latestDate = dtLatestValution.Value.Date;
                var currentDate = valuationDate.Date;
                if(currentDate <= latestDate)
                {
                    logger.Log(userToken, LogLevel.Error, $"cannot build account for report more than once a day. Returning previous report.");
                    bUpdate = false;
                }
            }
            //var factory = BuildFactory(format, path, connectionstr, valuationDate, bUpdate);
            if (userToken == null)
            {
                throw new ArgumentNullException("invalid user token");
            }

            //first, validate the cash account
            AssetReport assetReport = null;
            //    var trades = TradeLoader.GetTrades(_settings.GetTradeFile(accountName));

            if (_cashAccountManager.ValidateCashAccount(userToken, valuationDate) == false)
            {
                return assetReport;
            }

            var accountData = _userAccountData.GetUserAccountData(userToken);

            if (accountData == null)
            {
                logger.Log(userToken, LogLevel.Error, "no accounts for user", userToken.User);
                return assetReport;
            }

            var dtPreviousValuation = _clientData.GetPreviousAccountValuationDate(userToken, valuationDate);
            //first extract the cash account data

            //if this is a rerun of the currentmonth then,roll back any redemptions otherwsie
            //they will give aninaccurate bank balanceandtherefore unitprice
            if (bUpdate && dtPreviousValuation.HasValue)
            {
                _RollbackRedemptions(userToken, valuationDate, dtPreviousValuation.Value);
            }

            var cashAccountData = _cashAccountData.GetCashAccountData(userToken, valuationDate);
            //parse the trade file for any trades for this month and update the investment record
            //var trades = TradeLoader.GetTrades(tradeFile);
            var dtTradeValuationDate = valuationDate;
            var currentRecordDate = _recordBuilder.GetLatestRecordValuationDate(userToken);
            if (bUpdate)
            {
                if (currentRecordDate.HasValue)
                {
                    if (dtTradeValuationDate <= currentRecordDate)
                    {
                        logger.Log(userToken, LogLevel.Error, $"record date {dtTradeValuationDate.ToString()} must be later than the previous record valution date: {currentRecordDate.ToString()}");
                        return assetReport;
                    }
                }
                //trades now added seperately
                var emptyTrades = new Trades
                {
                    Buys = Enumerable.Empty<Stock>().ToArray(),
                    Sells = Enumerable.Empty<Stock>().ToArray(),
                    Changed = Enumerable.Empty<Stock>().ToArray()
                };

                if (_recordBuilder.UpdateInvestmentRecords(userToken, accountData, emptyTrades/*trades*/, cashAccountData, dtTradeValuationDate, manualPrices, dtPreviousValuation, progress) == false)
                {
                    //failed to update investments, return null report
                    return assetReport;
                }
            }
            else
            {
                if (currentRecordDate.HasValue)
                {
                    dtTradeValuationDate = currentRecordDate.Value;
                }
            }

            progress.Initialise("building asset report", 2);

            //now extract the latest data from the investment record
            var lstData = _recordBuilder.GetInvestmentRecords(userToken, accountData, dtTradeValuationDate, dtPreviousValuation, null, false).ToList();
            foreach (var val in lstData)
            {
                logger.Log(userToken, LogLevel.Info, string.Format("{0} : {1} : {2} : {3} : {4}", val.Name, val.SharePrice, val.NetSellingValue, val.MonthChange, val.MonthChangeRatio));
                //Console.WriteLine("{0} : {1} : {2} : {3} : {4}", val.sName, val.dSharePrice, val.dNetSellingValue, val.dMonthChange, val.dMonthChangeRatio);
            }

            progress.Increment();
            assetReport = _BuildAssetReport(
                                            userToken,
                                            valuationDate,
                                            dtPreviousValuation,
                                            accountData,
                                            lstData,
                                            cashAccountData.BankBalance,
                                            bUpdate);

            progress.Increment();

            //now process any redemptions that have occured since the previous valuation
            var updatedReport = dtPreviousValuation.HasValue ? _ProcessRedemptions(userToken, assetReport, accountData, dtPreviousValuation.Value, bUpdate) : assetReport;
            //finally, build the asset statement
            if (bUpdate == true)
            {
                _reportWriter.WriteAssetReport(updatedReport, _userAccountData.GetStartOfYearValuation(userToken, valuationDate), _settings.GetOutputPath(accountData.Identifier.GetPathName()), progress);
            }

            logger.Log(userToken, LogLevel.Info, "Report Generated, Account Builder Complete");
            return updatedReport;
        }

        /// <summary>
        /// This method returns a snapshot of the investment records using today as the current 
        /// valuation date and the most recent valuation date in the database as the previous
        /// valuation date. if they are both the same date then just return previous valuation date 
        /// </summary>
        public IEnumerable<CompanyData> GetCurrentInvestments(UserAccountToken userToken, ManualPrices manualPrices)
        {
            if (userToken == null)
            {
                throw new ArgumentNullException("invalid user token");
            }

            var accountData = _userAccountData.GetUserAccountData(userToken);

            //check this is a valid account
            if (accountData == null)
            {
                logger.Log(userToken, LogLevel.Error, "invalid account for user {0}. {1}", userToken.User, userToken.Account);
                return Enumerable.Empty<CompanyData>();
            }

            var dtPreviousValuation = _clientData.GetPreviousAccountValuationDate(userToken, DateTime.Now);
            var dtLatestUpdate = _recordBuilder.GetLatestRecordValuationDate(userToken);
            if (dtLatestUpdate.HasValue)
            {
                return _recordBuilder.GetInvestmentRecords(userToken, accountData, dtLatestUpdate.Value, dtPreviousValuation, manualPrices, true);
            }

            return Enumerable.Empty<CompanyData>();
            //return recordBuilder.GetInvestmentRecordSnapshot(userToken, accountData, manualPrices);

        }

        /// <summary>
        /// This method updates trade information. This is almost the same as build asset report because
        /// we need to update the database with the new date so any subsequent calls to get investment records
        /// will retrieve the new trades
        /// </summary>
        public bool UpdateTrades(UserAccountToken userToken, Trades trades, ManualPrices manualPrices, ProgressCounter progress, DateTime? valuationDate = null)
        {
            if (userToken == null)
            {
                throw new ArgumentNullException("invalid user token");
            }

            var accountData = _userAccountData.GetUserAccountData(userToken);

            //check this is a valid account
            if (accountData == null)
            {
                logger.Log(userToken, LogLevel.Error, "invalid account {0}", userToken.Account);
            }

            return _recordBuilder.UpdateInvestmentRecords(userToken, accountData, trades, null, valuationDate ?? DateTime.Now, manualPrices, null, progress);
        }

        /// <summary>
        /// Method redeems units for a user. checks there is sufficient funds and that it
        /// does not exceed users holding before executing 
        /// </summary>
        public bool RequestRedemption(UserAccountToken userToken, string user, double dAmount, DateTime transactionDate)
        {
            //the user parameter may be different from the user who is executing this method.For security 
            //purposes the user must be a member of the executing users account and the executing user must have
            //administrator access to the account
            userToken.AuthorizeUser(AuthorizationLevel.ADMINISTRATOR);

            logger.Log(userToken, LogLevel.Info, "redemption request from user {0} on account {1} for amount {2}",
                            user, userToken.Account, dAmount);

            //user request to redeem some units. 
            var dtPreviousValuation = _clientData.GetPreviousAccountValuationDate(userToken, transactionDate);
            if (dtPreviousValuation.HasValue == false)
            {
                //cannot redeem units if no previous valuation
                logger.Log(userToken, LogLevel.Error, "cannot redeem units as account not yet valued");
                return false;
            }

            if (_userAccountData.GetAccountMembers(userToken, dtPreviousValuation.Value).FirstOrDefault(
                        x => string.Equals(x, user)) == null)
            {
                logger.Log(userToken, LogLevel.Error, "user {0} is not a member of account {1}", user, userToken.Account);
                return false;
            }

            //The amount can be no more than the users share of the current cash balance.
            //include the cash from many sales since previous transaction
            double dBalance = _cashAccountData.GetBalanceInHand(userToken, dtPreviousValuation.Value);
            dBalance += _investmentRecordData.GetHistoricalTransactions(dtPreviousValuation.Value, transactionDate, userToken).Sells.Sum(x => x.TotalCost);

            if (dAmount > dBalance)
            {
                logger.Log(userToken, LogLevel.Error, "requested redemptiom amount more than available funds. reduce amount or sell more shares to rectify");
                return false;
            }

            var dPreviousUnitValue = _userAccountData.GetPreviousUnitValuation(userToken, dtPreviousValuation.Value);
            var memberUnits = _userAccountData.GetMemberAccountData(userToken, dtPreviousValuation.Value).Where(x => x.Key == user).Sum(x => x.Value);

            var requestedUnitRedemption = dAmount / dPreviousUnitValue;
            if (requestedUnitRedemption > memberUnits)
            {
                logger.Log(userToken, LogLevel.Error, "requested amount exceeds users holding");
                return false;
            }

            //add to redemption table
            _userAccountData.AddRedemption(userToken, user, transactionDate, dAmount);

            return true;
        }

        /// <summary>
        /// Return the list of redempdtions for the specified valuation date.
        /// </summary>
        public IEnumerable<Redemption> GetRedemptions(UserAccountToken userToken, DateTime dtValuationDate)
        {
            //first get the previous valuation date and return all the redemptions
            //since that date
            var dtPreviousValuation = _clientData.GetPreviousAccountValuationDate(userToken, dtValuationDate);
            if (dtPreviousValuation.HasValue)
            {
                return _userAccountData.GetRedemptions(userToken, dtPreviousValuation.Value).ToList();
            }
            return null;
        }

        /// <summary>
        /// Return the list of possible parameters for the specified transaction type.
        /// </summary>
        public IEnumerable<string> GetParametersForTransactionType(UserAccountToken userToken, DateTime valuationDate, string transactionType)
        {
            if (_typeProcedureLookup.ContainsKey(transactionType))
            {
                var methodInfo = _userAccountData.GetType().GetMethod(_typeProcedureLookup[transactionType]);
                if (methodInfo != null)
                {
                    return methodInfo.Invoke(_userAccountData, new object[] { userToken, valuationDate }) as IEnumerable<string>;
                }
            }
            return Enumerable.Empty<string>();

        }

        /// <summary>
        /// return the filename for the report specified by the current user token and the specfied
        /// valuation date
        /// </summary>
        public string GetInvestmentReport(UserAccountToken userToken, DateTime valuationDate)
        {
            return _reportWriter.GetReportFileName(valuationDate);
        }

        public IEnumerable<string> GetAllCurrencies()
        {
            //TODO do this properly
            return new List<string>
            {
                "GBP",
                "USD",
                "EUR",
                "JPY",
                "CHF",
                "CAD"
            };
        }

        #endregion

        #region  Private Methods

        /// <summary>
        /// Generate the asset report object using the specified values.
        /// </summary>
        private AssetReport _BuildAssetReport(
                                                UserAccountToken userToken,
                                                DateTime dtValuationDate,
                                                DateTime? dtPreviousValution,
                                                AccountModel accountData,
                                                IEnumerable<CompanyData> companyData,
                                                double dBankBalance,
                                                bool bUpdate)
        {
            logger.Log(userToken, LogLevel.Info, "building asset report...");
            AssetReport report = new AssetReport
            {
                AccountName = accountData.Identifier,
                ReportingCurrency = accountData.ReportingCurrency,
                ValuationDate = dtValuationDate.Date,
                Assets = companyData,
                BankBalance = dBankBalance
            };

            report.TotalAssetValue = companyData.Sum(c => c.NetSellingValue);
            report.MonthlyPnL = companyData.Sum(c => c.MonthChange);
            report.TotalAssets = report.BankBalance + report.TotalAssetValue;
            report.TotalLiabilities = default(double); //todo, record liabilities(if any)
            report.NetAssets = report.TotalAssets - report.TotalLiabilities;

            if (bUpdate)
            {
                report.IssuedUnits = _UpdateMembersCapitalAccount(userToken, accountData, dtPreviousValution, dtValuationDate, report.NetAssets);
            }
            else
            {
                report.IssuedUnits = _userAccountData.GetIssuedUnits(userToken, dtValuationDate);
            }

            if (report.IssuedUnits > default(double))
            {
                report.ValuePerUnit = report.NetAssets / report.IssuedUnits;
            }
            else
            {
                report.ValuePerUnit = 1d; //default unit value
            }

            //todo total assets
            //unit price
            if (bUpdate)
            {
                _userAccountData.SaveNewUnitValue(userToken, dtValuationDate, report.ValuePerUnit);
            }

            //now calculate the YTD value
            //YTD is compared against unit price from last month of previous year
            DateTime dtFrom = new DateTime(dtValuationDate.Year - 1, 12, 01);
            DateTime dtTo = new DateTime(dtValuationDate.Year - 1, 12, 31);
            var previousVals = _userAccountData.GetUnitValuationRange(userToken, dtFrom, dtTo).ToList();
            var startOfYear = previousVals.Count > 0 ? previousVals.Last() : 1.0;
            report.YearToDatePerformance = ((report.ValuePerUnit - startOfYear) / startOfYear) * 100.0;

            return report;
        }

        /// <summary>
        /// Update the members captial account table.
        /// </summary>
        private double _UpdateMembersCapitalAccount(
                                                    UserAccountToken userToken,
                                                    AccountModel accountData,
                                                    DateTime? dtPreviousValution,
                                                    DateTime dtValuationDate,
                                                    double dNetAssets)
        {
            logger.Log(userToken, LogLevel.Info, "updating members capital account...");
            //get total number of shares allocated for previous month
            //get list of all members who have made a deposit for current month
            double dResult = default(double);
            if (dtPreviousValution.HasValue)
            {
                var dPreviousUnitValue = _userAccountData.GetPreviousUnitValuation(userToken, dtPreviousValution);
                var memberAccountData = _userAccountData.GetMemberAccountData(userToken, dtPreviousValution ?? dtValuationDate).ToList();
                foreach (var member in memberAccountData)
                {
                    double dSubscription = _userAccountData.GetMemberSubscription(userToken, dtValuationDate, member.Key);
                    double dNewAmount = member.Value + (dSubscription * (1 / dPreviousUnitValue));
                    dResult += dNewAmount;
                    _userAccountData.UpdateMemberAccount(userToken, dtValuationDate, member.Key, dNewAmount);
                }
            }
            else
            {
                logger.Log(userToken, LogLevel.Info, "new account. setting issued units equal to net assets");
                //no previous valaution this is a new account, the total issued units should be the same as
                //the total netassets. this will give a unit valuation of 1.
                var members = _userAccountData.GetAccountMembers(userToken, DateTime.Today).ToList();
                double memberUnits = dNetAssets / members.Count;
                dResult = dNetAssets;
                foreach (var member in members)
                {
                    _userAccountData.UpdateMemberAccount(userToken, dtValuationDate, member, memberUnits);
                }
            }
            return dResult;
        }

        /// <summary>
        /// Process any redemptions that have occured since the previous valuation date
        /// </summary>
        private AssetReport _ProcessRedemptions(UserAccountToken userToken, AssetReport report, AccountModel accountData, DateTime previousValuation, bool bUpdate)
        {
            //now check if any redemptions have occured since the last valuation. The redemption can now
            //take place using the newly generated unit price. The redemption amount will need to be checked 
            //again to ensure the user has enough units to cover the redemption. Once complete, the cash account
            //will have to be updated with the new balance
            bool updated = false;

            var redemptions = _userAccountData.GetRedemptions(userToken, previousValuation).ToList();

            if (bUpdate == false)
            {
                report.Redemptions = redemptions;
                return report;
            }

            foreach (var redemption in redemptions)
            {
                if (redemption.Status == RedemptionStatus.Failed)
                {
                    continue;
                }

                logger.Log(userToken, LogLevel.Info, "Processing redemption for user {0}. Amount requested {1}", redemption.User, redemption.Amount);

                var memberUnits = _userAccountData.GetMemberAccountData(userToken, report.ValuationDate).Where(x => x.Key == redemption.User).Sum(x => x.Value);
                var requestedUnitRedemption = redemption.Amount / report.ValuePerUnit;
                if (requestedUnitRedemption == 0)
                {
                    //a requested redemption amount of 0 denotes a request to redeem wholeuser holding
                    requestedUnitRedemption = memberUnits;
                    //calculate the redemption amount based on the current unit valuation.round down
                    //so the club does not lose out on any rounding errors
                    redemption.UpdateAmount(Math.Floor(requestedUnitRedemption * report.ValuePerUnit));
                    logger.Log(userToken, LogLevel.Info, "redeeming full user holding for {0}. amount redeemed {1}", redemption.User, redemption.Amount);
                }
                else
                {
                    //need to check if user has enough units to cover requested redemption amount.
                    //if not enough then keep reduing the redemption amount until there is enough
                    while (requestedUnitRedemption > memberUnits)
                    {
                        redemption.UpdateAmount(redemption.Amount - 1);
                        requestedUnitRedemption = redemption.Amount / report.ValuePerUnit;
                    }

                    //logger.Log(LogLevel.Warn, "requested redemption amount exceeds users  {0} holding. reducing amount to {1}", redemption.User, redemption.Amount);
                }
                //if newuser unit count is less than 1 then round down to zero;
                double newMemberUnits = memberUnits - requestedUnitRedemption;
                if (newMemberUnits < 1)
                {
                    newMemberUnits = 0d;
                }

                //we now have both the final redemption amount and the new user unit holding
                //we need to update both the cash account and the member capital account table
                _userAccountData.UpdateMemberAccount(userToken, report.ValuationDate, redemption.User, newMemberUnits);

                //only update the cash account the redemption hasn't already been processed
                //this check is neededin case the user builds the asset report multiple times
                _cashAccountData.AddCashAccountTransaction(userToken, report.ValuationDate, report.ValuationDate, "Redemption", redemption.User,
                    redemption.Amount);

                //add in a -ve amount for the balancein handso bank balance reflects new amount
                _cashAccountData.AddCashAccountTransaction(userToken, report.ValuationDate, report.ValuationDate, "BalanceInHandCF", "BalanceInHandCF",
                    0d - redemption.Amount);

                logger.Log(userToken, LogLevel.Info, "redemption for user {0} complete. Units sold. {1}. Units remaining {2}", redemption.User,
                    requestedUnitRedemption, newMemberUnits);

                redemption.RedeemedUnits = requestedUnitRedemption;
                //update redemtpion table with the amount that was actually redeemed, the number of
                //units redeemed and the status to complete
                redemption.Status = _userAccountData.UpdateRedemption(userToken, redemption.User, redemption.TransactionDate, redemption.Amount, requestedUnitRedemption);

                updated = true;
            }

            if (updated == true)
            {
                //if there have been redemptions then update the asset report with the new balance
                //and units
                var cashAccountData = _cashAccountData.GetCashAccountData(userToken, report.ValuationDate);
                var newReport = _BuildAssetReport(userToken, report.ValuationDate, null, accountData, report.Assets,
                                         cashAccountData.BankBalance, false);
                newReport.Redemptions = redemptions;
                return newReport;
            }

            //nothing changed just return report
            return report;
        }

        //If the asset report is being rerun for the month then the rdemptions need to be rolled
        //back to reflect the correct bankbalance by adding the amounts back
        private void _RollbackRedemptions(UserAccountToken userToken, DateTime dtValuation, DateTime previousValuation)
        {
            var redemptions = _userAccountData.GetRedemptions(userToken, previousValuation).ToList();
            foreach (var redemption in redemptions)
            {
                //only needto roll back completed redemptions
                if (redemption.Status == RedemptionStatus.Complete)
                {
                    //add in a 
                    _cashAccountData.AddCashAccountTransaction(userToken, dtValuation, redemption.TransactionDate, "BalanceInHandCF", "BalanceInHandCF",
                        redemption.Amount);
                }
            }
        }

        /// <summary>
        /// Contract invariance method
        /// </summary>
        [ContractInvariantMethod]
        private void ObjectInvariantCheck()
        {
            Contract.Invariant(_settings != null);
            Contract.Invariant(_dataLayer != null);
            Contract.Invariant(_userAccountData != null);
            Contract.Invariant(_clientData != null);
            Contract.Invariant(_investmentRecordData != null);
            Contract.Invariant(_cashAccountManager != null);
            Contract.Invariant(_reportWriter != null);
            Contract.Invariant(_recordBuilder != null);
        }
        #endregion

        #region Private Data Members

        private static InvestmentBuilderLogger logger = new InvestmentBuilderLogger(LogManager.GetCurrentClassLogger());

        private readonly IConfigurationSettings _settings;
        private readonly IDataLayer _dataLayer;
        private readonly IUserAccountInterface _userAccountData;
        private readonly ICashAccountInterface _cashAccountData;
        private readonly IClientDataInterface _clientData;
        private readonly IInvestmentRecordInterface _investmentRecordData;
        private readonly CashAccountTransactionManager _cashAccountManager;
        private readonly IInvestmentReportWriter _reportWriter;
        private readonly IInvestmentRecordDataManager _recordBuilder;

        private readonly Dictionary<string, string> _typeProcedureLookup = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase)
        {
            {"Dividend", "GetActiveCompanies"},
            {"Subscription", "GetAccountMembers"}
        };

        #endregion
    }
}
