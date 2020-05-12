using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using InvestmentBuilderCore;
using System.Diagnostics.Contracts;

namespace PerformanceBuilderLib
{
    /// <summary>
    /// Public interface for generating the perfomrance statistics for an account
    /// </summary>
    public sealed class PerformanceBuilder 
    {
        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        public PerformanceBuilder(IConfigurationSettings settings, IDataLayer dataLayer,
                                IInvestmentReportWriter reportWriter)
        {
            _dataLayer = dataLayer;
            _settings = settings;
            _reportWriter = reportWriter;
        }

        /// <summary>
        /// Entry point for getting performance statistics for an account.
        /// </summary>
        public IList<IndexedRangeData> Run(UserAccountToken userToken, DateTime dtValuation, ProgressCounter progress)
        {
            Contract.Requires(userToken != null);
            Contract.Ensures(Contract.Result<IList<IndexedRangeData>>() != null);

            logger.Log(LogLevel.Info, "starting performance builder...");
            logger.Log(LogLevel.Info, "output path: {0}", _settings.GetOutputPath(userToken.Account.GetPathName()));
            logger.Log(LogLevel.Info, "valuation date {0}", dtValuation);

            var ladderBuilder = new PerformanceLaddersBuilder(_settings, _dataLayer);
            var allLadders = ladderBuilder.BuildPerformanceLadders(userToken, dtValuation, progress).ToList();

            //now insert the individual company company
            allLadders.InsertRange(0, ladderBuilder.BuildCompanyPerformanceLadders(userToken, progress));

            //now build the company dividend ladders
            //allLadders.Insert(0, ladderBuilder.BuildAccountDividendPerformanceLadder(userToken, progress));
           
            //and finally the company dividend yield ladders
            allLadders.Insert(0, ladderBuilder.BuildAccountDividendYieldPerformanceLadder(userToken, progress));

            logger.Log(LogLevel.Info, "data ladders building complete...");
            //now persist it to the spreadsheet, TODO, make configurable, allow persist to pdf
            _reportWriter.WritePerformanceData(allLadders, _settings.GetOutputPath(userToken.Account.GetPathName()), dtValuation, progress);

            logger.Log(LogLevel.Info, "performance chartbuilder complete");
            return allLadders;
        }

        #endregion

        #region Private Data

        private static Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IConfigurationSettings _settings;
        private readonly IDataLayer _dataLayer;
        private readonly IInvestmentReportWriter _reportWriter;

        #endregion
    }


}

