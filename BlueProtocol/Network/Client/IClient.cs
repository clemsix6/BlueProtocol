using System.Net;
using BlueProtocol.Controllers;
using BlueProtocol.Network.Events;
using BlueProtocol.Network.Requests;


namespace BlueProtocol.Network;


public interface IClient : IDisposable
{
    bool IsConnected { get; }
    int Timeout { get; set; }
    IPEndPoint RemoteEndPoint { get; }

    void Send(Request request);
    void Send(Event ev);
    void AddController(Controller controller);
    void Dispose(DisconnectEvent disconnectEvent);
}