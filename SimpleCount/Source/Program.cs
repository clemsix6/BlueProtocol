using System.Net;
using BlueProtocol.Controllers;
using BlueProtocol.Network.Communication.Requests;
using BlueProtocol.Network.Communication.System;
using BlueProtocol.Network.Sockets.Clients;
using BlueProtocol.Network.Sockets.Servers;


internal class Program
{
    private static void Main(string[] args)
    {
        var task1 = Task.Run(RunServer);
        var task2 = Task.Run(RunClient);

        Task.WaitAll(task1, task2);
    }


    private static void RunServer()
    {
        var server = new BlueServer<AsyncClient>(5055);
        server.AddController(new ServerController());
        server.Start();

        server.OnClientConnectedEvent += client => Console.WriteLine($"Client connected: {client.RemoteEndPoint}");
        server.OnClientDisconnectedEvent += (client, reason) => {
            Console.WriteLine($"Client disconnected: {client.RemoteEndPoint} ({reason})");
            server.Dispose();
        };
    }


    private static void RunClient()
    {
        Thread.Sleep(1000);

        var client = new AsyncClient(new IPEndPoint(IPAddress.Loopback, 5055));
        client.Start();

        for (var i = 0; i < 100; i++) {
            var countRequest = new CountRequest { Count = i };
            client.Send(countRequest);
            Console.WriteLine($"Sent: {i}");
            countRequest.WaitResult();
        }

        client.Close(CloseReason.Custom("Done"));
    }
}


internal class CountRequest : Request
{
    public required int Count { get; init; }
}


internal class CountResponse : Response
{
}


internal class ServerController : Controller
{
    [OnRequest]
    public Response OnMessageReceived(AsyncClient client, CountRequest message)
    {
        Console.WriteLine($"Received: {message.Count} from {client.RemoteEndPoint}");
        return new CountResponse();
    }
}