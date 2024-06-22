using System.Net;
using System.Net.Sockets;
using BlueProtocol.Requests;


namespace BlueProtocol.Network
{
    public class AsyncServer : IServer
    {
        public Action<AsyncClient> OnClientConnectedEvent;

        private readonly TcpListener tcpListener;
        private readonly List<AsyncClient> clients = [];
        private readonly List<Controller> controllers = [];

        public bool IsRunning => this.tcpListener.Server.IsBound;
        public IPEndPoint LocalEndPoint => (IPEndPoint)this.tcpListener.LocalEndpoint;


        public AsyncServer()
        {
            this.tcpListener = new TcpListener(IPAddress.Loopback, 0);
        }


        public AsyncServer(int port)
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

                var client = new AsyncClient(tcpClient);
                lock (this.controllers) this.controllers.ForEach(x => client.AddController(x));
                client.Start();

                lock (this.clients)
                    this.clients.Add(client);
                this.OnClientConnectedEvent?.Invoke(client);
            } catch (ObjectDisposedException) { }
        }


        public IEnumerable<AsyncClient> GetClients()
        {
            lock (this.clients)
                return this.clients.ToArray();
        }


        public void Dispose()
        {
            this.tcpListener.Stop();
        }


        public IClient Connect(IPEndPoint remoteEndPoint)
        {
            var client = AsyncClient.Connect(remoteEndPoint);
            lock (this.clients)
                this.clients.Add(client);
            lock (this.controllers)
                this.controllers.ForEach(x => client.AddController(x));
            this.OnClientConnectedEvent?.Invoke(client);
            return client;
        }
    }
}