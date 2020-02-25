using InvestmentBuilderCore;
using InvestmentBuilderLib;
using MarketDataServices;
using Transports.Utils;

namespace InvestmentBuilderService
{
    /// <summary>
    /// Class aggregates a number of injected services so they can be accessed from this class.
    /// </summary>
    internal class ServiceAggregator
    {
        #region Public Properties

        public AccountService AccountService { get; private set; }

        public CashAccountTransactionManager CashTransactionManager { get; private set; }

        public CashFlowManager CashFlowManager { get; private set; }

        public InvestmentBuilder Builder { get; private set; }

        public IConfigurationSettings Settings { get; private set; }

        public IConnectionSettings ConnectionSettings { get; private set; }

        public BrokerManager BrokerManager { get; private set; }

        public IDataLayer DataLayer { get; private set; }

        public IMarketDataSource MarketDataSource { get; private set; }
        #endregion

        #region Constructor

        /// <summary>
        /// Constructor. Inject all dependant services
        /// </summary>
        public ServiceAggregator(AccountService accountService, 
                                 CashAccountTransactionManager cashTransactionManager, 
                                 CashFlowManager cashFlowManager,
                                 InvestmentBuilder builder, 
                                 IConfigurationSettings settings, 
                                 IConnectionSettings connectionSettings, 
                                 BrokerManager brokerManager,
                                 IDataLayer dataLayer, 
                                 IMarketDataSource marketDataSource)
        {
            AccountService = accountService;
            CashTransactionManager = cashTransactionManager;
            CashFlowManager = cashFlowManager;
            Builder = builder;
            Settings = settings;
            ConnectionSettings = connectionSettings;
            BrokerManager = brokerManager;
            DataLayer = dataLayer;
            MarketDataSource = marketDataSource;
        }

        #endregion
    }
}
