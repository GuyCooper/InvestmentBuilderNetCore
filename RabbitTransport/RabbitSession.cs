using System;
using System.Threading.Tasks;
using Transports.Session;
using RabbitMQ.Client;
using Transports.Utils;
using RabbitMQ.Client.Events;
using NLog;

namespace RabbitTransport
{
    /// <summary>
    /// RabbitMQ connection session.
    /// </summary>
    class RabbitSession : IConnectionSession, IDisposable
    {
        #region Constructor

        /// <summary>
        /// Constructor. connect to the rabbit mq server
        /// </summary>
        public RabbitSession(ConnectionSettings settings, string appName)
        {
            _factory = new ConnectionFactory() { HostName = settings.ServerConnection.ServerName };
            _connection = _factory.CreateConnection();
            _model = _connection.CreateModel();
            _sourceID = Guid.NewGuid().ToString();
            var queueName = _model.QueueDeclare().QueueName;

            CreateClientResponseExchange();
            // create a unique queue for this client in
            _model.QueueBind(queue: queueName,
                                 exchange: _clientResponseExchange,
                                 routingKey: _sourceID);

            var consumer = new EventingBasicConsumer(_model);
            consumer.Received += (model, ea) =>
            {
                ProcessMessage(ea.Body);
            };

            _model.BasicConsume(queue: queueName,
                                 autoAck: true,
                                 consumer: consumer);
        }

        #endregion

        #region IConnectionSession

        /// <summary>
        /// Broadcast a message on specified channel (picked up by all listeners)
        /// </summary>
        public void BroadcastMessage(string channel, string payload)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Register as an authentication server.
        /// </summary>
        public Task<bool> RegisterAuthenticationServer(string identifier)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Register a listener for a channel. handles non - routed requests on this channel. Use this method 
        /// for registering a handler for worker request messages.
        /// </summary>
        public void RegisterChannelListener(string channel)
        {
            CreateChannel(channel);

            var consumer = new EventingBasicConsumer(_model);
            consumer.Received += (model, ea) =>
            {
                ProcessMessage(ea.Body);
                _model.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            _model.BasicConsume(queue: channel,
                                 autoAck: false,
                                 consumer: consumer);
        }

        /// <summary>
        /// Register a handler to consume all incoming messages
        /// </summary>
        public void RegisterMessageHandler(SessionMessageHandler handler)
        {
            _handler = handler;
        }

        /// <summary>
        /// If registered as a authentication server, send the authentication result.
        /// </summary>
        public void SendAuthenticationResult(bool result, string message, string requestid)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sends a message to a channel. If destination is not specified then message will be picked up by
        /// first available worker that is subscribng to this channel. Done using a round robin method.
        /// </summary>
        public void SendMessageToChannel(string channel, string payload, string destination, string requestId, byte[] binaryPayload)
        {
            var message = new Middleware.Message
            {
                Channel = channel,
                Type = Middleware.MessageType.REQUEST,
                DestinationId = destination,
                RequestId = requestId,
                Payload = payload,
                BinaryPayload = binaryPayload,
                SourceId = _sourceID
            };

            var serialisedMessage = Middleware.MiddlewareUtils.SerialiseObject(message);

            IBasicProperties properties = null;
            string route = null;
            string exchange = "";
            if(string.IsNullOrWhiteSpace(destination))
            {
                CreateChannel(channel);
                properties = _model.CreateBasicProperties();
                properties.Persistent = true;
                route = channel;
            }
            else
            {
                exchange = _clientResponseExchange;
                route = destination;
            }

            _model.BasicPublish(exchange: exchange,
                           routingKey: route,
                           basicProperties: properties,
                           body: serialisedMessage);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) return;
            _model.Dispose();
            _connection.Dispose();
            IsDisposed = true;
        }

        protected bool IsDisposed { get; set; }
        #endregion

        #region Private Methods

        /// <summary>
        /// Creates the specified channel on the server. Doesn't matter if it alreadys exists
        /// </summary>
        private void CreateChannel(string channel)
        {
            if (_model == null)
            {
                throw new ApplicationException("Must connect to server first...");
            }

            if (_handler == null)
            {
                throw new ApplicationException("Must register a callback before calling...");
            }

            _model.QueueDeclare(queue: channel,
                               durable: true,
                               exclusive: false,
                               autoDelete: false,
                               arguments: null);

        }

        /// <summary>
        /// Create an exchange. this is for creating directed queues.
        /// </summary>
        private void CreateClientResponseExchange()
        {
            if (_model == null)
            {
                throw new ApplicationException("Must connect to server first...");
            }

            if (_handler == null)
            {
                throw new ApplicationException("Must register a callback before calling...");
            }

            _model.ExchangeDeclare(exchange: _clientResponseExchange, type: "direct");
        }

        /// <summary>
        /// Process message
        /// </summary>
        private void ProcessMessage(byte[] payload)
        {
            if(_handler == null)
            {
                logger.Error("NO callback handler registered");
                return;
            }

            var message = Middleware.MiddlewareUtils.DeserialiseObject<Middleware.Message>(payload);
            _handler(message);
        }
        #endregion

        #region Private Data

        private readonly ConnectionFactory _factory;
        private readonly RabbitMQ.Client.IConnection _connection;
        private readonly string _sourceID;
        private readonly IModel _model;
        private SessionMessageHandler _handler;
        private static readonly string _clientResponseExchange = "CLIENT_RESPONSE";
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        #endregion
    }
}
