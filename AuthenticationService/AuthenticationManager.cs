﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Middleware;
using InvestmentBuilderCore;
using NLog;
using Transports;
using InvestmentBuilderLib;
using System;
using AuthenticationService.Dtos;

namespace AuthenticationService
{
    /// <summary>
    /// this endpoint manager class handles all the authentication requests from the 
    /// middleware server. it creates a usersession if the authentiction is 
    /// successful 
    /// </summary>

    internal class AuthenticationManager 
    {
        #region Public Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public AuthenticationManager(IAuthDataLayer authtdata, AccountManager accountManager, IUserAccountInterface userAccountData)
        {
            _authdata = authtdata;
            _accountManager = accountManager;
            _userAccountData = userAccountData;
        }

        //return the usersession for this session. If it returns null then
        //this session has not been authenticated.
        public UserSession GetUserSession(string sessionId)
        {
            return GetUserSessionFromId(sessionId);
        }

        //remove the specifed session from the list of valid usersessions
        public void RemoveUserSession(string sessionId)
        {
            _userSessions.Remove(sessionId);
        }

        /// <summary>
        /// Update the current account for the specified user
        /// </summary>
        public void UpdateCurrentAccount(string sessionId, string accountName)
        {
            var userSession = GetUserSessionFromId(sessionId);
            if(userSession != null)
            {
                var accountId = _accountManager.GetAccountNames(userSession.UserName).FirstOrDefault(i => i.Name == accountName);
                if(accountId != null)
                {
                    userSession.AccountName = accountId;
                }
            }
        }

        /// <summary>
        /// Update the valuation date for the specified session.
        /// </summary>
        public void UpdateValuationDate(string sessionId, DateTime dtValuationDate)
        {
            var userSession = GetUserSessionFromId(sessionId);
            if (userSession != null)
            {
                userSession.ValuationDate = dtValuationDate;
            }
        }

        #endregion

        #region Protected Methods

        //this method handles authentication calls from the middleware server. authenitcate
        //user against the authentication database. password must be stored as encrypted
        public bool AuthenticateUser(string sessionId, LoginRequestDto login)
        {
            //request to authenticate a login request. authentication process could be
            //quite slow so marshall onto a separate thread and let that respond when it is ready
            //var login = MiddlewareUtils.DeserialiseObject<LoginPayload>(message.Payload);
            var salt = _authdata.GetSalt(login.UserName);
            var hash = SaltedHash.GenerateHash(login.Password, salt);

            bool authenticated = _authdata.AuthenticateUser(login.UserName, hash);
            if (authenticated == true)
            {
                _userAccountData.AddUser(login.UserName, login.UserName);

                UserSession session = new UserSession(login.UserName, sessionId);
                var accounts = _accountManager.GetAccountNames(login.UserName).ToList();
                var defaultAccount = accounts.FirstOrDefault();
                if(defaultAccount != null)
                {
                    session.AccountName = defaultAccount;
                }
                _userSessions.Add(sessionId, session);
            }

            return authenticated;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Return the user session for specified session 
        /// </summary>
        private UserSession GetUserSessionFromId(string sessionId)
        {
            UserSession userSession = null;
            if (_userSessions.TryGetValue(sessionId, out userSession) == false)
            {
                logger.Log(LogLevel.Error, "unknown user for session: {0} ", sessionId);
            }
            return userSession;
        }

        #endregion

        #region Private Data Members

        private readonly Dictionary<string, UserSession> _userSessions = new Dictionary<string, UserSession>();
        private IAuthDataLayer _authdata;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private AccountManager _accountManager;
        private readonly IUserAccountInterface _userAccountData;

        #endregion
    }
}
