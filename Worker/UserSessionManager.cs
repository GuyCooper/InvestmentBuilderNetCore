using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Middleware;
using InvestmentBuilderCore;
using NLog;
using Transports;
using Transports.Session;
using Unity;
using System.Collections.Concurrent;

namespace Worker
{
    /// <summary>
    /// this endpoint manager class handles all the authentication requests from the 
    /// middleware server. it creates a usersession if the authentiction is 
    /// successful 
    /// </summary>
    internal interface ISessionManager
    {
        Task<UserSession> GetUserSession(Middleware.Message message);
        void RemoveUserSession(string sessionId);
    }

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
            GetSession().RegisterMessageHandler(EndpointMessageHandler);
            GetSession().RegisterChannelListener("USER_SESSION_UPDATE");
        }

        //return the usersession for this session. If it returns null then
        //this session has not been authenticated.
        public Task<UserSession> GetUserSession(string sessionId)
        {
            UserSession userSession = null;
            if (_userSessions.TryGetValue(sessionId, out userSession) == true)
            {
                return Task.FromResult<UserSession>(userSession);
            }

            // retrieve the usersession from the authentication service
            TaskCompletionSource<UserSession> ts;
            if(_pendingRequests.TryGetValue(sessionId, out ts) == true)
            {
                //request already pending...
                return ts.Task;
            }

            var request = new 
            GetSession().SendMessageToChannel("GET_USER_SESSION_REQUEST", )
        }

        //remove the specifed session from the list of valid usersessions
        public void RemoveUserSession(string sessionId)
        {
            UserSession session;
            _userSessions.TryRemove(sessionId, out session);
        }

        public override void RegisterChannels(IUnityContainer container)
        {
        }

        #endregion

        #region Protected Methods

        //this method handles authentication calls from the middleware server. authenitcate
        //user against the authentication database. password must be stored as encrypted
        protected override void EndpointMessageHandler(Message message)
        {
            if (message.Command == HandlerNames.NOTIFY_CLOSE)
            {
                logger.Log(LogLevel.Info, "session closing: {0}", message.Payload);
                _userSessions.Remove(message.Payload);
                return;
            }

            if (message.Command == HandlerNames.LOGIN)
            {
                //request to authenticate a login request. authentication process could be
                //quite slow so marshall onto a separate thread and let that respond when it is ready
                Task.Factory.StartNew(() =>
                {
                    var login = MiddlewareUtils.DeserialiseObject<LoginPayload>(message.Payload);
                    var salt = _authdata.GetSalt(login.UserName);
                    var hash = SaltedHash.GenerateHash(login.Password, salt);

                    bool authenticated = _authdata.AuthenticateUser(login.UserName, hash);
                    if (authenticated == true)
                    {
                        _userAccountData.AddUser(login.UserName, login.UserName);

                        var userSession = new UserSession(login.UserName, message.SourceId);
                        var accounts = _accountManager.GetAccountNames(login.UserName).ToList();
                        var defaultAccount = accounts.FirstOrDefault();
                        if(defaultAccount != null)
                        {
                            userSession.AccountName = defaultAccount;
                        }
                        _userSessions.Add(message.SourceId, userSession);
                    }
                    GetSession().SendAuthenticationResult(authenticated, authenticated ? "authentication succeded" : "authentication failed", message.RequestId);

                });
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
