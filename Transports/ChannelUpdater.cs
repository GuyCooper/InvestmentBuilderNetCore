using System.Timers;
using System;
using Transports.Session;
using Middleware;

namespace Transports
{
    /// <summary>
    /// Interface defines an updater. this is an object that can send updates to a channel
    /// </summary>
    public interface IChannelUpdater
    {
        /// <summary>
        /// send an update to the channel
        /// </summary>
        /// <param name="payload"></param>
        void SendUpdate(Dto payload);
        /// <summary>
        /// flag is set when the updater has completed (if ever)
        /// </summary>
        bool Completed { get; }
    }

    /// <summary>
    /// base class for an ChannelUpdater. sends updates to a channel
    /// </summary>
    public abstract class ChannelUpdater : IChannelUpdater
    {
        #region Pubic Methods
        public ChannelUpdater(IConnectionSession session, string channel, string sourceId, string requestId)
        {
            _channel = channel;
            _session = session;
            _sourceId = sourceId;
            _requestId = requestId;
        }
        /// <summary>
        /// Send an update to the channel
        /// </summary>
        public void SendUpdate(Dto payload)
        {
            if(payload.GetType() == typeof(BinaryDto))
            {
                _session.SendMessageToChannel(_channel, null, _sourceId, _requestId, ((BinaryDto)payload).Payload);
            }
            else
            {
                _session.SendMessageToChannel(_channel, MiddlewareUtils.SerialiseObjectToString(payload), _sourceId, _requestId, null);
            }
        }

        public bool Completed { get; protected set; }

        #endregion

        #region Private Data Members
        private readonly IConnectionSession _session;
        private readonly string _sourceId;
        private readonly string _channel;
        private readonly string _requestId;
        #endregion
    }
    /// <summary>
    /// this is an abstract class for updating a channel. it contains a timer that can 
    /// be configured to fire at intervals/ Concrete implementations can override the
    /// elapsed callback
    /// </summary>
    public abstract class TimerUpdater : ChannelUpdater,  IDisposable
    {
        public TimerUpdater(IConnectionSession session, string channel, string sourceId, string requestId, int intervalMS) 
            : base(session,channel, sourceId, requestId)
        {
            _timer = new Timer(intervalMS); //build report timer will update every intervalMS
            _timer.Elapsed += (o, s) =>
            {
                if(OnUpdate() == false)
                {
                    _timer.Stop();
                    Completed = true;
                }
            };
            _timer.AutoReset = true;
            _timer.Enabled = false;
        }

        #region Protected Members
        protected abstract bool OnUpdate();

        /// <summary>
        /// start the timer
        /// </summary>
        public void Start()
        {
            _timer.Start();
        }

        #endregion
        #region IDisposable
        public void Dispose()
        {
            if(IsDisposed == true)
            {
                return;
            }
            _timer.Dispose();
            IsDisposed = true;
        }

        private bool IsDisposed;
        #endregion

        #region Private Data Members
        private readonly Timer _timer;
        #endregion
    }
}
