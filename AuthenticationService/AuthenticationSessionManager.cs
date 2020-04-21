using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Transports;

namespace AuthenticationService
{
    /// <summary>
    /// SessionManager for authentication service.
    /// </summary>
    class AuthenticationSessionManager : ISessionManager
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        public AuthenticationSessionManager(AuthenticationManager authenticationManager)
        {
            _authenticationManager =  authenticationManager;
        }

        /// <summary>
        /// Returns the usersession for the specified user
        /// </summary>
        public Task<UserSession> GetUserSession(string sessionId)
        {
            var userSession = _authenticationManager.GetUserSession(sessionId);
            if(userSession == null)
            {
                userSession = new UserSession(null, sessionId)
                {
                    IsValid = false
                };
            }
            return Task.FromResult(userSession);
        }

        /// <summary>
        /// Remove user session from service.
        /// </summary>
        public void RemoveUserSession(string sessionId)
        {
            _authenticationManager.RemoveUserSession(sessionId);
        }

        private readonly AuthenticationManager _authenticationManager;
    }

}
