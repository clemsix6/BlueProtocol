using System.Net;
using System.Net.Sockets;
using BlueProtocol.Controllers;


namespace BlueProtocol.Network;


/// <summary>
/// The <c>SyncServer</c> class models a server that listens for incoming connections.
/// It only works with <c>SyncServer</c> instances.
/// </summary>
public class SyncServer : IServer
{
    // ReSharper disable once UnassignedField.Global
    // ReSharper disable once InconsistentNaming
    public Action<SyncClient> OnClientConnectedEvent;

    private readonly TcpListener tcpListener;
    private readonly List<SyncClient> clients = [];
    private readonly List<Controller> controllers = [];

    public bool IsRunning => this.tcpListener.Server.IsBound;
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


    /// <summary>
    /// Get all the <c>SyncClient</c> instances connected to the server.
    /// </summary>
    public List<SyncClient> GetClients()
    {
        lock (this.clients)
            return [..this.clients];
    }


    /// <inheritdoc/>
    public void Dispose()
    {
        this.tcpListener.Stop();
    }


    /// <inheritdoc/>
    public IClient Connect(IPEndPoint remoteEndPoint)
    {
        var client = SyncClient.Connect(remoteEndPoint);
        lock (this.clients)
            this.clients.Add(client);
        lock(this.controllers)
            this.controllers.ForEach(x => client.AddController(x));
        this.OnClientConnectedEvent?.Invoke(client);
        return client;
    }
}