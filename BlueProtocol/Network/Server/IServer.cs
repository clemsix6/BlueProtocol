using System.Net;
using BlueProtocol.Requests;


namespace BlueProtocol.Network
{
    public interface IServer : IDisposable
    {
        bool IsRunning { get; }
        IPEndPoint LocalEndPoint { get; }

        void Start();
        void AddController(Controller controller);
        IClient Connect(IPEndPoint remoteEndPoint);
    }
}