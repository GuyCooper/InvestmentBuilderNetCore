using InvestmentBuilderCore;
using InvestmentBuilderLib;
using System.Linq;
using Transports;

namespace AuthenticationService.Channels
{
    /// <summary>
    /// UpdateCurrentAccount request dto.
    /// </summary>
    internal class UpdateCurrentAccountRequestDto : Dto
    {
        public int AccountId { get; set; }
    }

    /// <summary>
    /// Class handles Updatecurrentaccount request.
    /// </summary>
    class UpdateCurrentAccountChannel : EndpointChannel<UpdateCurrentAccountRequestDto, ChannelUpdater>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public UpdateCurrentAccountChannel(AccountManager accountManager) :
            base("UPDATE_CURRENT_ACCOUNT_REQUEST", "UPDATE_CURRENT_ACCOUNT_RESPONSE")
        {
            _accountManager = accountManager;
        }

        /// <summary>
        /// Handle update current account request.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, UpdateCurrentAccountRequestDto payload, ChannelUpdater update)
        {
            var userAccounts = _accountManager.GetAccountNames(userSession.UserName);
            var account = userAccounts.FirstOrDefault(a => a.AccountId == payload.AccountId);
            var ok = account != null;
            if (ok)
            {
                userSession.AccountName = account;
            }
            
            return new ResponseDto { Status = ok, IsError = !ok, Error = !ok ? "Invalid account id" : "" };
        }

        #region Private Data

        private readonly AccountManager _accountManager;

        #endregion
    }
}
