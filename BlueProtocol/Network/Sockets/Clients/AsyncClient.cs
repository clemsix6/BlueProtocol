using System.Net;
using System.Net.Sockets;
using BlueProtocol.Controllers;
using BlueProtocol.Exceptions;
using BlueProtocol.Network.Communication.Events;
using BlueProtocol.Network.Communication.Messages;
using BlueProtocol.Network.Communication.Requests;
using BlueProtocol.Network.Communication.System;


namespace BlueProtocol.Network.Sockets.Clients;


/// <summary>
/// Class <c>AsyncClient</c> models a client that processes messages asynchronously.
/// It automatically processes all incoming messages in a separate thread asynchronously.
/// </summary>
public class AsyncClient : BlueClient
{
    /// <summary>
    /// The request handler for the client. It is used to process incoming requests and events.
    /// </summary>
    public RequestHandler<AsyncClient> RequestHandler { get; }


    public AsyncClient(TcpClient tcpClient, ClientShield shield = null,
        RequestHandler<AsyncClient> requestHandler = null) :
        base(tcpClient, shield)
    {
        this.RequestHandler = requestHandler ?? new RequestHandler<AsyncClient>();
        RegisterSystemActions();
    }


    public AsyncClient(IPEndPoint remoteEndPoint, ClientShield shield = null,
        RequestHandler<AsyncClient> requestHandler = null) :
        base(remoteEndPoint, shield)
    {
        this.RequestHandler = requestHandler ?? new RequestHandler<AsyncClient>();
        RegisterSystemActions();
    }


    private void RegisterSystemActions()
    {
        this.RequestHandler.RegisterEventHandler<PingEvent>((_, _) => { });

        this.RequestHandler.RegisterRequestHandler<CloseRequest>((_, request) => {
            OnRemoteClose(request.Reason);
            return new CloseResponse();
        });
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


    private void UpdateRequest(ARequest request)
    {
        var result = this.RequestHandler.OnRequest(this, request, out var response);
        if (result && response is Response r) {
            Send(r);
            return;
        }

        throw new BlueProtocolControllerException(
            $"No controller found for request {request.GetType().FullName}");
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
        if (this.RequestHandler.OnEvent(this, ev))
            return;

        throw new BlueProtocolControllerException($"No controller found for event {ev.GetType().FullName}");
    }


    private void ReceiveLoop()
    {
        while (true) {
            var data = ReceiveData();
            if (data == null)
                continue;
            if (!this.IsRunning)
                break;

            RegisterRequest();
            ApplyRateLimit();

            switch (data) {
                case ARequest request:
                    Task.Run(() => UpdateRequest(request));
                    continue;

                case Response response:
                    Task.Run(() => UpdateResponse(response));
                    continue;

                case Event ev:
                    Task.Run(() => UpdateEvent(ev));
                    continue;

                default:
                    throw new BlueProtocolNetworkException($"Invalid data {data}");
            }
        }
    }
}