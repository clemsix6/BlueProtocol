using System;
using BlueProtocol.Network.Events;
using BlueProtocol.Requests;


namespace BlueProtocol.Network
{
    public interface IClient : IDisposable
    {
        bool IsConnected { get; }
        int Timeout { get; set; }

        void Send(Request request);
        void Send(Event @event);
        void AddController(Controller controller);
        void Dispose(DisconnectEvent disconnectEvent);
    }
}