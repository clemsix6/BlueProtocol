using System.Net;
using System.Net.Sockets;
using BlueProtocol.Controllers;
using BlueProtocol.Exceptions;
using BlueProtocol.Network.Communication.System;
using BlueProtocol.Network.Sockets.Clients;


namespace BlueProtocol.Network.Sockets.Servers;


/// <summary>
/// The <c>BlueServer</c> class models the logic for a server that listens for clients.
/// </summary>
/// <typeparam name="TClient">
/// The type of client to use (<c>SyncClient</c> or <c>AsyncClient</c>).
/// </typeparam>
public class BlueServer<TClient> where TClient : BlueClient
{
    private readonly TcpListener tcpListener;
    private bool disposed;


    /// <summary>
    /// Event that is triggered when a client connects to the server.
    /// </summary>
    public event Action<TClient> OnClientConnectedEvent;

    /// <summary>
    /// Event that is triggered when a client disconnects from the server.
    /// </summary>
    public event Action<TClient, CloseReason> OnClientDisconnectedEvent;

    /// <summary>
    /// Gets a value indicating whether the server is running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets the local end point of the server.
    /// </summary>
    public IPEndPoint LocalEndPoint => (IPEndPoint)this.tcpListener.LocalEndpoint;

    /// <summary>
    /// The shield that protects the server with timeouts and rate limits.
    /// </summary>
    public ServerShield Shield { get; }

    /// <summary>
    /// The request handler that handles incoming requests.
    /// </summary>
    public RequestHandler<TClient> RequestHandler { get; } = new();


    /// <summary>
    /// Create a new instance of <c>AsyncServer</c> with the default local end point.
    /// It listens on the loopback address and a random port.
    /// </summary>
    /// <param name="shield">The shield to protect the server.</param>
    public BlueServer(ServerShield shield = null)
    {
        this.Shield = shield ?? new ServerShield();
        this.tcpListener = new TcpListener(IPAddress.Any, 0);
    }


    /// <summary>
    /// Create a new instance of <c>AsyncServer</c> with the specified port.
    /// It listens on any address and the specified port.
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    /// <param name="shield">The shield to protect the server.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the port is out of range.</exception>
    public BlueServer(int port, ServerShield shield = null)
    {
        this.Shield = shield ?? new ServerShield();
        this.tcpListener = new TcpListener(IPAddress.Any, port);
    }


    /// <summary>
    /// Start the server.
    /// </summary>
    public void Start()
    {
        this.IsRunning = true;
        this.tcpListener.Start();
        this.tcpListener.BeginAcceptTcpClient(OnClientConnected, null);
    }


    /// <summary>
    /// Stop the server.
    /// </summary>
    public void Stop()
    {
        this.IsRunning = false;
        this.tcpListener.Stop();
    }


    private void ApplyConnectionRateLimit()
    {
        while (true) {
            lock (this.Shield) {
                var now = Environment.TickCount64;

                this.Shield.ConnectionTimesSecond.RemoveAll(x => x < now - 1000);
                this.Shield.ConnectionTimesMinute.RemoveAll(x => x < now - 60000);

                if (this.Shield.ConnectionTimesSecond.Count < this.Shield.MaxConnectionsPerSecond &&
                    this.Shield.ConnectionTimesMinute.Count < this.Shield.MaxConnectionsPerMinute)
                    break;
                Thread.Sleep(100);
            }
        }
    }


    private void RegisterConnection()
    {
        lock (this.Shield) {
            this.Shield.ConnectionTimesSecond.Add(Environment.TickCount64);
            this.Shield.ConnectionTimesMinute.Add(Environment.TickCount64);
        }
    }


    private void OnClientConnected(IAsyncResult result)
    {
        try {
            var tcpClient = this.tcpListener.EndAcceptTcpClient(result);

            var shield = (ClientShield)this.Shield.DefaultClientShield.Clone();
            var requestHandler = (RequestHandler<TClient>)this.RequestHandler.Clone();
            if (Activator.CreateInstance(typeof(TClient), tcpClient, shield, requestHandler) is not TClient client)
                throw new InvalidOperationException("The client type is invalid.");

            client.Start();
            if (!Shield.AcceptNewConnections || !this.IsRunning) {
                client.Close(CloseReason.ConnectionRefused("Server is not accepting new connections."));
            } else {
                client.OnDisconnectedEvent += (c, r) => { this.OnClientDisconnectedEvent?.Invoke((TClient)c, r); };
                this.OnClientConnectedEvent?.Invoke(client);
            }

            RegisterConnection();
            ApplyConnectionRateLimit();

            if (this.IsRunning)
                this.tcpListener.BeginAcceptTcpClient(OnClientConnected, null);
        } catch (SocketException) {
            if (!this.disposed)
                throw;
        } catch (ObjectDisposedException) {
            if (!this.disposed)
                throw;
        }
    }


    public void Dispose()
    {
        this.disposed = true;
        this.tcpListener.Stop();
        this.tcpListener.Server.Dispose();
    }


    public BlueClient Connect(IPEndPoint remoteEndPoint)
    {
        var shield = (ClientShield)this.Shield.DefaultClientShield.Clone();
        var requestHandler = (RequestHandler<TClient>)this.RequestHandler.Clone();
        if (Activator.CreateInstance(typeof(TClient), remoteEndPoint, shield, requestHandler) is not TClient client)
            throw new InvalidOperationException("The client type is invalid.");

        client.Start();
        client.OnDisconnectedEvent += (c, r) => { this.OnClientDisconnectedEvent?.Invoke((TClient)c, r); };
        this.OnClientConnectedEvent?.Invoke(client);

        this.OnClientConnectedEvent?.Invoke(client);

        return client;
    }
}