using System.Net;
using System.Net.Sockets;
using BlueProtocol.Exceptions;
using BlueProtocol.Network.Events;
using BlueProtocol.Network.Messages;
using BlueProtocol.Network.Requests;


namespace BlueProtocol.Network;


/// <summary>
/// Class <c>SyncClient</c> models a client that processes messages synchronously.
/// It processes all messages in the queue when <c>ProcessAll</c> is called,
/// or processes one message when <c>ProcessOne</c> is called.
/// </summary>
public class SyncClient : BlueClient
{
    private readonly MessageQueue messages = new();


    internal SyncClient(TcpClient tcpClient) : base(tcpClient) { }

    private SyncClient(IPEndPoint remoteEndPoint) : base(remoteEndPoint) { }


    /// <summary>
    /// Create a new instance of <c>SyncClient</c> with the specified remote end point.
    /// </summary>
    /// <param name="remoteEndPoint">The remote end point to connect to.</param>
    /// <returns>The new instance of <c>SyncClient</c>.</returns>
    /// <exception cref="BlueProtocolNetworkException">Thrown when the host is null, the port is out of range or there is a socket error.</exception>
    public static SyncClient Create(IPEndPoint remoteEndPoint)
    {
        var client = new SyncClient(remoteEndPoint);
        return client;
    }


    protected override void OnStart()
    {
        var thread = new Thread(ReceiveLoop);
        thread.Start();
        this.threads.Add(thread);
    }


    private object ReceiveData()
    {
        try {
            // ReSharper disable once InconsistentlySynchronizedField
            var message = Message.Receive(this.networkStream);
            return message.Deserialize();
        } catch (Exception e) when (e is IOException || e is ObjectDisposedException) {
            return null;
        }
    }


    private void ReceiveLoop()
    {
        while (this.IsConnected) {
            var data = ReceiveData();
            if (data == null)
                continue;

            RegisterRequest();
            ApplyRateLimit();

            if (data is not PingEvent)
                this.messages.Enqueue(data);
        }
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
    public void ProcessAll()
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


    /// <summary>
    /// Process one waiting request, response or event in the same thread.
    /// It processes the oldest message in the queue.
    /// </summary>
    /// <exception cref="BlueProtocolControllerException"></exception>
    public void ProcessOne()
    {
        var message = this.messages.Dequeue();
        if (message == null)
            return;

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