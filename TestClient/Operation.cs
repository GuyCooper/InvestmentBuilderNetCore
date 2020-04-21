using System;
using System.Threading;
using Transports;
using Transports.Session;

namespace TestClient
{
    /// <summary>
    /// Interface defines an operation to perform on the server.
    /// </summary>

    internal interface IOperation
    {
        void DoOperation();
        void ProcessResponse(Message message);
    }

    /// <summary>
    /// class defines an operation to perform on the server.
    /// </summary>
    internal abstract class Operation<RequestDto, ResponseDto> : IOperation
    {
        public string Name { get; private set; }

        public Operation(IConnectionSession session, string name, string requestChannel, string responseChannel)
        {
            Name = name;
            m_session = session;
            m_requestChannel = requestChannel;
            m_responseChannel = responseChannel;

            m_session.RegisterChannelListener(m_responseChannel);
        }

        public void DoOperation()
        {
            Console.WriteLine($"Running Test: {Name}");
            var request = GetRequest();
            var payload = TransportUtils.SerialiseObjectToString(request);
            m_session.SendMessageToChannel(m_requestChannel, payload, "", "", null);
            m_completed.WaitOne();
        }

        public void ProcessResponse(Message message)
        {
            Console.WriteLine($"Handling response for Test: {Name}");
            
            if(message.Channel != m_responseChannel)
            {
                Console.WriteLine($"Incorrect response channel {message.Channel}. Should be {m_responseChannel}");
                return;
            }

            var response = TransportUtils.DeserialiseObject<ResponseDto>(message.Payload);

            var result = HandleResponse(response);
            if(result)
            {
                Console.WriteLine($"Test {Name} Succeded!!");
            }
            else
            {
                Console.WriteLine($"Test {Name} Failed :-O");
            }
            m_completed.Set();
        }

        protected abstract RequestDto GetRequest();
        protected abstract bool HandleResponse(ResponseDto response);

        private readonly ManualResetEvent m_completed = new ManualResetEvent(false);

        private readonly IConnectionSession m_session;

        private readonly string m_requestChannel;

        private readonly string m_responseChannel;
    }
}
