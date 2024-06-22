using System.Net;
using System.Net.Sockets;
using BlueProtocol.Exceptions;
using BlueProtocol.Network.Events;
using BlueProtocol.Network.Messages;
using BlueProtocol.Requests;


namespace BlueProtocol.Network
{
    /// <summary>
    /// Class <c>Client</c> models all the logic for a client, it includes the connection and the communication.
    /// </summary>
    public class SyncClient : IClient
    {
        public Action<SyncClient, DisconnectEvent> OnDisconnectedEvent;

        public int Timeout { get; set; } = 5000;

        private readonly TcpClient tcpClient;
        private readonly NetworkStream networkStream;
        private readonly MessageQueue messages = new();

        private readonly ClientMemory<Controller> controllers = new();
        private readonly ClientMemory<Request> requests = new();

        public bool IsConnected { get; private set; }
        public IPEndPoint RemoteEndPoint => (IPEndPoint)this.tcpClient.Client.RemoteEndPoint;


        internal SyncClient(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
            this.networkStream = tcpClient.GetStream();

            this.OnDisconnectedEvent += OnRemoteDisconnected;
        }


        private SyncClient(string host, int port)
        {
            this.tcpClient = new TcpClient(host, port);
            this.networkStream = this.tcpClient.GetStream();

            this.OnDisconnectedEvent += OnRemoteDisconnected;
        }


        public static SyncClient Connect(string host, int port)
        {
            var client = new SyncClient(host, port);
            client.Start();
            return client;
        }


        public static SyncClient Connect(IPEndPoint remoteEndPoint)
        {
            var client = new SyncClient(remoteEndPoint.Address.ToString(), remoteEndPoint.Port);
            client.Start();
            return client;
        }


        public void Send(Request request)
        {
            if (request.IsWaitingForResponse()) {
                request.Id = Guid.NewGuid().ToString();
                lock (this.requests)
                    this.requests.Add(request);
            }

            var message = Message.Create(request);
            try {
                message.Send(this.networkStream);
            } catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException) { }
        }


        public void Send(Event ev)
        {
            var message = Message.Create(ev);

            try {
                message.Send(this.networkStream);
            } catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException) { }
        }


        private void Send(Response data)
        {
            var message = Message.Create(data);

            try {
                message.Send(this.networkStream);
            } catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException) { }
        }


        private object ReceiveData()
        {
            try {
                var message = Message.Receive(this.networkStream);
                return message.Deserialize();
            } catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException) {
                return null;
            }
        }


        private void ReceiveLoop()
        {
            while (this.IsConnected) {
                var data = ReceiveData();
                if (data == null) continue;

                if (data is DisconnectEvent ev) {
                    this.IsConnected = false;
                    this.OnDisconnectedEvent?.Invoke(this, ev);
                    return;
                }

                if (data is PingEvent) continue;
                this.messages.Enqueue(data);
            }
        }


        private void SendPing()
        {
            Send(new PingEvent());
        }


        private void UpdateTimeout()
        {
            lock (this.requests) {
                if (this.requests.GetTimedOutItems(this.Timeout).Any()) {
                    var ev = new DisconnectEvent("Timeout");
                    Dispose(ev);
                    throw new BlueProtocolTimeoutException("Request timed out");
                }
            }
        }


        private void MainLoop()
        {
            while (this.IsConnected) {
                Thread.Sleep(2000);
                SendPing();
                UpdateTimeout();
            }
        }


        internal void Start()
        {
            this.IsConnected = true;
            new Thread(ReceiveLoop).Start();
            new Thread(MainLoop).Start();
        }


        public void AddController(Controller controller)
        {
            controller.Build();
            lock (this.controllers)
                this.controllers.Add(controller);
        }


        private void UpdateRequest(Request request)
        {
            lock (this.controllers) {
                foreach (var controller in this.controllers.Items) {
                    if (controller.OnRequest(this, request, out var response) && response is Response r) {
                        Send(r);
                        return;
                    }
                }
            }

            throw new BlueProtocolControllerException($"No controller found for request {request.GetType().FullName}");
        }


        private void UpdateResponse(Response response)
        {
            lock (this.requests) {
                var request = this.requests.Items.Find(x => x.Id == response.RequestId);
                if (request == null)
                    throw new BlueProtocolControllerException($"Request {response.RequestId} not found");

                request.OnResponse(response);
                this.requests.Remove(request);
            }
        }


        private void UpdateEvent(Event ev)
        {
            lock (this.controllers) {
                foreach (var controller in this.controllers.Items) {
                    if (controller.OnEvent(this, ev))
                        return;
                }
            }

            throw new BlueProtocolControllerException($"No controller found for event {ev.GetType().FullName}");
        }


        public void Update()
        {
            var messages = this.messages.DequeueAll();
            foreach (var message in messages) {
                switch (message) {
                    case Request request:
                        UpdateRequest(request);
                        break;
                    case Response response:
                        UpdateResponse(response);
                        break;
                    case Event ev:
                        UpdateEvent(ev);
                        break;
                    default:
                        throw new BlueProtocolControllerException(
                            $"Unknown message type {message.GetType().FullName}");
                }
            }
        }


        private void OnRemoteDisconnected(SyncClient client, DisconnectEvent disconnectEvent)
        {
            this.IsConnected = false;
            this.tcpClient.Dispose();
            this.networkStream.Dispose();
        }


        public void Dispose(DisconnectEvent disconnectEvent)
        {
            Send(disconnectEvent);
            this.OnDisconnectedEvent?.Invoke(this, disconnectEvent);

            this.IsConnected = false;
            this.networkStream.Dispose();
            this.tcpClient.Dispose();
        }


        public void Dispose()
        {
            var ev = new DisconnectEvent("Client disconnected");
            Dispose(ev);
        }
    }
}