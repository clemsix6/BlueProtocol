using System.Net;
using BlueProtocol.Controllers;
using BlueProtocol.Exceptions;
using BlueProtocol.Network;
using SimpleRepeat.Requests;


namespace SimpleRepeat;


public class Bot : Controller
{
    private readonly string id;
    private readonly int port;
    private readonly ConsoleColor color;
    private readonly string[] sentences;
    private readonly Semaphore outputSemaphore;

    private readonly Dictionary<string, AsyncClient> clients;
    private readonly AsyncServer server;
    public Thread Thread { get; }


    public Bot(string id, int port, ConsoleColor color, string[] sentences, Semaphore outputSemaphore)
    {
        this.id = id;
        this.port = port;
        this.color = color;
        this.sentences = sentences;
        this.outputSemaphore = outputSemaphore;

        this.clients = new Dictionary<string, AsyncClient>();
        this.server = new AsyncServer(port);
        this.Thread = new Thread(this.Loop);

        this.server.AddController(this);
    }


    private void Print(string prefix, string message)
    {
        this.outputSemaphore.WaitOne();
        Console.Write(prefix);
        Console.ForegroundColor = this.color;
        Console.WriteLine($"[{this.id}] {message}");
        Console.ResetColor();
        this.outputSemaphore.Release();
    }


    private void Search()
    {
        var searchingPort = Config.StartingPort - 1;

        while (true) {
            searchingPort++;
            if (searchingPort == this.port)
                continue;

            try {
                // Connect to the server on the searching port
                var client = (AsyncClient)this.server.Connect(new IPEndPoint(IPAddress.Loopback, searchingPort));

                // Create the connection request
                var connectionRequest = new ConnectionRequest { Id = this.id };
                // Set the response handler
                connectionRequest.OnResponse(response => {
                    // Cast the response to a connection response
                    var connectionResponse = (ConnectionResponse)response;

                    // If the connection was not authorized, dispose the client
                    if (!connectionResponse.Authorized) {
                        client.Dispose();
                        return;
                    }

                    // Add the client to the list of clients
                    lock (this.clients)
                        this.clients.Add(connectionResponse.Id, client);

                    this.Print("[+] ", "Connection authorized by " + connectionResponse.Id);
                });

                // Send the connection request
                client.Send(connectionRequest);
            } catch (BlueProtocolNetworkException) {
                break;
            }
        }
    }


    public void Start()
    {
        Thread.Sleep(Random.Shared.Next(0, 100));

        this.server.Start();
        this.Thread.Start();

        this.Print("! ", "Started on port " + this.port);
    }


    private void SendRandomMessage()
    {
        var message = new Message {
            SenderId = this.id,
            Content = this.sentences[Random.Shared.Next(this.sentences.Length)],
        };
        var clients = this.server.GetClients();

        Print("> ", $"Sending message: {message.Content}");
        foreach (var client in clients)
            client.Send(message);
    }


    private void Loop()
    {
        var startTime = DateTime.Now;

        while ((DateTime.Now - startTime).TotalMilliseconds < Config.SimulationDuration) {
            Thread.Sleep(Random.Shared.Next(3000, 5000));
            Search();

            if ((DateTime.Now - startTime).TotalMilliseconds > 5000)
                SendRandomMessage();
        }
    }


    [OnEvent]
    public void OnReceiveMessage(Message message)
    {
        Print("< ", $"Received message from {message.SenderId}: {message.Content}");
    }


    [OnRequest]
    public ConnectionResponse OnConnectionRequest(AsyncClient client, ConnectionRequest request)
    {
        lock (this.clients) {
            var authorized = this.clients.TryAdd(request.Id, client);
            if (authorized)
                Print("[+] ", "Connection authorized for " + request.Id);
            return new ConnectionResponse { Authorized = authorized, Id = this.id };
        }
    }
}