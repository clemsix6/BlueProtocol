using System;
using BlueProtocol.Requests;


namespace BlueProtocol.Network
{
    public interface IServer : IDisposable
    {
        bool IsRunning { get; }

        void Start();
        void AddController(Controller controller);
    }
}