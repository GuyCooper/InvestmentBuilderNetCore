using InvestmentBuilderService.Dtos;
using Transports;

namespace InvestmentBuilderService.Channels
{
    internal class GetCashFlowRequestDto : Dto
    {
        public string DateFrom { get; set; }
    }

    /// <summary>
    /// handler class for getting the cash flow details 
    /// </summary>
    internal class GetCashFlowChannel : AuthorisationEndpointChannel<GetCashFlowRequestDto, ChannelUpdater>
    {
        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public GetCashFlowChannel(ServiceAggregator aggregator) : 
            base("GET_CASH_FLOW_REQUEST", "GET_CASH_FLOW_RESPONSE", aggregator.AccountService)
        {
            _cashFlowManager = aggregator.CashFlowManager;
        }

        #endregion

        #region Protected Overrides

        /// <summary>
        /// Handles GetCashFlow request.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, GetCashFlowRequestDto payload, ChannelUpdater update)
        {
            return CashFlowModelAndParams.GenerateCashFlowModelAndParams(userSession, _cashFlowManager, payload.DateFrom);
        }

        #endregion

        #region Private Data

        private CashFlowManager _cashFlowManager;

        #endregion
    }
}
