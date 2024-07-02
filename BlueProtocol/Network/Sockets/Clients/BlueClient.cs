using System.Net;
using System.Net.Sockets;
using BlueProtocol.Controllers;
using BlueProtocol.Exceptions;
using BlueProtocol.Network.Communication.Events;
using BlueProtocol.Network.Communication.Messages;
using BlueProtocol.Network.Communication.Requests;
using BlueProtocol.Network.Communication.System;
using BlueProtocol.Network.Memory;


namespace BlueProtocol.Network.Sockets.Clients;


/// <summary>
/// The <c>BlueClient</c> class models all the logic for a client,
/// it includes the connection and the sending of messages.
/// This class is abstract and should be inherited by a specific client implementation
/// (<c>SyncClient</c> or <c>AsyncClient</c>).
/// </summary>
public abstract class BlueClient
{
    private readonly TcpClient tcpClient;
    protected readonly NetworkStream networkStream;

    internal readonly ClientMemory<Controller> controllers = new();
    internal readonly ClientMemory<Request> requests = new();
    protected readonly List<Thread> threads = [];


    /// <summary>
    /// Event that is triggered when the client is disconnected.
    /// </summary>
    public event Action<BlueClient, CloseReason> OnDisconnectedEvent;

    /// <summary>
    /// The shield that protects the client with timeouts and rate limits.
    /// </summary>
    public ClientShield Shield { get; }

    /// <summary>
    /// Indicates if the client is connected.
    /// </summary>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// The remote endpoint of the client.
    /// </summary>
    public IPEndPoint RemoteEndPoint => (IPEndPoint)this.tcpClient.Client.RemoteEndPoint;

    /// <summary>
    /// The local endpoint of the client.
    /// </summary>
    public IPEndPoint LocalEndPoint => (IPEndPoint)this.tcpClient.Client.LocalEndPoint;

    /// <summary>
    /// The time when the client connected.
    /// </summary>
    public DateTime ConnectionTime { get; } = DateTime.Now;


    internal BlueClient(TcpClient tcpClient, ClientShield clientShield = null)
    {
        this.Shield = clientShield ?? new ClientShield();
        this.tcpClient = tcpClient;
        this.networkStream = tcpClient.GetStream();
    }


    protected BlueClient(IPEndPoint remoteEndPoint, ClientShield clientShield = null)
    {
        this.Shield = clientShield ?? new ClientShield();

        try {
            this.tcpClient = new TcpClient();
            this.tcpClient.Connect(remoteEndPoint);
        } catch (ObjectDisposedException e) {
            throw new BlueProtocolNetworkException("The TcpClient is disposed", e);
        } catch (ArgumentNullException e) {
            throw new BlueProtocolNetworkException("Host is null", e);
        } catch (ArgumentOutOfRangeException e) {
            throw new BlueProtocolNetworkException("Port is out of range", e);
        } catch (SocketException e) {
            throw new BlueProtocolNetworkException("Socket error", e);
        }

        try {
            this.networkStream = this.tcpClient.GetStream();
        } catch (ObjectDisposedException e) {
            throw new BlueProtocolNetworkException("The TcpClient is disposed", e);
        } catch (InvalidOperationException e) {
            throw new BlueProtocolNetworkException("The TcpClient is not connected", e);
        }
    }


    /// <summary>
    /// Start the client, this will receive and send messages.
    /// </summary>
    public void Start()
    {
        if (this.IsConnected)
            return;
        this.IsConnected = true;

        var mainThread = new Thread(MainLoop);
        mainThread.Start();
        this.threads.Add(mainThread);

        OnStart();
    }


    private void SendPing()
    {
        Send(new PingEvent());
    }


    private void UpdateTimeout()
    {
        lock (this.requests) {
            if (this.requests.GetTimedOutItems(this.Shield.ResponseTimeout).Count != 0) {
                Close(CloseReason.Timeout("Response timed out"));
                throw new BlueProtocolTimeoutException("Request timed out");
            }
        }
    }


    private void CheckLifeTime()
    {
        if (this.Shield.LifeTime != -1 &&
            (DateTime.Now - this.ConnectionTime).TotalMilliseconds >= this.Shield.LifeTime)
            Close(CloseReason.LifeTimeExceeded("Life time exceeded"));
    }


    private void MainLoop()
    {
        var lastPingTime = Environment.TickCount64;

        while (this.IsConnected) {
            Thread.Sleep(500);

            if (Environment.TickCount64 - lastPingTime >= 5000) {
                SendPing();
                lastPingTime = Environment.TickCount64;
            }

            UpdateTimeout();
            CheckLifeTime();
        }
    }


    /// <summary>
    /// Send a request to the remote host.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <exception cref="BlueProtocolException">Thrown when the request is waiting for a response.</exception>
    public void Send(Request request)
    {
        request.RequestId = Guid.NewGuid().ToString();
        lock (this.requests)
            this.requests.Add(request);

        var message = Message.Create(request);

        try {
            lock (this.networkStream)
                message.Send(this.networkStream);
        } catch (BlueProtocolNetworkException) {
            Close(CloseReason.InternalError("Failed to send response"));
        }
    }


    /// <summary>
    /// Send an event to the remote host.
    /// </summary>
    /// <param name="ev">The event to send.</param>
    /// <exception cref="BlueProtocolConnectionClosed">Thrown when the connection is closed.</exception>
    public void Send(Event ev)
    {
        var message = Message.Create(ev);

        try {
            lock (this.networkStream)
                message.Send(this.networkStream);
        } catch (BlueProtocolNetworkException) {
            Close(CloseReason.InternalError("Failed to send response"));
        }
    }


    protected void Send(Response data)
    {
        var message = Message.Create(data);

        try {
            lock (this.networkStream)
                message.Send(this.networkStream);
        } catch (BlueProtocolNetworkException) {
            Close(CloseReason.InternalError("Failed to send response"));
        }
    }


    /// <summary>
    /// Add a controller to the client.
    /// </summary>
    /// <param name="controller">The controller to add.</param>
    /// <exception cref="BlueProtocolControllerException">Thrown when the controller is invalid.</exception>
    public void AddController(Controller controller)
    {
        controller.Build();
        lock (this.controllers)
            this.controllers.Add(controller);
    }


    protected void ApplyRateLimit()
    {
        while (true) {
            lock (this.Shield) {
                var now = Environment.TickCount64;

                this.Shield.RequestTimesSecond.RemoveAll(x => x < now - 1000);
                this.Shield.RequestTimesMinute.RemoveAll(x => x < now - 60000);

                if (this.Shield.RequestTimesSecond.Count < this.Shield.MaxRequestsPerSecond &&
                    this.Shield.RequestTimesMinute.Count < this.Shield.MaxRequestsPerMinute)
                    break;
                Thread.Sleep(100);
            }
        }
    }


    protected void RegisterRequest()
    {
        lock (this.Shield) {
            this.Shield.RequestTimesSecond.Add(Environment.TickCount64);
            this.Shield.RequestTimesMinute.Add(Environment.TickCount64);
        }
    }


    protected virtual void OnStart()
    {
        throw new NotImplementedException();
    }


    protected void OnRemoteClose(CloseReason reason)
    {
        this.IsConnected = false;
        this.OnDisconnectedEvent?.Invoke(this, reason);

        Thread.Sleep(1000);
        this.tcpClient?.Dispose();
        lock (this.networkStream)
            this.networkStream?.Dispose();

        foreach (var thread in this.threads)
            thread.Join();
    }


    /// <summary>
    /// Close the client with the specified reason.
    /// </summary>
    /// <param name="reason">The reason for closing the client.</param>
    public void Close(CloseReason reason)
    {
        if (!this.IsConnected)
            return;

        this.IsConnected = false;
        this.OnDisconnectedEvent?.Invoke(this, reason);

        var closeRequest = new CloseRequest { Reason = reason };
        Send(closeRequest);
        closeRequest.Wait(1000);

        this.tcpClient?.Dispose();
        lock (this.networkStream)
            this.networkStream?.Dispose();

        foreach (var thread in this.threads)
            thread.Join();
    }



    /// <summary>
    /// [Unrecommended] Close the client without sending a close request.
    /// </summary>
    public void Close()
    {
        if (!this.IsConnected)
            return;

        var reason = CloseReason.NoReason();
        this.IsConnected = false;
        this.OnDisconnectedEvent?.Invoke(this, reason);

        var closeRequest = new CloseRequest { Reason = reason };
        Send(closeRequest);
        closeRequest.Wait(1000);

        this.tcpClient?.Dispose();
        lock (this.networkStream)
            this.networkStream?.Dispose();

        foreach (var thread in this.threads)
            thread.Join();
    }
}