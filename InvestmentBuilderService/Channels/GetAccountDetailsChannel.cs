using System;
using InvestmentBuilderCore;
using System.Linq;
using Transports;
using InvestmentBuilderService.Dtos;

namespace InvestmentBuilderService.Channels
{
    /// <summary>
    /// GetAccountDetailsRequestDto class. Dto for GetAccountDetailsRequest channel.
    /// </summary>
    internal class GetAccountDetailsRequestDto : Dto
    {
        public AccountIdentifier AccountName { get; set; }
    }

    /// <summary>
    /// Channel Handler class handles requests for account details for a specific account
    /// </summary>
    class GetAccountDetailsChannel : AuthorisationEndpointChannel<GetAccountDetailsRequestDto, ChannelUpdater>
    {
        #region Constructor

        public GetAccountDetailsChannel(ServiceAggregator aggregator) : 
            base("GET_ACCOUNT_DETAILS_REQUEST", "GET_ACCOUNT_DETAILS_RESPONSE", aggregator.AccountService)
        {
        }

        #endregion

        #region EndpointChannel overrides

        /// <summary>
        /// Handle request for GetAccountDetails.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, GetAccountDetailsRequestDto payload, ChannelUpdater updater)
        {
            return ToExternalAccountDetails(GetAccountService().GetAccount(userSession, payload.AccountName));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Method translates an internal AccountModel into a dto
        /// </summary>
        private AccountDetailsDto ToExternalAccountDetails(AccountModel account)
        {
            return new AccountDetailsDto
            {
                AccountName = account.Identifier,
                AccountType = account.Type,
                Broker = account.Broker,
                Description = account.Description,
                Enabled = true,
                ReportingCurrency = account.ReportingCurrency,
                Members = account.Members.Select(ToExternalAccountMember).ToList()
            };
        }

        /// <summary>
        /// Method translates an internal AccountMember object into a dto
        /// </summary>
        private AccountMemberDto ToExternalAccountMember(AccountMember member)
        {
            return new AccountMemberDto
            {
                Name = member.Name,
                Permission = member.AuthLevel.ToString()
            };
        }

        #endregion
    }
}
