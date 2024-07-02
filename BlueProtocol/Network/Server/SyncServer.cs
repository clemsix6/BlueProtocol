using System.Net;
using System.Net.Sockets;
using BlueProtocol.Controllers;
using BlueProtocol.Network.Events;


namespace BlueProtocol.Network;


/// <summary>
/// The <c>SyncServer</c> class models a server that listens for incoming connections.
/// It only works with <c>SyncServer</c> instances.
/// </summary>
public class SyncServer : IServer
{
    /// <summary>
    /// Event that is triggered when a client connects to the server.
    /// </summary>
    public event Action<SyncClient> OnClientConnectedEvent;

    /// <summary>
    /// Event that is triggered when a client disconnects from the server.
    /// </summary>
    public event Action<SyncClient, CloseReason> OnClientDisconnectedEvent;


    private readonly TcpListener tcpListener;
    private readonly List<Controller> controllers = [];

    /// <inheritdoc/>
    public bool IsRunning => this.tcpListener.Server.IsBound;

    /// <inheritdoc/>
    public IPEndPoint LocalEndPoint => (IPEndPoint)this.tcpListener.LocalEndpoint;


    /// <summary>
    /// Create a new instance of <c>SyncServer</c> with the default local end point.
    /// It listens on the loopback address and a random port.
    /// </summary>
    public SyncServer()
    {
        this.tcpListener = new TcpListener(IPAddress.Any, 0);
    }


    /// <summary>
    /// Create a new instance of <c>SyncServer</c> with the specified port.
    /// It listens on any address and the specified port.
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the port is out of range.</exception>
    public SyncServer(int port)
    {
        this.tcpListener = new TcpListener(IPAddress.Any, port);
    }


    /// <inheritdoc/>
    public void Start()
    {
        this.tcpListener.Start();
        this.tcpListener.BeginAcceptTcpClient(OnClientConnected, null);
    }


    /// <inheritdoc/>
    public void AddController(Controller controller)
    {
        lock (this.controllers)
            this.controllers.Add(controller);
    }


    private void RunClient(SyncClient client)
    {
        client.Start();

        client.OnDisconnectedEvent += (c, r) => {
            var syncClient = (SyncClient)c;
            this.OnClientDisconnectedEvent?.Invoke(syncClient, r);
        };
    }


    private void OnClientConnected(IAsyncResult result)
    {
        try {
            var tcpClient = this.tcpListener.EndAcceptTcpClient(result);
            this.tcpListener.BeginAcceptTcpClient(OnClientConnected, null);

            var client = new SyncClient(tcpClient);
            lock (this.controllers) this.controllers.ForEach(x => client.AddController(x));
            RunClient(client);

            this.OnClientConnectedEvent?.Invoke(client);
        } catch (ObjectDisposedException) { }
    }


    /// <inheritdoc/>
    public void Dispose()
    {
        this.tcpListener.Stop();
    }


    /// <inheritdoc/>
    public BlueClient Connect(IPEndPoint remoteEndPoint)
    {
        var client = SyncClient.Create(remoteEndPoint);
        RunClient(client);
        lock (this.controllers)
            this.controllers.ForEach(x => client.AddController(x));
        this.OnClientConnectedEvent?.Invoke(client);
        return client;
    }
}