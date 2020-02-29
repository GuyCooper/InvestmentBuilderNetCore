using System;
using System.Collections.Generic;
using System.Linq;
using InvestmentBuilderCore;
using Middleware;
using NLog;
using Unity;
using Transports.Session;
using Transports;
using System.Threading.Tasks;

namespace Worker
{
    /// <summary>
    /// ChannelEndpointManager class. Manages all channel endpoints. Uses reflection to
    /// determine list of channels
    /// </summary>
    class ChannelEndpointManager : EndpointManager
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public ChannelEndpointManager(IConnectionSession session, ISessionManager userSessionManager) 
            : base(session)
        {
            _userSessionManager = userSessionManager;
        }

        /// <summary>
        /// Use reflection to construct the list of Endpoint channels defined in this
        /// assembly.
        /// </summary>
        public override void RegisterChannels(IUnityContainer container, string service )
        {
            //register and resolve all concrete classes in this module that are derived 
            //from EndpointChannel
            //first find a list of all the endpoint channels types
            var channelTypes = new List<Type>();
            var assembley = LoadServiceAssembley(service);
            foreach ( var t in assembley.GetTypes())
            {
                if((t.IsAbstract == false)&&(IsEndpointChannelClass(t).ToList().Count > 0))
                {
                    channelTypes.Add(t);
                }
            }

            //now register each endpoint channel
            foreach(var channel in channelTypes)
            {
                ContainerManager.RegisterType(channel, true);
            }

            //now resolve (instantiate) each endpointchannel and register the 
            //channels with the middleware layer
            foreach (var channel in channelTypes)
            {
                var endpoint = ContainerManager.ResolveValueOnContainer<IEndpointChannel>(channel, container);
                if(endpoint != null)
                {
                    if (string.IsNullOrEmpty(endpoint.RequestName) == false)
                    {
                        GetSession().RegisterChannelListener(endpoint.RequestName);
                    }

                    RegisterChannel(endpoint);
                }
            }
        }

        /// <summary>
        /// Register an endpoint channel. add it to cache
        /// </summary>
        /// <param name="channel"></param>
        private void RegisterChannel(IEndpointChannel channel)
        {
            if(channel != null)
            {
                if(_channels.ContainsKey(channel.RequestName) == true)
                {
                    logger.Log(LogLevel.Error, "duplicate channel name {0}!!", channel.RequestName);
                    return;
                }
                _channels.Add(channel.RequestName, channel);
            }
        }

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

        #region Private Methods

        /// <summary>
        /// Helper method for determining if an object is of type IEndpointChannel
        /// </summary>
        private IEnumerable<Type> IsEndpointChannelClass(Type objType)
        {
            return
            from interfaceType in objType.GetInterfaces()
            where interfaceType == typeof(IEndpointChannel)
            select interfaceType;
        }

        /// <summary>
        /// Attempt to load the specified service assembley
        /// </summary>
        private System.Reflection.Assembly LoadServiceAssembley(string service)
        {
            string filename = $"{service}.dll";
            return System.Reflection.Assembly.LoadFrom(filename);
        }

        #endregion

        #region Private Data Members

        // List of channels
        private readonly Dictionary<string, IEndpointChannel> _channels = new Dictionary<string, IEndpointChannel>();
        private readonly ISessionManager _userSessionManager;
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        #endregion
    }
}
