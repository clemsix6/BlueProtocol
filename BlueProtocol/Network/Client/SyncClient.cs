using System.Net;
using System.Net.Sockets;
using BlueProtocol.Controllers;
using BlueProtocol.Exceptions;
using BlueProtocol.Network.Events;
using BlueProtocol.Network.Messages;
using BlueProtocol.Network.Requests;


namespace BlueProtocol.Network;


/// <summary>
/// Class <c>SyncClient</c> models all the logic for a client, it includes the connection and the communication.
/// It processes all the messages in the same thread by calling the <c>Update</c> method.
/// </summary>
public class SyncClient : IClient
{
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once MemberCanBePrivate.Global
    public Action<SyncClient, DisconnectEvent> OnDisconnectedEvent;

    /// <inheritdoc/>
    public int ResponseTimeout { get; set; } = 5000;

    private readonly TcpClient tcpClient;
    private readonly NetworkStream networkStream;
    private readonly MessageQueue messages = new();

    private readonly ClientMemory<Controller> controllers = new();
    private readonly ClientMemory<Request> requests = new();

    /// <inheritdoc/>
    public bool IsConnected { get; private set; }

    /// <inheritdoc/>
    public IPEndPoint RemoteEndPoint => (IPEndPoint)this.tcpClient.Client.RemoteEndPoint;

    /// <inheritdoc/>
    public IPEndPoint LocalEndPoint => (IPEndPoint)this.tcpClient.Client.LocalEndPoint;

    /// <inheritdoc/>
    public DateTime ConnectionTime { get; } = DateTime.Now;

    /// <inheritdoc/>
    public DateTime LastResponseTime { get; private set; } = DateTime.Now;


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


    /// <summary>
    /// Connect to a remote host.
    /// </summary>
    /// <param name="host">The host to connect to.</param>
    /// <param name="port">The port to connect to.</param>
    /// <returns>The client connected to the remote host.</returns>
    /// <exception cref="BlueProtocolNetworkException">Thrown when the host is null, the port is out of range or there is a socket error.</exception>
    public static SyncClient Connect(string host, int port)
    {
        var client = new SyncClient(host, port);
        client.Start();
        return client;
    }


    /// <summary>
    /// Connect to a remote end point.
    /// </summary>
    /// <param name="remoteEndPoint">The remote end point to connect to.</param>
    /// <returns>The client connected to the remote end point.</returns>
    /// <exception cref="BlueProtocolNetworkException">Thrown when the host is null, the port is out of range or there is a socket error.</exception>
    public static SyncClient Connect(IPEndPoint remoteEndPoint)
    {
        var client = new SyncClient(remoteEndPoint.Address.ToString(), remoteEndPoint.Port);
        client.Start();
        return client;
    }


    /// <inheritdoc/>
    public void Send(Request request)
    {
        if (request.IsWaitingForResponse()) {
            request.RequestId = Guid.NewGuid().ToString();
            lock (this.requests)
                this.requests.Add(request);
        }

        var message = Message.Create(request);

        try {
            lock (this.networkStream)
                message.Send(this.networkStream);
        } catch (BlueProtocolNetworkException) {
            OnDisconnectedEvent.Invoke(this, new DisconnectEvent("Connection closed"));
        }
    }


    /// <inheritdoc/>
    public void Send(Event ev)
    {
        var message = Message.Create(ev);
        try {
            lock (this.networkStream)
                message.Send(this.networkStream);
        } catch (BlueProtocolNetworkException) {
            OnDisconnectedEvent.Invoke(this, new DisconnectEvent("Connection closed"));
        }
    }


    private void Send(Response data)
    {
        var message = Message.Create(data);

        try {
            lock (this.networkStream)
                message.Send(this.networkStream);
        } catch (BlueProtocolNetworkException) {
            OnDisconnectedEvent.Invoke(this, new DisconnectEvent("Connection closed"));
        }
    }


    private object ReceiveData()
    {
        try {
            // ReSharper disable once InconsistentlySynchronizedField
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

            this.LastResponseTime = DateTime.Now;

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
            if (this.requests.GetTimedOutItems(this.ResponseTimeout).Count != 0) {
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
        if (this.IsConnected)
            return;
        this.IsConnected = true;
        new Thread(ReceiveLoop).Start();
        new Thread(MainLoop).Start();
    }


    /// <inheritdoc/>
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
            var request = this.requests.Items.Find(x => x.RequestId == response.RequestId);
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


    /// <summary>
    /// Process all waiting requests, responses and events in the same thread.
    /// </summary>
    /// <exception cref="BlueProtocolControllerException"></exception>
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

        // ReSharper disable once InconsistentlySynchronizedField
        this.networkStream.Dispose();
    }


    /// <inheritdoc/>
    public void Dispose(DisconnectEvent disconnectEvent)
    {
        Send(disconnectEvent);
        this.OnDisconnectedEvent?.Invoke(this, disconnectEvent);

        this.IsConnected = false;

        // ReSharper disable once InconsistentlySynchronizedField
        this.networkStream.Dispose();
        this.tcpClient.Dispose();
    }


    /// <inheritdoc/>
    public void Dispose()
    {
        var ev = new DisconnectEvent("Client disconnected");
        Dispose(ev);
    }
}