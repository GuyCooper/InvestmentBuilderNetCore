using System;
using System.Collections.Generic;
using System.Linq;
using InvestmentBuilderCore;
using Transports;

namespace InvestmentBuilderService.Channels
{
    /// <summary>
    /// Response dto for RecentValutionDates list.
    /// </summary>
    internal class RecentValuationDatesListDto : Dto
    {
        public IEnumerable<string> Dates { get; set; }
    }

    /// <summary>
    /// handler class for retreiving list of recent valuation dates
    /// </summary>
    internal class GetRecentValutationDatesChannel : AuthorisationEndpointChannel<Dto, ChannelUpdater>
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public GetRecentValutationDatesChannel(ServiceAggregator aggregator) 
            : base("GET_RECENT_VALUATION_DATES_REQUEST", "GET_RECENT_VALUATION_DATES_RESPONSE", aggregator.AccountService)
        {
            _clientData = aggregator.DataLayer.ClientData;
        }

        #endregion

        #region Protected Overrides

        protected override Dto HandleEndpointRequest(UserSession userSession, Dto payload, ChannelUpdater update)
        {
            var userToken = GetCurrentUserToken(userSession);
            return new RecentValuationDatesListDto
            {
                Dates = _clientData.GetRecentValuationDates(userToken, DateTime.Now).Select(x =>
                                         x.ToShortDateString()).ToList()
            };
        }

        #endregion

        #region Private Data

        private readonly IClientDataInterface _clientData;

        #endregion
    }
}
