using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using BlueProtocol.Requests;


namespace BlueProtocol.Network
{
    public class SyncServer : IServer
    {
        public Action<SyncClient> OnClientConnectedEvent;

        private readonly TcpListener tcpListener;
        private readonly List<SyncClient> clients = new List<SyncClient>();
        private readonly List<Controller> controllers = new List<Controller>();

        public bool IsRunning => this.tcpListener.Server.IsBound;


        public SyncServer(int port)
        {
            this.tcpListener = new TcpListener(IPAddress.Any, port);
        }


        public void Start()
        {
            this.tcpListener.Start();
            this.tcpListener.BeginAcceptTcpClient(OnClientConnected, null);
        }


        public void AddController(Controller controller)
        {
            lock (this.controllers)
                this.controllers.Add(controller);
        }


        private void OnClientConnected(IAsyncResult result)
        {
            try {
                var tcpClient = this.tcpListener.EndAcceptTcpClient(result);
                this.tcpListener.BeginAcceptTcpClient(OnClientConnected, null);

                var client = new SyncClient(tcpClient);
                lock (this.controllers) this.controllers.ForEach(x => client.AddController(x));
                client.Start();

                lock (this.clients)
                    this.clients.Add(client);
                this.OnClientConnectedEvent?.Invoke(client);
            } catch (ObjectDisposedException) { }
        }


        public IEnumerable<SyncClient> GetClients()
        {
            lock (this.clients)
                return this.clients.ToArray();
        }


        public void Dispose()
        {
            this.tcpListener.Stop();
        }
    }
}