using System.Net;
using BlueProtocol.Controllers;
using BlueProtocol.Exceptions;
using BlueProtocol.Network;
using SimpleRepeat.Requests;


namespace SimpleRepeat;


public class Bot : Controller
{
    private readonly int port;
    private readonly ConsoleColor color;
    private readonly string[] sentences;
    private readonly Semaphore outputSemaphore;

    private readonly Dictionary<int, AsyncClient> clients;
    private readonly AsyncServer server;

    public Thread Thread { get; }


    public Bot(int port, ConsoleColor color, string[] sentences, Semaphore outputSemaphore)
    {
        this.port = port;
        this.color = color;
        this.sentences = sentences;
        this.outputSemaphore = outputSemaphore;

        this.clients = new Dictionary<int, AsyncClient>();
        this.server = new AsyncServer(port);
        this.Thread = new Thread(this.Loop);

        this.server.AddController(this);
    }


    private void Print(string prefix, string message)
    {
        /*
        // Use the semaphore to control access to the console output
        this.outputSemaphore.WaitOne();
        Console.Write(prefix);
        Console.ForegroundColor = this.color;
        Console.WriteLine($"[{this.port}] {message}");
        Console.ResetColor();
        this.outputSemaphore.Release();
        */
    }


    private void Search()
    {
        var searchingPort = Config.StartingPort - 1;

        while (searchingPort < Config.StartingPort + Config.BotCount - 1) {
            searchingPort++;
            if (searchingPort == this.port || this.clients.ContainsKey(searchingPort))
                continue;
            Print("! ", $"Searching for [{searchingPort}]");

            try {
                // Connect to the server on the searching port
                var client = (AsyncClient)this.server.Connect(new IPEndPoint(IPAddress.Loopback, searchingPort));

                // Create the connection request
                var connectionRequest = new ConnectionRequest { Port = this.port };
                // Set the response handler
                connectionRequest.OnResponse(response => {
                    // Cast the response to a connection response
                    var connectionResponse = (ConnectionResponse)response;

                    // If the connection was not authorized, dispose the client
                    if (!connectionResponse.Authorized) {
                        client.Close();
                        return;
                    }

                    // Add the client to the list of clients
                    lock (this.clients)
                        this.clients.Add(connectionResponse.Port, client);

                    this.Print("[+] ", $"Connection authorized by [{connectionResponse.Port}]");
                    Console.WriteLine($"Clients: {this.clients.Count}");
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

        this.Print("! ", "Started");
    }


    private void SendRandomMessage()
    {
        var message = new Message {
            SenderPort = this.port,
            Content = this.sentences[Random.Shared.Next(this.sentences.Length)],
        };

        Print("> ", $"Sending message: {message.Content}");

        lock (this.clients) {
            foreach (var client in this.clients) {
                client.Value.Send(message);
            }
        }
    }


    private void Loop()
    {
        var nextSearchTime = Environment.TickCount64 + 1000;
        var nextSendMessageTime = Environment.TickCount64 + 1000;

        while (true) {
            Thread.Sleep(Random.Shared.Next(50, 100));

            lock (this.clients) {
                if (Environment.TickCount64 >= nextSearchTime && this.clients.Count < Config.BotCount) {
                    nextSearchTime = Environment.TickCount64 + Random.Shared.Next(1000, 3000);
                    Search();
                }
            }

            if (Environment.TickCount64 >= nextSendMessageTime) {
                nextSendMessageTime = Environment.TickCount64 + Random.Shared.Next(1000, 10000);
                SendRandomMessage();
            }
        }
    }


    // --- Event handlers ---


    [OnEvent]
    private void OnReceiveMessage(Message message)
    {
        Print("< ", $"Received message from {message.SenderPort}: {message.Content}");
    }


    [OnRequest]
    private ConnectionResponse OnConnectionRequest(AsyncClient client, ConnectionRequest request)
    {
        lock (this.clients) {
            if (!this.clients.TryAdd(request.Port, client)) {
                Print("[!] ", $"Connection refused for [{request.Port}]");
                client.Close();
                return new ConnectionResponse { Authorized = false, Port = this.port };
            }

            Console.WriteLine($"Clients: {this.clients.Count}");
            Print("[+] ", $"Connection authorized for [{request.Port}]");
            return new ConnectionResponse { Authorized = true, Port = this.port };
        }
    }
}