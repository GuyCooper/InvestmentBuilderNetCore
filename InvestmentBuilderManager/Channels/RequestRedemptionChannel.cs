using InvestmentBuilderLib;
using System;
using Transports;

namespace InvestmentBuilderManager.Channels
{
    /// <summary>
    /// Request redemption dto.
    /// </summary>
    internal class RequestRedemptionDto : Dto
    {
        /// <summary>
        /// The name of the user making the redemption request.
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// The amount in cash the user is requesting to redeem.
        /// </summary>
        public double Amount { get; set; }
        /// <summary>
        /// The transaction date of this request.
        /// </summary>
        public string TransactionDate { get; set; }
    }

    /// <summary>
    /// Dto defines the response from a request redemption request.
    /// </summary>
    internal class RequestRedemptionResponseDto : Dto
    {
        public bool Success { get; set; }
    }

    /// <summary>
    /// Handle the Request Redemption request.
    /// </summary>
    class RequestRedemptionChannel : AuthorisationEndpointChannel<RequestRedemptionDto, ChannelUpdater>
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public RequestRedemptionChannel(ServiceAggregator aggregator) : 
            base("REQUEST_REDEMPTION_REQUEST", "REQUEST_REDEMPTION_RESPONSE", aggregator.AccountService)
        {
            _builder = aggregator.Builder;
        }

        #endregion

        /// <summary>
        /// Handle the request redemption request. Returns true if the request succeded otherwise returns
        /// false. Do not give the reason for failure as this could be security vunerability.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, RequestRedemptionDto payload, ChannelUpdater updater)
        {
            var userToken = GetCurrentUserToken(userSession);

            bool success = _builder.RequestRedemption(userToken,
                                                      payload.UserName,
                                                      payload.Amount,
                                                      DateTime.Parse(payload.TransactionDate));

            return new RequestRedemptionResponseDto
            {
                Success = success
            };
        }

        #region Private Data
        
        private readonly InvestmentBuilder _builder;

        #endregion
    }
}
