using System;
using System.Collections.Generic;
using NLog;
using Transports.Session;
using Transports;

namespace Worker
{
    /// <summary>
    /// ChannelEndpointManager class. Manages all channel endpoints. Uses reflection to
    /// determine list of channels
    /// </summary>
    class ChannelEndpointManager : EndpointManager
    {
        #region Public Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public ChannelEndpointManager(IConnectionSession session) 
            : base(session)
        {
            
        }

        /// <summary>
        /// Register an endpoint channel. add it to cache
        /// </summary>
        public void RegisterChannel(IEndpointChannel channel)
        {
            if(channel != null)
            {
                if (string.IsNullOrEmpty(channel.RequestName) == false)
                {
                    if (_channels.ContainsKey(channel.RequestName) == true)
                    {
                        logger.Log(LogLevel.Error, "duplicate channel name {0}!!", channel.RequestName);
                        return;
                    }
                    GetSession().RegisterChannelListener(channel.RequestName);
                    _channels.Add(channel.RequestName, channel);
                }
            }
        }

        /// <summary>
        /// Set the session  manager.
        /// </summary>
        public void SetSessionManager(ISessionManager sessionManager)
        {
            _userSessionManager = sessionManager;
        }

        #endregion

        /// <summary>
        /// Method handles an incoming message. delegates the message to the correct
        /// endpoint
        /// </summary>
        protected async override void EndpointMessageHandler(Message message)
        {
            if (message.Type != MessageType.REQUEST)
            {
                //GetLogger().LogError(string.Format("invalid message type: {0}", message.Type));
                logger.Log(LogLevel.Error, "invalid message type");
            }
            else
            {
                IEndpointChannel channel;
                if (_channels.TryGetValue(message.Channel, out channel) == true)
                {
                    try
                    {
                        var userSession = await _userSessionManager.GetUserSession(message.SourceId);
                        if(userSession == null)
                        {
                            //unable to validate user do not continue
                            logger.Error("User validation failed.");
                        }
                        else
                        {
                            channel.ProcessMessage(GetSession(), userSession, message.Payload, message.SourceId, message.RequestId);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Log(LogLevel.Error, ex);
                    }
                }
                else
                {
                    logger.Log(LogLevel.Error, "invalid channel : {0}", message.Channel);
                }
            }
        }


        #region Private Data Members

        // List of channels
        private readonly Dictionary<string, IEndpointChannel> _channels = new Dictionary<string, IEndpointChannel>();
        private ISessionManager _userSessionManager;
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        #endregion
    }
}
