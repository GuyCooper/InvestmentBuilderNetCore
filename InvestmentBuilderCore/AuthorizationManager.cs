using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
namespace InvestmentBuilderCore
{
    /// <summary>
    /// Enum specifies the possibe levels of authorisation on an account
    /// </summary>
    public enum AuthorizationLevel
    {
        NONE = 0, //No permssions
        READ, //read only permissions
        UPDATE, //read and update permssions
        ADMINISTRATOR //read, update and special administrator privilleges such as adding / removing users
    }

    /// <summary>
    /// UserAccountToken class defines a user authorization level for an account
    /// </summary>
    public class UserAccountToken
    {
        #region Public Properties

        public string User { get; private set; }

        public AccountIdentifier Account { get; private set; }

        public bool IsAdministrator { get { return _authorization == AuthorizationLevel.ADMINISTRATOR; } } 

        /// <summary>
        /// Returns true if this user has an invalid account. False if it is valid.
        /// </summary>
        public bool HasInvalidAccount()
        {
            return (Account == null) || (string.IsNullOrEmpty(Account.Name) || Account.AccountId == 0) ;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        public UserAccountToken(string user, AccountIdentifier account,AuthorizationLevel authorization)
        {
            User = user;
            Account = account;
            _authorization = authorization;
        }

        /// <summary>
        /// Authorizes the user specified by this token against the authorisation level.
        /// </summary>
        public void AuthorizeUser(AuthorizationLevel level)
        {
            if(_authorization < level)
            {
                throw new UnauthorizedAccessException("User does not have permission for this action on this account");
            }
        }

        /// <summary>
        /// Updates the account for this token
        /// </summary>
        public void UpdateAccount(AccountIdentifier account)
        {
            if(account != null && string.IsNullOrEmpty(account.Name) == false)
            {
                Account = account;
            }
        }

        #endregion

        #region Contract Invariance Methods

        [ContractInvariantMethod]
        protected void ObjectInvariantMethod()
        {
            Contract.Invariant(string.IsNullOrEmpty(User) == false);
            Contract.Invariant(Account != null);
            Contract.Invariant(string.IsNullOrEmpty(Account.Name) == false);
        }

        #endregion

        #region Private Members Data

        private AuthorizationLevel _authorization;

        #endregion
    }
    /// <summary>
    /// interface defines an authorization manager. user can have any of the levels in
    /// authorization level. lowest is none highest is administrator. each level inherits
    /// the permissions of all the previous ones.i.e. adminstrator inherits all other 
    /// permissions
    /// </summary>
    public interface IAuthorizationManager
    {
        UserAccountToken GetUserAccountToken(string user, AccountIdentifier account);
        UserAccountToken SetUserAccountToken(string user, AccountIdentifier account);
        UserAccountToken GetCurrentTokenForUser(string user);
    }

    public abstract class AuthorizationManager : IAuthorizationManager
    {
        #region Public Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public AuthorizationManager()
        {
            _userTokenlookup = new Dictionary<string, UserAccountToken>();
        }

        /// <summary>
        /// Factory method returns a UserToken for a user
        /// </summary>
        public UserAccountToken GetUserAccountToken(string user, AccountIdentifier account)
        {
            if(IsGlobalAdministrator(user) == true)
            {
                return new UserAccountToken(user, account, AuthorizationLevel.ADMINISTRATOR);
            }

            if(account != null && string.IsNullOrEmpty(account.Name) == false)
            {
                return new UserAccountToken(user, account,
                    GetUserAuthorizationLevel(user, account));
            }

            return new UserAccountToken(user, new AccountIdentifier { Name=""}, AuthorizationLevel.NONE);
        }

        /// <summary>
        /// each user is only ever allowed a single useraccount token at any time. 
        /// NOTE: This method must be thread safe
        /// </summary>
        public UserAccountToken SetUserAccountToken(string user, AccountIdentifier account)
        {
            UserAccountToken existingToken;
            if (IsGlobalAdministrator(user) || account?.Name == null)
            {
                _userTokenlookup.TryGetValue(user, out existingToken);
                if(existingToken != null)
                {
                    existingToken.UpdateAccount(account);
                }
            }
            else
                existingToken = _userTokenlookup.Values.FirstOrDefault(t => t.Account == account && t.User == user);

            if(existingToken == null)
            {
                existingToken = GetUserAccountToken(user, account);
                try
                {
                    _userTokenlookup.Add(user, existingToken);
                }
                catch(Exception)
                { /*insertion may have been preempted by another thread */ }
            }
            return existingToken;
        }

        /// <summary>
        /// Returns the current token for a user if there is one.
        /// </summary>
        public UserAccountToken GetCurrentTokenForUser(string user)
        {
            UserAccountToken token;
            if(_userTokenlookup.TryGetValue(user, out token) == true)
            {
                return token;
            }
            return null;
        }

        #endregion

        #region Protected Abstract Methods

        protected abstract AuthorizationLevel GetUserAuthorizationLevel(string user, AccountIdentifier account);

        protected abstract bool IsGlobalAdministrator(string user);

        #endregion

        #region Private Data Members

        private Dictionary<string, UserAccountToken> _userTokenlookup;

        #endregion

    }
}
