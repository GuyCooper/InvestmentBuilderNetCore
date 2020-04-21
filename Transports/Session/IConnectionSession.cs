using System.Threading.Tasks;

namespace Transports.Session
{
    public delegate void SessionMessageHandler(Message message);

    /// <summary>
    /// Interface defines a session to a middleware layer
    /// </summary>
    public interface IConnectionSession
    {
        void RegisterMessageHandler(SessionMessageHandler handler);
        void SendMessageToChannel(string channel, string payload, string destination, string requestId, byte[] binaryPayload);
        void BroadcastMessage(string channel, string payload);
        Task<bool> RegisterAuthenticationServer(string identifier);
        void SendAuthenticationResult(bool result, string message, string requestid);
        void RegisterChannelListener(string channel);
    }
}
