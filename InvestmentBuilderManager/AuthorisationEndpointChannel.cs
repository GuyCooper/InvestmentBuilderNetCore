using InvestmentBuilderCore;
using Transports;

namespace InvestmentBuilderManager
{
    /// <summary>
    /// Abstraction for an authorised channel
    /// </summary>
    internal abstract class AuthorisationEndpointChannel<Request, Update> : EndpointChannel<Request, Update>
        where Request : Dto, new()
        where Update : IChannelUpdater

    {
        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        public AuthorisationEndpointChannel(string requestName, string responseName, AccountService accountService) : base(requestName, responseName)
        {
            _accountService = accountService;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Helper method for getting the current user token.
        /// </summary>
        protected UserAccountToken GetCurrentUserToken(UserSession session, AccountIdentifier account = null)
        {
            return _accountService.GetUserAccountToken(session, account);
        }

        /// <summary>
        /// Returns the AccountService.
        /// </summary>
        /// <returns></returns>
        protected AccountService GetAccountService()
        {
            return _accountService;
        }

        #endregion

        #region Private Data

        private readonly AccountService _accountService;

        #endregion

    }
}
