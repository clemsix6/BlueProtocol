using System.Net;
using BlueProtocol.Controllers;
using BlueProtocol.Exceptions;
using BlueProtocol.Network.Events;
using BlueProtocol.Network.Requests;


namespace BlueProtocol.Network;


public interface IClient : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the client is connected to the remote host.
    /// </summary>
    bool IsConnected { get; }


    /// <summary>
    /// Get the time maximum for the client to wait for a response.
    /// </summary>
    int ResponseTimeout { get; set; }


    /// <summary>
    /// Get the time when the client connected to the remote host.
    /// </summary>
    DateTime ConnectionTime { get; }


    /// <summary>
    /// Get the time when the client received the last response.
    /// </summary>
    DateTime LastResponseTime { get; }


    /// <summary>
    /// Get the remote end point of the client.
    /// </summary>
    IPEndPoint RemoteEndPoint { get; }


    /// <summary>
    /// Get the local end point of the client.
    /// </summary>
    IPEndPoint LocalEndPoint { get; }


    /// <summary>
    /// Send a request to the remote host.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <exception cref="BlueProtocolException">Thrown when the request is waiting for a response.</exception>
    void Send(Request request);


    /// <summary>
    /// Send an event to the remote host.
    /// </summary>
    /// <param name="ev">The event to send.</param>
    /// <exception cref="BlueProtocolConnectionClosed">Thrown when the connection is closed.</exception>
    void Send(Event ev);


    /// <summary>
    /// Add a controller to the client.
    /// </summary>
    /// <param name="controller">The controller to add.</param>
    /// <exception cref="BlueProtocolControllerException">Thrown when the controller is invalid.</exception>
    void AddController(Controller controller);


    /// <summary>
    /// Dispose the client with a default disconnect event.
    /// </summary>
    void Dispose(DisconnectEvent disconnectEvent);
}