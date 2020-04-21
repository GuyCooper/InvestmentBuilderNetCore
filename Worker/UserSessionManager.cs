using System.Threading.Tasks;
using NLog;
using Transports;
using Transports.Session;
using System.Collections.Concurrent;
using System;

namespace Worker
{
    /// <summary>
    /// Manager usersessions. Maintains a lazy cache of usersessions. If user session not present
    /// for a user, will authenticate user against authentication server.
    /// </summary>
    internal class UserSessionManager : EndpointManager, ISessionManager
    {
        #region Public Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserSessionManager(IConnectionSession session)
            : base(session)
        {
            GetSession().RegisterChannelListener("GET_USER_SESSION_RESPONSE");
        }

        //return the usersession for this session. If it returns null then
        //this session has not been authenticated.
        public Task<UserSession> GetUserSession(string sessionId)
        {
            UserSession userSession = null;
            if (_userSessions.TryGetValue(sessionId, out userSession) == true)
            {
                var task = Task.FromResult<UserSession>(userSession);
                task.ConfigureAwait(false);
                return task;
            }

            // retrieve the usersession from the authentication service
            TaskCompletionSource<UserSession> ts;
            if(_pendingRequests.TryGetValue(sessionId, out ts) == false)
            {
                //Send a request to the authentication server to return the usersession
                //for this user
                ts = new TaskCompletionSource<UserSession>();
                ts.Task.ConfigureAwait(false);
                _pendingRequests.TryAdd(sessionId, ts);
                var request = new UserSessionRequestDto
                {
                    SessionId = sessionId
                };

                GetSession().SendMessageToChannel("GET_USER_SESSION_REQUEST", TransportUtils.SerialiseObjectToString(request), "", "", null);
            }

            return ts.Task;
        }

        //remove the specifed session from the list of valid usersessions
        public void RemoveUserSession(string sessionId)
        {
            UserSession session;
            _userSessions.TryRemove(sessionId, out session);
        }

        #endregion

        #region Protected Methods

        //this method handles authentication calls from the middleware server. authenitcate
        //user against the authentication database. password must be stored as encrypted
        protected override void EndpointMessageHandler(Message message)
        {
            try
            {
                var response = TransportUtils.DeserialiseObject<UserSessionResponseDto>(message.Payload);
                TaskCompletionSource<UserSession> ts;
                if (_pendingRequests.TryRemove(response.SessionID, out ts) == false)
                {
                    logger.Error($"Unknown session id from usersession response: {response.SessionID}");
                }
                else
                {
                    UserSession userSession = null;
                    if(response.Success == false)
                    {
                        logger.Error($"Unable to validate user for session id {response.SessionID}!");
                    }
                    else
                    {
                        logger.Info($"Validated user for session id {response.SessionID}");
                        userSession = response.Session;
                        _userSessions.TryAdd(response.SessionID, userSession);
                    }
                    ts.TrySetResult(userSession);
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        #endregion

        #region Private Data Members

        private readonly ConcurrentDictionary<string, UserSession> _userSessions = new ConcurrentDictionary<string, UserSession>();

        private readonly ConcurrentDictionary<string, TaskCompletionSource<UserSession>> _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<UserSession>>();

        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion
    }
}
