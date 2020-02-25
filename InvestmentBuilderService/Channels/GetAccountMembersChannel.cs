using System.Collections.Generic;
using System.Linq;
using Transports;

namespace InvestmentBuilderService.Channels
{
    /// <summary>
    /// Dto for  list of account members
    /// </summary>
    internal class AccountMemberListDto : Dto
    {
        public List<string> Members { get; set; }
    }

    /// <summary>
    /// Channel handler for returning the list of members for an account.
    /// </summary>
    class GetAccountMembersChannel : AuthorisationEndpointChannel<Dto, ChannelUpdater>
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public GetAccountMembersChannel(ServiceAggregator aggregator) :
            base("GET_ACCOUNT_MEMBERS_REQUEST", "GET_ACCOUNT_MEMBERS_RESPONSE", aggregator.AccountService)
        {
        }

        #endregion

        #region EndpointChannel overrides

        /// <summary>
        /// Handle request for GetAccountMembers.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, Dto payload, ChannelUpdater updater)
        {
            var userToken = GetCurrentUserToken(userSession);
            return new AccountMemberListDto
            {
                Members = GetAccountService().
                          GetAccountMembers(userToken, userSession.ValuationDate).
                          Select(m => m.Name).ToList()
            };
        }

        #endregion
    }
}
