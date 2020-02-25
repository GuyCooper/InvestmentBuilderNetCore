using InvestmentBuilderCore;
using InvestmentBuilderLib;
using InvestmentBuilderManager.Translators;
using Transports;

namespace InvestmentBuilderManager.Channels
{
    /// <summary>
    /// handler class for retreiving the investment summary
    /// </summary>
    class GetInvestmentSummaryChannel : AuthorisationEndpointChannel<Dto, ChannelUpdater>
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public GetInvestmentSummaryChannel(ServiceAggregator aggregator) :
            base("GET_INVESTMENT_SUMMARY_REQUEST", "GET_INVESTMENT_SUMMARY_RESPONSE", aggregator.AccountService)
        {
            _clientData = aggregator.DataLayer.ClientData;
            _builder = aggregator.Builder;
        }

        #endregion

        #region Protected Overrrides

        /// <summary>
        /// Method called when a request for an investnent summary is received.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, Dto payload, ChannelUpdater update)
        {
            var userToken = GetCurrentUserToken(userSession);
            var dtValuation = _clientData.GetLatestValuationDate(userToken);
            //var dtPrevious = _clientData.GetPreviousAccountValuationDate(userToken, userSession.ValuationDate);
            if (dtValuation.HasValue)
            {
                return _builder.BuildAssetReport(userToken, dtValuation.Value, false, null, null).ToInvestmentSummaryModel();
            }
            else
            {
                return new Dtos.InvestmentSummaryModel
                {
                    AccountName = userToken.Account,
                    ValuationDate = userSession.ValuationDate,
                    ValuePerUnit = "1"
                };
            }
        }

        #endregion

        #region Private Data

        private IClientDataInterface _clientData;
        private InvestmentBuilder _builder;

        #endregion

    }
}
