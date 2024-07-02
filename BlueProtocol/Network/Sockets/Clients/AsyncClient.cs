using System.Net;
using System.Net.Sockets;
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
    public AsyncClient(TcpClient tcpClient) : base(tcpClient) { }


    public AsyncClient(IPEndPoint remoteEndPoint) : base(remoteEndPoint) { }


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


    private void ProcessCloseEvent(CloseRequest closeRequest)
    {
        OnRemoteClose(closeRequest.Reason);
        var response = Response.Ok();
        response.RequestId = closeRequest.RequestId;
        Send(response);
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
        lock (this.controllers) {
            foreach (var controller in this.controllers.Items) {
                if (controller.OnEvent(this, ev))
                    return;
            }
        }

        throw new BlueProtocolControllerException($"No controller found for event {ev.GetType().FullName}");
    }


    private void ReceiveLoop()
    {
        while (this.IsConnected) {
            var data = ReceiveData();
            if (data == null)
                continue;

            RegisterRequest();
            ApplyRateLimit();

            switch (data) {
                case PingEvent:
                    continue;

                case CloseRequest closeEvent:
                    ProcessCloseEvent(closeEvent);
                    continue;

                case Request request:
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