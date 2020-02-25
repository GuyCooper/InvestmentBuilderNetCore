using System.Collections.Generic;
using System.Linq;
using InvestmentBuilderCore;
using InvestmentBuilderLib;
using Transports;

namespace InvestmentBuilderService.Channels
{
    /// <summary>
    /// Dto for returning the list of redemptions to a client.
    /// </summary>
    internal class RedemptionsDto : Dto
    {
        public List<Redemption> Redemptions { get; set; }
    }

    /// <summary>
    /// Handles the GetRedemptions request.
    /// </summary>
    class GetRedemptionsChannel : AuthorisationEndpointChannel<Dto, ChannelUpdater>
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public GetRedemptionsChannel(ServiceAggregator aggregator) : 
            base("GET_REDEMPTIONS_REQUEST", "GET_REDEMPTIONS_RESPONSE", aggregator.AccountService)
        {
            _builder = aggregator.Builder;
        }

        #endregion

        #region Protected Overrides

        /// <summary>
        /// Handle the GetRedemptions request and return the list of redemptions for this users account.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, Dto payload, ChannelUpdater updater)
        {
            var userToken = GetCurrentUserToken(userSession);
            return new RedemptionsDto
            {
                Redemptions = _builder.GetRedemptions(userToken, userSession.ValuationDate).
                                       Where(redemption => redemption.Status != RedemptionStatus.Complete).ToList()
            };
        }

        #endregion

        #region Private Data

        private readonly InvestmentBuilder _builder;

        #endregion
    }
}
