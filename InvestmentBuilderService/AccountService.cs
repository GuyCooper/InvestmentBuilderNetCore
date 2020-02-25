using System.Collections.Generic;
using InvestmentBuilderCore;
using System;
using InvestmentBuilderLib;
using Transports;

namespace InvestmentBuilderService
{ 
    /// <summary>
    /// AccountService class. Wrapper class for the _authroization manager and account manager
    /// </summary>
    internal class AccountService
    {
        #region Public Methods

        /// <summary>
        /// Constructor. Injects accountmanager and authorization manager
        /// </summary>
        public AccountService(AccountManager accountManager, IAuthorizationManager authorizationManager)
        {
            _authorizationManager = authorizationManager;
            _accountManager = accountManager;
        }

        /// <summary>
        /// Method returns the usertoken for the requested user.
        /// </summary>
        public UserAccountToken GetUserAccountToken(UserSession userSession, AccountIdentifier selectedAccount)
        {
            UserAccountToken token = null;
            var username = userSession.UserName;
            if (username != null)
            {
                token = _authorizationManager.SetUserAccountToken(username, selectedAccount ?? userSession.AccountName);
            }
            return token;
        }

        /// <summary>
        /// Method returns the list of configured account names for the specified user
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public IEnumerable<AccountIdentifier> GetAccountsForUser(UserSession session)
        {
            return _accountManager.GetAccountNames(session.UserName);
        }

        /// <summary>
        /// Method updates the account details for the specified user
        /// </summary>
        public bool UpdateUserAccount(UserSession userSession, AccountModel account)
        {
            return _accountManager.UpdateUserAccount(userSession.UserName, account, userSession.ValuationDate);
        }

        /// <summary>
        /// Method creates a new account for the specified user
        /// </summary>
        public bool CreateUserAccount(UserSession userSession, AccountModel account)
        {
            return _accountManager.CreateUserAccount(userSession.UserName, account, userSession.ValuationDate);
        }

        public AccountModel GetAccount(UserSession userSession, AccountIdentifier accountName)
        {
            return _accountManager.GetAccountData(GetUserAccountToken(userSession, accountName),
                                                                      userSession.ValuationDate);
        }

        /// <summary>
        /// Return a list of members of an account
        /// </summary>
        public IEnumerable<AccountMember> GetAccountMembers(UserAccountToken userToken, DateTime dtValuationDate)
        {
            return _accountManager.GetAccountMembers(userToken, dtValuationDate);
        }
        #endregion

        #region Private Data Members

        private readonly IAuthorizationManager _authorizationManager;
        private readonly AccountManager _accountManager;

        #endregion
    }
}
