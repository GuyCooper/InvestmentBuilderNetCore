using Transports.Session;
using Transports;

namespace Worker
{
    /// <summary>
    /// Abstract EndpointManager class. Defines a base class cfor an endpoint. An endpoint manager
    /// registrs with the connection session and multiplexes messages onto the correct endpoint.
    /// 
    /// </summary>
    internal abstract class EndpointManager
    {
        #region Public Methods

        /// <summary>
        /// Constructor. Inject the Connection session.
        /// </summary>
        public EndpointManager(IConnectionSession session)
        {
            _session = session;
            _session.RegisterMessageHandler(EndpointMessageHandler);
        }

        /// <summary>
        /// Get the underlying session.
        /// </summary>
        public IConnectionSession GetSession()
        {
            return _session;
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Handle a message from the session.
        /// </summary>
        protected abstract void EndpointMessageHandler(Message message);

        #endregion

        #region Private Data

        /// <summary>
        /// Session connection instance.
        /// </summary>
        private IConnectionSession _session;

        #endregion
    }
}
