using System;
using System.Collections.Generic;
using System.Text;
using Transports;

namespace AuthenticationService.Channels
{
    /// <summary>
    /// Handles get user session requests. 
    /// </summary>
    class GetUserSessionChannel : EndpointChannel<UserSessionRequestDto, ChannelUpdater>
    {

        /// <summary>
        ///Constructor
        /// </summary>
        public GetUserSessionChannel(AuthenticationManager authManager) : base("GET_USER_SESSION_REQUEST", "GET_USER_SESSION_RESPONSE")
        {
            _authManager = authManager;
        }

        /// <summary>
        /// Handles request to get a usersession
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, UserSessionRequestDto payload, ChannelUpdater updater)
        {
            var session = _authManager.GetUserSession(payload.SessionId);
            return new UserSessionResponseDto
            {
                SessionID = payload.SessionId,
                Success = session != null,
                Session = session
            };
        }

        #region Private Data

        private readonly AuthenticationManager _authManager;

        #endregion

    }
}
