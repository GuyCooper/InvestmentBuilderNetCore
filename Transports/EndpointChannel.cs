using System.Collections.Generic;
using InvestmentBuilderCore;
using NLog;
using System;
using System.Threading.Tasks;
using Transports.Session;
using Middleware;
using Transports.Utils;

namespace Transports
{
    //base class for all Dtos
    public class Dto
    {
        public string Name { get; private set; }
        public bool IsError { get; set; }
        public string Error { get; set; }
        public Dto()
        {
            Name = this.GetType().Name;
        }
    }

    /// <summary>
    /// A Dto that contains a binary payload
    /// </summary>
    public class BinaryDto : Dto
    {
        public Byte[] Payload { get; set; }
    }

    //base class for reponse Dtos
    public class ResponseDto : Dto
    {
        public bool Status { get; set; }
    }

    /// <summary>
    /// Interface for endpoint channels. An endpoint channel can manage both a request and response channel
    /// for the same topic. 
    /// </summary>
    public interface IEndpointChannel
    {
        string RequestName { get;}
        string ResponseName { get;}
        void ProcessMessage(IConnectionSession session, UserSession userSession,  string payload, string sourceId,  string requestId);
    }

    /// <summary>
    /// Base class for an endpoint channel. These classes are instantiated using the
    /// Unity dependency injection framework. require a channel request name and optionally
    /// a channel response name. Concrete classes define the request and response channel
    /// names
    /// </summary>
    public abstract class EndpointChannel<Request, Update> : IEndpointChannel 
        where Request : Dto, new()
        where Update : IChannelUpdater
    {
        #region Public Properties

        public string RequestName { get; private set; }
        public string ResponseName { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        protected EndpointChannel(string requestName, string responseName)
        {
            RequestName = requestName;
            ResponseName = responseName;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Process an incoming request on this endpoint
        /// </summary>
        public void ProcessMessage(IConnectionSession session, UserSession userSession, string payload, string sourceId, string requestId)
        {
            var requestPayload = ConvertToRequestPayload(payload);
            var updater = GetUpdater(session, userSession, sourceId, requestId);
            if (updater != null)
            {
                //TODO. this list should be periodically cleaned up
                _updaterList.Add(updater);
            }
            Dto responsePayload;
            try
            {
                responsePayload = HandleEndpointRequest(userSession, requestPayload, updater);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                responsePayload = new Dto { IsError = true, Error = "Internal Server Error" };
            }

            if ((responsePayload != null) && (string.IsNullOrEmpty(ResponseName) == false))
            {

                if (responsePayload.GetType() == typeof(BinaryDto))
                {
                    session.SendMessageToChannel(ResponseName, null, sourceId, requestId, ((BinaryDto)responsePayload).Payload);
                }
                else
                {
                    session.SendMessageToChannel(ResponseName, MiddlewareUtils.SerialiseObjectToString(responsePayload), sourceId, requestId, null);
                }
            }
        }

        /// <summary>
        /// Factory method for creating an updater to use for this channel. by default does not create one.
        /// An updater is required for for sending updates on the response channel. By default, only a single response
        /// is sent for a request.
        /// </summary>
        public virtual Update GetUpdater(IConnectionSession session, UserSession userSession, string sourceId, string requestId)
        {
            return default(Update);
        }

        /// <summary>
        /// Common method for converting a string payload into a request dto object.
        /// </summary>
        public Request ConvertToRequestPayload(string payload)
        {
            return payload != null ? MiddlewareUtils.DeserialiseObject<Request>(payload) : new Request();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// abstract method for handling requests on this channel
        /// </summary>
        protected abstract Dto HandleEndpointRequest(UserSession userSession, Request payload, Update updater);

        /// <summary>
        /// Helper method creates a url link for the valuation reort for the specified account on the
        /// specified report date.
        /// </summary>
        protected string CreateReportLink(IConnectionSettings settings, AccountIdentifier account, DateTime reportDate)
        {
            var root = settings.ServerConnection.ServerName;
            var index = root.IndexOf(':');
            var prefix = root.Substring(0, index);
            if (prefix == "ws")
                root = "http" + root.Substring(index);
            else if (prefix == "wss")
                root = "https" + root.Substring(index);

            return $"{root}/VALUATION_REPORT?Account={account};Date={reportDate.ToString("MMM-yyyy")}";
        }

        #endregion

        #region Private Data Members

        private readonly List<IChannelUpdater> _updaterList = new List<IChannelUpdater>();
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion
    }
}