using System.Net;
using System.Net.Sockets;
using BlueProtocol.Controllers;
using BlueProtocol.Exceptions;
using BlueProtocol.Network.Events;
using BlueProtocol.Network.Messages;
using BlueProtocol.Network.Requests;


namespace BlueProtocol.Network;


/// <summary>
/// Class <c>Client</c> models all the logic for a client, it includes the connection and the communication.
/// </summary>
public class AsyncClient : IClient
{
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once MemberCanBePrivate.Global
    public Action<AsyncClient, DisconnectEvent> OnDisconnectedEvent;

    public int Timeout { get; set; } = 5000;

    private readonly TcpClient tcpClient;
    private readonly NetworkStream networkStream;

    private readonly ClientMemory<Controller> controllers = new();
    private readonly ClientMemory<Request> requests = new();

    public bool IsConnected { get; private set; }
    public IPEndPoint RemoteEndPoint => (IPEndPoint)this.tcpClient.Client.RemoteEndPoint;


    internal AsyncClient(TcpClient tcpClient)
    {
        this.tcpClient = tcpClient;
        this.networkStream = tcpClient.GetStream();

        this.OnDisconnectedEvent += OnRemoteDisconnected;
    }


    private AsyncClient(string host, int port)
    {
        this.tcpClient = new TcpClient(host, port);
        this.networkStream = this.tcpClient.GetStream();

        this.OnDisconnectedEvent += OnRemoteDisconnected;
    }


    public static AsyncClient Connect(string host, int port)
    {
        var client = new AsyncClient(host, port);
        client.Start();
        return client;
    }


    public static AsyncClient Connect(IPEndPoint remoteEndPoint)
    {
        var client = new AsyncClient(remoteEndPoint.Address.ToString(), remoteEndPoint.Port);
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
        } catch (Exception e) when (e is IOException || e is ObjectDisposedException) { }
    }


    public void Send(Event ev)
    {
        var message = Message.Create(ev);

        try {
            message.Send(this.networkStream);
        } catch (Exception e) when (e is IOException || e is ObjectDisposedException) { }
    }


    private void Send(Response data)
    {
        var message = Message.Create(data);

        try {
            message.Send(this.networkStream);
        } catch (Exception e) when (e is IOException || e is ObjectDisposedException) { }
    }


    private object ReceiveData()
    {
        try {
            var message = Message.Receive(this.networkStream);
            return message.Deserialize();
        } catch (Exception e) when (e is IOException || e is ObjectDisposedException) {
            return null;
        }
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

        throw new BlueProtocolControllerException(
            $"No controller found for request {request.GetType().FullName}");
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


    private void ReceiveLoop()
    {
        while (this.IsConnected) {
            var data = ReceiveData();
            if (data == null) continue;

            if (data is DisconnectEvent disconnectEvent) {
                this.IsConnected = false;
                this.OnDisconnectedEvent?.Invoke(this, disconnectEvent);
                return;
            }

            if (data is PingEvent) continue;

            if (data is Request request) {
                Task.Run(() => UpdateRequest(request));
                continue;
            }

            if (data is Response response) {
                Task.Run(() => UpdateResponse(response));
                continue;
            }

            if (data is Event ev) {
                Task.Run(() => UpdateEvent(ev));
                continue;
            }

            throw new BlueProtocolNetworkException($"Invalid data {data}");
        }
    }


    private void SendPing()
    {
        Send(new PingEvent());
    }


    private void UpdateTimeout()
    {
        lock (this.requests) {
            if (this.requests.GetTimedOutItems(this.Timeout).Count != 0) {
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


    private void OnRemoteDisconnected(AsyncClient client, DisconnectEvent disconnectEvent)
    {
        this.IsConnected = false;
        this.tcpClient.Dispose();
        this.networkStream.Dispose();
    }


    public void Dispose(DisconnectEvent disconnectEvent)
    {
        if (!this.IsConnected) return;

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