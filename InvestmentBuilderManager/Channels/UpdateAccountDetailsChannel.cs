using System;
using InvestmentBuilderCore;
using System.Linq;
using NLog;
using InvestmentBuilderManager.Dtos;
using Transports;

namespace InvestmentBuilderManager.Channels
{
    /// <summary>
    /// Endpoint handler class. Updates the account details for the current user
    /// </summary>
    class UpdateAccountDetailsChannel : AuthorisationEndpointChannel<AccountDetailsDto, ChannelUpdater>
    {
        #region Constructor

        public UpdateAccountDetailsChannel(ServiceAggregator aggregator) :
            base("UPDATE_ACCOUNT_DETAILS_REQUEST", "UPDATE_ACCOUNT_DETAILS_RESPONSE", aggregator.AccountService)
        {
        }

        #endregion

        #region EndpointChannel overrides
        /// <summary>
        /// Handle the update account details request.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, AccountDetailsDto payload, ChannelUpdater update)
        {
            bool success = false;
            string error = "";
            try
            {
                success = GetAccountService().UpdateUserAccount(userSession, ToInternalAccount(payload));
            }
            catch(Exception ex)
            {
                logger.Error(ex);
                error = ex.Message;
            }

            return new UpdateAccountResponseDto
            {
                Status = success,
                Error = error,
                AccountNames = GetAccountService().GetAccountsForUser(userSession).ToList()
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Helper method for converting an AccountDetailsDto into an internal
        /// AccountModel.
        /// </summary>
        private AccountModel ToInternalAccount(AccountDetailsDto dto)
        {
            return new AccountModel(dto.AccountName, dto.Description, dto.ReportingCurrency,
                dto.AccountType, true, dto.Broker, dto.Members.Select(x =>
                {
                    return new AccountMember(x.Name, ToInternalAuthorizationLevel(x.Permission));
                }).ToList());
        }

        /// <summary>
        /// Helper method for converting an authorization level string into an AuthorizationLevel
        /// enum
        /// </summary>
        private AuthorizationLevel ToInternalAuthorizationLevel(string level)
        {
            AuthorizationLevel result;
            if(Enum.TryParse<AuthorizationLevel>(level, out result) == false)
            {
                throw new ArgumentException($"Invalid AuthorizationLevel: {level}");
            }
            return result;
        }

        #endregion

        #region Private Data

        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion
    }
}
