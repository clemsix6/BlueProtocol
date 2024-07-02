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
/// <typeparam name="T">
/// The type of client to use (<c>SyncClient</c> or <c>AsyncClient</c>).
/// </typeparam>
public class BlueServer<T> where T : BlueClient
{
    private readonly TcpListener tcpListener;
    private readonly List<Controller> controllers = [];
    private bool disposed;


    /// <summary>
    /// Event that is triggered when a client connects to the server.
    /// </summary>
    public event Action<T> OnClientConnectedEvent;

    /// <summary>
    /// Event that is triggered when a client disconnects from the server.
    /// </summary>
    public event Action<T, CloseReason> OnClientDisconnectedEvent;

    /// <summary>
    /// Gets a value indicating whether the server is running.
    /// </summary>
    public bool IsRunning => this.tcpListener.Server.IsBound;

    /// <summary>
    /// Gets the local end point of the server.
    /// </summary>
    public IPEndPoint LocalEndPoint => (IPEndPoint)this.tcpListener.LocalEndpoint;


    /// <summary>
    /// Create a new instance of <c>AsyncServer</c> with the default local end point.
    /// It listens on the loopback address and a random port.
    /// </summary>
    public BlueServer()
    {
        this.tcpListener = new TcpListener(IPAddress.Any, 0);
    }


    /// <summary>
    /// Create a new instance of <c>AsyncServer</c> with the specified port.
    /// It listens on any address and the specified port.
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the port is out of range.</exception>
    public BlueServer(int port)
    {
        this.tcpListener = new TcpListener(IPAddress.Any, port);
    }


    /// <summary>
    /// Start the server.
    /// </summary>
    public void Start()
    {
        this.tcpListener.Start();
        this.tcpListener.BeginAcceptTcpClient(OnClientConnected, null);
    }


    /// <summary>
    /// Add a controller to the server.
    /// </summary>
    /// <param name="controller">The controller to add.</param>
    /// <exception cref="BlueProtocolControllerException">Thrown when the controller is invalid.</exception>
    public void AddController(Controller controller)
    {
        lock (this.controllers)
            this.controllers.Add(controller);
    }


    private void RunClient(T client)
    {
        client.Start();

        client.OnDisconnectedEvent += (c, r) => {
            this.OnClientDisconnectedEvent?.Invoke((T)c, r);
        };
    }


    private void OnClientConnected(IAsyncResult result)
    {
        try {
            var tcpClient = this.tcpListener.EndAcceptTcpClient(result);
            this.tcpListener.BeginAcceptTcpClient(OnClientConnected, null);

            if (Activator.CreateInstance(typeof(T), tcpClient) is not T client)
                throw new InvalidOperationException("The client type is invalid.");

            lock (this.controllers)
                this.controllers.ForEach(x => client.AddController(x));

            RunClient(client);

            this.OnClientConnectedEvent?.Invoke(client);
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
        var client = Activator.CreateInstance(typeof(T), remoteEndPoint) as T;
        if (client == null)
            throw new InvalidOperationException("The client type is invalid.");

        RunClient(client);

        lock (this.controllers)
            this.controllers.ForEach(x => client.AddController(x));

        this.OnClientConnectedEvent?.Invoke(client);

        return client;
    }
}