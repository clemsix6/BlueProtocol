using System.Net;
using BlueProtocol.Controllers;
using BlueProtocol.Exceptions;


namespace BlueProtocol.Network;


public interface IServer : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the server is running.
    /// </summary>
    bool IsRunning { get; }


    /// <summary>
    /// Gets the local end point of the server.
    /// </summary>
    IPEndPoint LocalEndPoint { get; }


    /// <summary>
    /// Start the server.
    /// </summary>
    void Start();


    /// <summary>
    /// Add a controller to the server.
    /// </summary>
    /// <param name="controller">The controller to add.</param>
    /// <exception cref="BlueProtocolControllerException">Thrown when the controller is invalid.</exception>
    void AddController(Controller controller);


    /// <summary>
    /// Create a <c>IClient</c>, connect to a remote end point and save the client in the server.
    /// </summary>
    /// <param name="remoteEndPoint">The remote end point to connect to.</param>
    /// <returns>The client connected to the remote end point.</returns>
    /// <exception cref="BlueProtocolNetworkException">Thrown when the host is null,the port is out of range or there is a socket error.</exception>
    BlueClient Connect(IPEndPoint remoteEndPoint);
}