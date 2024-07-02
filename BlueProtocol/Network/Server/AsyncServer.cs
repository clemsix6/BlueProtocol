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
    private readonly TcpListener tcpListener;
    private readonly List<Controller> controllers = [];
    private bool disposed = false;


    /// <summary>
    /// Event that is triggered when a client connects to the server.
    /// </summary>
    public event Action<AsyncClient> OnClientConnectedEvent;

    /// <summary>
    /// Event that is triggered when a client disconnects from the server.
    /// </summary>
    public event Action<AsyncClient, CloseReason> OnClientDisconnectedEvent;

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


    private void RunClient(AsyncClient client)
    {
        client.Start();

        client.OnDisconnectedEvent += (c, r) => {
            var asyncClient = (AsyncClient)c;
            this.OnClientDisconnectedEvent?.Invoke(asyncClient, r);
        };
    }


    private void OnClientConnected(IAsyncResult result)
    {
        try {
            var tcpClient = this.tcpListener.EndAcceptTcpClient(result);
            this.tcpListener.BeginAcceptTcpClient(OnClientConnected, null);

            var client = new AsyncClient(tcpClient);
            lock (this.controllers) this.controllers.ForEach(x => client.AddController(x));
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


    /// <inheritdoc/>
    public void Dispose()
    {
        this.disposed = true;
        this.tcpListener.Stop();
        this.tcpListener.Server.Dispose();
    }


    /// <inheritdoc/>
    public BlueClient Connect(IPEndPoint remoteEndPoint)
    {
        var client = AsyncClient.Create(remoteEndPoint);
        RunClient(client);

        lock (this.controllers)
            this.controllers.ForEach(x => client.AddController(x));
        this.OnClientConnectedEvent?.Invoke(client);
        return client;
    }
}