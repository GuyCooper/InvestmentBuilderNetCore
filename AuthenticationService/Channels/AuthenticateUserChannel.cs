using AuthenticationService.Dtos;
using Transports;

namespace AuthenticationService.Channels
{
    public class LoginResponseDto : Dto
    {
        public bool Success { get; set; }
        public string SessionID { get; set; }
    }

    /// <summary>
    /// Handles user authentication requests.
    /// </summary>
    class AuthenticateUserChannel : EndpointChannel<LoginRequestDto, ChannelUpdater>
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public AuthenticateUserChannel(AuthenticationManager authManager) : base("AUTHENTICATE_USER_REQUEST", "AUTHENTICATE_USER_RESPONSE")
        {
            _authManager = authManager;
        }
            
        #endregion

        #region EndpointChannel overrides

        /// <summary>
        /// Handle user authentication request.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, LoginRequestDto payload, ChannelUpdater updater)
        {
            var success = _authManager.AuthenticateUser(userSession.SessionId, payload);
            return new LoginResponseDto { 
                                            Success = success, 
                                            SessionID = userSession.SessionId
                                        };
        }

        #endregion

        #region Private Data

        private readonly AuthenticationManager _authManager;

        #endregion
    }
}
