using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using InvestmentBuilderCore;
using Newtonsoft.Json;
using System.Globalization;

namespace PerformanceBuilderLib
{
    using DATE_POINT = Tuple<DateTime?, string>;

    internal class RawHistoricalData
    {
        public string date { get; set; }
        public string price { get; set; }
    }

    /// <summary>
    /// this class builds all the different perfomance ladders that will be included in the
    /// report.
    /// </summary>
    internal class PerformanceLaddersBuilder
    {
        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        public PerformanceLaddersBuilder(IConfigurationSettings settings, IDataLayer dataLayer)
        {
            _historicalDataReader = dataLayer.HistoricalData;
            _investmentRecordData = dataLayer.InvestmentRecordData;
            _userAccountData = dataLayer.UserAccountData;
            _settings = settings;
        }

        /// <summary>
        /// Builds the performance ladders that allows the performance of the users portfolio
        /// to be compared to a configured list of global indices. Returns a performance ladder for the
        /// users portfolio and all the configured indices.
        /// </summary>
        public IEnumerable<IndexedRangeData> BuildPerformanceLadders(UserAccountToken userToken, DateTime dtValuation, ProgressCounter progress)
        {
            logger.Log(LogLevel.Info, "building portfolio performance ladders");

            var historicalData = _historicalDataReader.GetHistoricalAccountData(userToken);
            var firstRecord = historicalData != null ? historicalData.FirstOrDefault() : null;
            if (firstRecord != null)
            {
                var performanceRangeList = _DetermineIndexRanges(firstRecord.Date);
                progress.Initialise("building performance ladders", performanceRangeList.Count);
                //now retrieve all historical data ladders from the market data source 
                //var allLadders = new List<IndexedRangeData>();
                foreach (var point in performanceRangeList)
                {
                    logger.Log(LogLevel.Info, "building data ladder for {0}", point.Item2);

                    //Console.WriteLine("building data ladder for {0}", perfPoint.Item2);
                    var indexladder = _BuildIndexLadders(userToken, point.Item1, historicalData, userToken.Account);
                    var result = new IndexedRangeData
                    {
                        MinValue = 0.8,
                        IsHistorical = true,
                        Name = point.Item2,
                        Data = indexladder,
                        Title = "Account Performance"
                    };

                    progress.Increment();

                    yield return result;
                }
            }
            logger.Log(LogLevel.Info, "Finished building portfolio performance ladders");
        }

        /// <summary>
        /// Method returns a peformance ladder for each investment in the users portfolio. Allows user
        /// to compare relative performance of each investment.
        /// </summary>
        public IEnumerable<IndexedRangeData> BuildCompanyPerformanceLadders(UserAccountToken userToken, ProgressCounter progress)
        {
            var dtValuation = _investmentRecordData.GetLatestRecordInvestmentValuationDate(userToken);
            if (dtValuation.HasValue == true)
            {
                logger.Log(LogLevel.Info, "building company performance data ladders");
                var companies = _userAccountData.GetActiveCompanies(userToken, dtValuation.Value).ToList();
                var investmentRecords = _investmentRecordData.GetFullInvestmentRecordData(userToken).OrderBy(x => x.ValuationDate).ToList();
                var firstRecord = investmentRecords.FirstOrDefault();
                if (firstRecord != null)
                {
                    var performanceRangeList = _DetermineIndexRanges(firstRecord.ValuationDate);
                    foreach (var point in performanceRangeList)
                    {
                        yield return new IndexedRangeData
                        {
                            MinValue = 0.0,
                            IsHistorical = true,
                            Name = point.Item2,
                            Data = _BuildCompanyIndexData(companies, investmentRecords, point, dtValuation.Value, progress),
                            Title = string.Format("Individual company performance (units)")
                        };
                    }
                }

                logger.Log(LogLevel.Info, "Finished building company performance data ladders");
            }
        }

        /// <summary>
        /// Method returns a list containing the total dividend for each investment in the users 
        /// portfolio.
        /// 
        /// </summary>
        public IndexedRangeData BuildAccountDividendPerformanceLadder(UserAccountToken userToken, ProgressCounter progress)
        {
            var dtValuation = _investmentRecordData.GetLatestRecordInvestmentValuationDate(userToken);
            if (dtValuation.HasValue == false)
                return null;

            logger.Log(LogLevel.Info, "building company dividend data ladders");

            var indexData = new IndexedRangeData
            {
                MinValue = 0,
                IsHistorical = false,
                KeyName = "Company",
                Name = "Dividends",
                Data = _BuildAccountDividendIndexData(userToken, dtValuation.Value, progress),
                Title = "Individual Company dividends received"
            };

            logger.Log(LogLevel.Info, "Finished building company dividend data ladders");

            return indexData;
        }

        /// <summary>
        /// Method returns a list containing the average dividend yield for each investment in the users 
        /// portfolio. It also returns an average volume weighted yield for the portfolio as a whole.
        /// 
        /// </summary>
        public IndexedRangeData BuildAccountDividendYieldPerformanceLadder(UserAccountToken userToken, ProgressCounter progress)
        {
            var dtValuation = _investmentRecordData.GetLatestRecordInvestmentValuationDate(userToken);
            if (dtValuation.HasValue == false)
                return null;

            logger.Log(LogLevel.Info, "building company dividend yield data ladders");

            var indexData =  new IndexedRangeData
            {
                MinValue = 0,
                IsHistorical = false,
                KeyName = "Company",
                Name = "Average Yield",
                Data = _BuildAccountDividendYieldIndexData(userToken, dtValuation.Value, progress),
                Title = "Individual Company average yield (%)"
            };

            logger.Log(LogLevel.Info, "Finished building company dividend yield data ladders");

            return indexData;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Method returns the year and month of the specified date
        /// </summary>
        private DateTime MonthlyDate(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1);
        }

        /// <summary>
        /// Method outputs historical data to the logger
        /// </summary>
        private void DumpData(string title, IEnumerable<HistoricalData> enData)
        {
            //Console.WriteLine(title);
            logger.Log(LogLevel.Info, title);
            foreach (var elem in enData)
            {
                logger.Log(LogLevel.Info, "{0} : {1}", elem.Date, elem.Price);
                //Console.WriteLine("{0} : {1}", elem.Date, elem.Price);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private bool _GetIndexRangeForYear(DateTime dtStartDate, int years, string description, IList<Tuple<DateTime?, string>> listIndexes)
        {
            var dtYear = DateTime.Today.AddYears(years);
            if (dtStartDate < dtYear)
            {
                //add a one year chart. note, we add one month onto the calculated month for the date otherwise
                //we will display an extra month in the index
                listIndexes.Add(new Tuple<DateTime?, string>(dtYear.AddMonths(1), description));
                return true;
            }
            return false;
        }
        /// <summary>
        /// using the historical data for the account,determine index ranges we
        /// want to include in the report.for example. if the account has data
        /// for the last 3 1/2 years, we would want to have 1, 3 and all time
        /// charts
        /// </summary>
        /// <returns></returns>
        private IList<DATE_POINT> _DetermineIndexRanges(DateTime? startDate)
        {
            //historical data must be in ascending chronological order (oldest data first)
            var result = new List<DATE_POINT>();
            if (startDate.HasValue)
            {
                //add all time range
                result.Add(new DATE_POINT(null, "All Time"));
                int previousYear = -1;
                string description = "1 year";
                //other indexes displayed will be for 1, 3 and 5 year, then every 5th
                //year thereon. (i.e. 10, 15, 20 etc.)
                while (_GetIndexRangeForYear(startDate.Value, previousYear, description, result))
                {
                    if (previousYear == -1)
                        previousYear = -3;
                    else if (previousYear == -3)
                        previousYear = -5;
                    else
                        previousYear -= 5;

                    description = string.Format("{0} year", Math.Abs(previousYear));
                }
            }
            return result;
        }

        /// <summary>
        /// method filters out any older prices than start date,and rebases on first date
        /// </summary>
        private IEnumerable<HistoricalData> RebaseDataList(IEnumerable<HistoricalData> dataList, DateTime? dtStartDate)
        {
            DateTime? dtPrevious = null;
            var resultList = new List<HistoricalData>();

            var tmpList = dtStartDate.HasValue ? dataList.Where(x => x.Date >= dtStartDate).OrderBy(x => x.Date).ToList() :
                dataList.OrderBy(x => x.Date).ToList();

            if(tmpList.Count == 0)
            {
                return resultList;
            }

            //scan through the list, removing any duplicate entries for the same month
            foreach (var item in tmpList)
            {
                //where multiple records exist for a single month, just use the the last one
                if (item.Date.HasValue == true &&
                    dtPrevious.HasValue == true &&
                    dtPrevious.Value.Year == item.Date.Value.Year &&
                    dtPrevious.Value.Month == item.Date.Value.Month)
                {
                    //logger.Log(LogLevel.Info, "RebaseDataList: removing duplicate date entry : {0}", dtPrevious.Value.Date.ToShortDateString());
                    resultList.RemoveAt(resultList.Count - 1);
                }

                dtPrevious = item.Date;
                resultList.Add(new HistoricalData
                (
                    date: item.Date,
                    price: item.Price
                ));
            }

            //now we have filtered out the prices we can rebase them...
            var firstPrice = resultList.First().Price;
            foreach(var entry in resultList)
            {
                entry.RebasePrice(firstPrice);
            }

            return resultList;
        }

        /// <summary>
        /// Method deserialises the stored historical data from the database and converts it into
        /// an HistoricalData list.
        /// </summary>
        private IEnumerable<HistoricalData> ParseHistoricalData(string data, DateTime? dtFrom)
        {
            var rawData = JsonConvert.DeserializeObject<IList<RawHistoricalData>>(data);
            foreach (var item in rawData)
            {
                var date = DateTime.ParseExact(item.date, "M/d/yyyy", CultureInfo.InvariantCulture);
                if (dtFrom.HasValue == false || isDateGreaterThanOrEqual(dtFrom.Value, date))
                {
                    if (string.IsNullOrEmpty(item.price) == false)
                    {
                        yield return new HistoricalData
                        (
                            date: date,
                            price: double.Parse(item.price)
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Compare a date to a ref date. return truw if the date is later or the same
        /// based on the year and month
        /// </summary>
        private bool isDateGreaterThanOrEqual(DateTime dtRef, DateTime dt2)
        {
            if ((dtRef.Year < dt2.Year) ||
                ((dtRef.Year == dt2.Year) && (dtRef.Month <= dt2.Month)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// method retrieves the historical price information for all the configured indexes for all required
        /// date ranges. Data is then rebased for each date range to allow easy determination of relative performance
        /// </summary>
        private IList<IndexData> _BuildIndexLadders(UserAccountToken userToken, DateTime? startDate, IEnumerable<HistoricalData> historicalData, AccountIdentifier account)
        {
            //first get club history
            //var clubData = _GetClubData().OrderBy(x => x.Date).ToList();
            var result = new List<IndexData>();

            var clubData = historicalData.OrderBy(x => x.Date).ToList();
            DumpData("club data", clubData);
            var rebasedClubData = RebaseDataList(clubData, startDate).ToList();

            if(rebasedClubData.Count == 0)
            {
                return result;
            }

            DateTime dtFirstDate = rebasedClubData.First().Date.Value;

            result.Add(new IndexData
            {
                Name = account.Name,
                StartDate = dtFirstDate,
                Data = rebasedClubData
            });

            int clubItemCount = rebasedClubData.Count;
            if(clubItemCount == 0)
            {
                return result;
            }
            //all comparison indexes must have the same item count as the club index
            _settings.ComparisonIndexes.ToList().ForEach(index =>
            {
                var indexedData = ParseHistoricalData(_historicalDataReader.GetIndexHistoricalData(userToken, index.Symbol), dtFirstDate).ToList();
                if (indexedData != null)
                {
                    var rebasedIndexedData = RebaseDataList(indexedData, null).ToList();
                    
                    //it could be the case that the index data has not been updated for a while and not
                    //all available to the current date. In this case just pad it out...
                    while(rebasedClubData.Count > rebasedIndexedData.Count)
                    {
                        rebasedIndexedData.Add(rebasedIndexedData.Last());
                    }

                    result.Add(new IndexData
                    {
                        Name = index.Name,
                        StartDate = dtFirstDate,
                        Data = rebasedIndexedData
                    });
                }
            });

            return result;
        }

        /// <summary>
        /// Method retuns the total return for an individual company
        /// </summary>
        private double _GetPerformanceAmount(CompanyData company)
        {
            company.NetSellingValue = company.Quantity * company.SharePrice;
            return (company.NetSellingValue + company.Dividend) / company.TotalCost;
        }

        /// <summary>
        /// Method determines if an index curve starts later than the specified date or not.
        /// </summary>
        private bool _IsPointLaterThan(IndexData index, DateTime dtDate)
        {
            if ((index.StartDate.Value.Year > dtDate.Year) ||
                ((index.StartDate.Value.Year == dtDate.Year) && (index.StartDate.Value.Month > dtDate.Month)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Method builds the performance curves for a list of investments 
        /// </summary>
        private IList<IndexData> _BuildCompanyIndexData(IList<string> companies,
                                                        IList<CompanyData> investmentRecords,
                                                        DATE_POINT startPoint,
                                                        DateTime valuationDate,
                                                        ProgressCounter progress)
        {
            var title = string.Format("building company performance ladders for {0}", startPoint.Item2);
            progress.Initialise(title, companies.Count);
            var indexes = new List<IndexData>();
            foreach (var company in companies)
            {
                DateTime dtPrevious = new DateTime();
                var dataList = new List<HistoricalData>();
                var index = new IndexData { Name = company };
                var companyRecords = investmentRecords.Where(x => {
                    bool match = x.Name == company;
                    if(startPoint.Item1.HasValue == true)
                    {
                        return match && (x.ValuationDate >= startPoint.Item1.Value);
                    }
                    return match;
                }).ToList();
                foreach (var investment in companyRecords)
                {
                    //where multiple records exist for a single month, just the the last one
                    if(dtPrevious.Year == investment.ValuationDate.Year && 
                        dtPrevious.Month == investment.ValuationDate.Month)
                    {
                        //logger.Log(LogLevel.Info, "BuildCompanyIndexData: removing duplicate date entry : {0}", dtPrevious.Date.ToShortDateString());
                        dataList.RemoveAt(dataList.Count - 1);
                    }

                    dtPrevious = investment.ValuationDate;
                    dataList.Add(new HistoricalData
                    (
                        date: investment.ValuationDate.Date,
                        price: _GetPerformanceAmount(investment)
                    ));
                }
                if(dataList.Count > 0)
                {
                    index.StartDate = dataList.First().Date;
                    index.Data = dataList;
                    indexes.Add(index);
                }
                progress.Increment();
            }
            //now get the oldest company index
            //and pad all other indexes with unit data points
            if (indexes.Count == 0)
                return indexes;

            var oldest = indexes.OrderBy(x => x.StartDate).First();
            foreach(var index in indexes)
            {
                if(_IsPointLaterThan(index, oldest.StartDate.Value) == true)
                {
                    foreach (var point in oldest.Data)
                    {
                        if (_IsPointLaterThan(index, point.Date.Value) == true)
                        {
                            index.Data.Insert(0, new HistoricalData
                            (
                                date: point.Date,
                                price: 1d
                            ));
                        }
                        else
                            break;
                    }
                    index.StartDate = oldest.StartDate;
                }
            }

            foreach (var index in indexes)
            {
                index.Data = RebaseDataList(index.Data, index.StartDate).ToList();
            }

            return indexes;
        }

        /// <summary>
        /// Method builds the dividend amounts curve for all investments in the portfolio. 
        /// </summary>
        private IList<IndexData> _BuildAccountDividendIndexData(UserAccountToken userToken, DateTime valuationDate, ProgressCounter progress)
        {
            progress.Initialise("Building Company income data ladder", 1);
            var investmentRecords = _investmentRecordData.GetInvestmentRecordData(userToken, valuationDate).ToList();
            progress.Increment();
            var indexes = new List<IndexData>();
            var index = new List<HistoricalData>();
            foreach (var record in investmentRecords)
            {
                index.Add(new HistoricalData
                (
                    key: record.Name,
                    price: record.Dividend
                ));
            }

            indexes.Add(new IndexData
            {
                Name = "Dividends",
                Data = index
            });

            return indexes;
        }

        /// <summary>
        /// Method builds the dividend yield items for all investments in the portfolio.
        /// </summary>
        private IList<IndexData> _BuildAccountDividendYieldIndexData(UserAccountToken userToken, DateTime valuationDate, ProgressCounter progress)
        {
            progress.Initialise("building company yield data ladder", 3);
            var allRecords = _investmentRecordData.GetFullInvestmentRecordData(userToken).OrderBy(x => x.ValuationDate).ToList();
            progress.Increment();
            var investmentRecords = _investmentRecordData.GetInvestmentRecordData(userToken, valuationDate);
            progress.Increment();
            var indexes = new List<IndexData>();
            var index = new List<HistoricalData>();
            double dVWAP = 0d;
            double dTotalCost = 0d;
            foreach (var record in investmentRecords)
            {
                var firstRecord = allRecords.FirstOrDefault(x => x.Name == record.Name);
                var span = record.ValuationDate - firstRecord.LastBrought;
                double dYears = span.TotalDays / 365d;
                double yield = (record.Dividend / record.TotalCost / dYears) * 100;
                index.Add(new HistoricalData
                (
                    key: record.Name,
                    price: yield
                ));
                dVWAP += (record.TotalCost * yield);
                dTotalCost += record.TotalCost;
            }

            progress.Increment();
            //now add the average yield for all current investments
            // cannot do average yield for whole account because we may not 
            //have all the information available
            //must do VWA yield to get accurate figure for account yield
            double average = dVWAP / dTotalCost;

            index.Add(new HistoricalData
            (
                key: userToken.Account.Name,
                price: average
            ));

            indexes.Add(new IndexData
            {
                Name = "Average Yield",
                Data = index
            });

            return indexes;
        }

        #endregion

        #region Private Methods

        private static Logger logger = LogManager.GetCurrentClassLogger();
        private IConfigurationSettings _settings;
        private IHistoricalDataReader _historicalDataReader;
        private IInvestmentRecordInterface _investmentRecordData;
        private IUserAccountInterface _userAccountData;

        #endregion
    }
}
