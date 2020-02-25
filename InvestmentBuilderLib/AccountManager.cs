using System;
using System.Collections.Generic;
using System.Linq;
using InvestmentBuilderCore;
using NLog;

namespace InvestmentBuilderLib
{
    /// <summary>
    /// AccountManager class. Manages account data
    /// </summary>
    public sealed class AccountManager
    {
        #region Public Methods

        /// <summary>
        /// Constructor. inject datalayer and authorization manager
        /// </summary>
        public AccountManager(IDataLayer dataLayer, IAuthorizationManager authorizationManager, IConfigurationSettings settings)
        {
            _accountData = dataLayer.UserAccountData;
            _authorizationManager = authorizationManager;
            _maximumAccountsPerUser = settings.MaxAccountsPerUser;
        }

        /// <summary>
        /// Method Account details for the specified user.
        /// </summary>
        public AccountModel GetAccountData(UserAccountToken userToken, DateTime dtValuationDate)
        {
            AccountModel data = _accountData.GetAccount(userToken);
            if (data != null)
            {
                data.AddMembers(GetAccountMembers(userToken, dtValuationDate).ToList());
            }
            return data;
        }

        /// <summary>
        /// Create an account for the specified user
        /// </summary>
        public bool CreateUserAccount(string user, AccountModel account, DateTime dtValuationDate)
        {
            //we have to create a temporary token for this user to create this account. This is because any
            //registered user is allowed to create an account.
            var token = new UserAccountToken(user, null, AuthorizationLevel.ADMINISTRATOR);
            logger.Log(token, LogLevel.Info, "creating account {0}", account.Identifier.Name);
            return _updateInvestmentAccount(token, account, dtValuationDate, true);
        }

        /// <summary>
        /// Update an account for the specified user
        /// </summary>
        public bool UpdateUserAccount(string user, AccountModel account, DateTime dtValuationDate)
        {
            var token = _authorizationManager.GetUserAccountToken(user, account.Identifier);
            if (_accountData.InvestmentAccountExists(account.Identifier) == true)
            {
                logger.Log(token, LogLevel.Info, "updating account {0}", account.Identifier.Name);
                return _updateInvestmentAccount(token, account, dtValuationDate, false);
            }

            logger.Log(token, LogLevel.Error, "account {0} does not exist", account.Identifier.Name);
            return false;

        }

        /// <summary>
        /// Method returns the member details of the specified account
        /// </summary>
        public IEnumerable<AccountMember> GetAccountMembers(UserAccountToken token, DateTime dtValuationDate)
        {
            return _accountData.GetAccountMemberDetails(token, dtValuationDate);
        }

        /// <summary>
        /// Method returns the member names of the specified account
        /// </summary>
        public IEnumerable<AccountIdentifier> GetAccountNames(string user)
        {
            return _accountData.GetAccountNames(user, true);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Implementation for creating / modifiying an account
        /// </summary>
        private bool _updateInvestmentAccount(UserAccountToken token, AccountModel account, DateTime dtValuationDate, bool create)
        {
            logger.Log(token, LogLevel.Info, "Description {0}", account.Description);
            logger.Log(token, LogLevel.Info, "Reporting Currency {0}", account.ReportingCurrency);
            logger.Log(token, LogLevel.Info, "Account Type {0}", account.Type);
            logger.Log(token, LogLevel.Info, "Enabled {0}", account.Enabled);
            logger.Log(token, LogLevel.Info, "Broker {0}", account.Broker);

            //there must be at least 1 member with administrator rights in the account
            if (_ValidateAccount(account, token) == false)
            {
                return false;
            }

            //the group must already exist otherwise the user will not be grnted permission to
            //modify it
            if(create == true)
            {
                account.Identifier.AccountId = _accountData.CreateAccount(token, account);
                token.UpdateAccount(account.Identifier);
            }
            else
            {
                _accountData.UpdateAccount(token, account);
            }

            var existingMembers = _accountData.GetAccountMembers(token, dtValuationDate).ToList();
            //GetAccountMembers(tmpToken).ToList();
            foreach (var member in existingMembers)
            {
                if (account.Members.FirstOrDefault(x => string.Equals(x.Name, member, StringComparison.InvariantCultureIgnoreCase)) == null)
                {
                    //remove this member
                    logger.Log(token, LogLevel.Info, "removing member {0} from account {1}", member, account.Identifier.Name);
                    _UpdateMemberForAccount(token, member, AuthorizationLevel.NONE, false);
                }
            }

            //now add the members
            foreach (var member in account.Members)
            {
                logger.Log(token, LogLevel.Info, "adding member {0} to account {1}", member, account.Identifier.Name);
                _UpdateMemberForAccount(token, member.Name, member.AuthLevel, true);
            }

            return true;
        }

        /// <summary>
        /// Update the account with the specified member
        /// </summary>
        private void _UpdateMemberForAccount(UserAccountToken token, string member, AuthorizationLevel level, bool bAdd)
        {
            if(bAdd == true)
            {

            }
            _accountData.UpdateMemberForAccount(token, member, level, bAdd);
        }

        /// <summary>
        /// Pre Validate the account before creating it. Checks that the account meets the
        /// minimum criteria for a valid account.
        /// </summary>
        private bool _ValidateAccount(AccountModel account, UserAccountToken token)
        {
            //account must have at least one administrator and no user can be a 
            //member of 5 accounts
            bool hasAdmin = false;

            if(account.Type  == "Personal")
            {
                //personal accounts can only have a single member who is the administrator
                account.ClearAllMembers();
                account.AddMember(token.User, AuthorizationLevel.ADMINISTRATOR);
            }

            //each account must have at least one administrator. 
            //check no user has gone beyond the max accounts per user setting
            //ensure all members are valid users
            foreach (var member in account.Members)
            {
                var userAccounts = _accountData.GetAccountNames(member.Name, false).ToList();
                if(userAccounts.Count >= _maximumAccountsPerUser)
                {
                    logger.Log(token, LogLevel.Error, "user {0} hs exceeded maximum group allowance!", member.Name);
                    return false;
                }

                if(_accountData.GetUserId(member.Name) == -1)
                {
                    logger.Log(token, LogLevel.Error, $" Invalid user: {member.Name}");
                    return false;
                }

                hasAdmin |= member.AuthLevel == AuthorizationLevel.ADMINISTRATOR;
            }
            if(hasAdmin == false)
            {
                logger.Log(token, LogLevel.Error, "account {0} must have at least one administrator", account.Identifier.Name);
            }

            return hasAdmin;
        }

        #endregion

        #region Private Data Members

        private static InvestmentBuilderLogger logger = new InvestmentBuilderLogger(LogManager.GetCurrentClassLogger());

        private IUserAccountInterface _accountData;
        private IAuthorizationManager _authorizationManager;
        private readonly int _maximumAccountsPerUser;

        #endregion

    }
}
