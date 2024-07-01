using System.Net;
using System.Net.Sockets;
using BlueProtocol.Controllers;
using BlueProtocol.Network.Events;


namespace BlueProtocol.Network;


/// <summary>
/// The <c>AsyncServer</c> class models a server that listens for incoming connections.
/// It only works with <c>AsyncClient</c> instances.
/// </summary>
public class AsyncServer : IServer
{
    // ReSharper disable once UnassignedField.Global
    // ReSharper disable once InconsistentNaming
    public Action<AsyncClient> OnClientConnectedEvent;

    // ReSharper disable once UnassignedField.Global
    // ReSharper disable once InconsistentNaming
    public Action<AsyncClient, DisconnectEvent> OnClientDisconnectedEvent;

    private readonly TcpListener tcpListener;
    private readonly List<AsyncClient> clients = [];
    private readonly List<Controller> controllers = [];

    /// <inheritdoc/>
    public bool IsRunning => this.tcpListener.Server.IsBound;

    /// <inheritdoc/>
    public IPEndPoint LocalEndPoint => (IPEndPoint)this.tcpListener.LocalEndpoint;


    /// <summary>
    /// Create a new instance of <c>AsyncServer</c> with the default local end point.
    /// It listens on the loopback address and a random port.
    /// </summary>
    public AsyncServer()
    {
        this.tcpListener = new TcpListener(IPAddress.Any, 0);
    }


    /// <summary>
    /// Create a new instance of <c>AsyncServer</c> with the specified port.
    /// It listens on any address and the specified port.
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the port is out of range.</exception>
    public AsyncServer(int port)
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

    private void AddClient(AsyncClient client)
    {
        lock (this.clients)
            this.clients.Add(client);
        client.Start();

        client.OnDisconnectedEvent += (c, e) => {
            lock (this.clients)
                this.clients.Remove(c);
            this.OnClientDisconnectedEvent?.Invoke(c, e);
        };
    }


    private void OnClientConnected(IAsyncResult result)
    {
        try {
            var tcpClient = this.tcpListener.EndAcceptTcpClient(result);
            this.tcpListener.BeginAcceptTcpClient(OnClientConnected, null);

            var client = new AsyncClient(tcpClient);
            lock (this.controllers) this.controllers.ForEach(x => client.AddController(x));
            AddClient(client);

            this.OnClientConnectedEvent?.Invoke(client);
        } catch (ObjectDisposedException) { }
    }


    /// <summary>
    /// Get all the <c>AsyncClient</c> instances connected to the server.
    /// </summary>
    public List<AsyncClient> GetClients()
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
        var client = AsyncClient.Connect(remoteEndPoint);
        AddClient(client);

        lock (this.controllers)
            this.controllers.ForEach(x => client.AddController(x));
        this.OnClientConnectedEvent?.Invoke(client);
        return client;
    }
}