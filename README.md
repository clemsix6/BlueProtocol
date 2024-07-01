# 🔵 BlueProtocol

BlueProtocol is a C# library designed to provide a high-level interface for TCP communication, particularly recommended
in domains where speed is a critical factor, not necessarily limited to video games. It offers a lightweight, 
bidirectional request system that maintains a persistent connection between peers to minimize response times. 
The library is currently being developed to function in a decentralized network.

## ❓ Why BlueProtocol and not HTTP?

HTTP, while widely used, is not optimized for real-time communication required in many video games. 
BlueProtocol provides a more efficient, low-latency communication mechanism tailored for interactive applications. 
By maintaining persistent connections and using lightweight requests and events, BlueProtocol reduces overhead and 
improves responsiveness.

## ⚙️ How does BlueProtocol work?

BlueProtocol allows communication between clients and servers through two main components: requests and events. 
Clients send requests for specific actions to the server, which processes these requests and sends back responses 
indicating success or failure. Events are used by the server to notify clients of changes, ensuring all clients 
remain synchronized.

## 🔄 Async vs. Sync: Differences and Recommendations

- **AsyncClient**: Used on the server-side, executes code asynchronously upon receiving a request using `Task.Run` 
- to avoid blocking the main thread.
- **SyncClient**: Used on the client-side, stores incoming requests in a thread-safe list and processes them during the 
- `Update` call, making it suitable for Unity’s main thread.

### 💡 Recommendation:
Use `AsyncClient` for server-side operations to handle multiple client requests efficiently without blocking. 
Use`SyncClient` on the client-side to ensure smooth integration with Unity’s single-threaded update loop.

## 🔗 Communication Between Peers

1. **Requests**: A client sends a request to the server to perform an action. 
2. The server processes the request and returns a response.
2. **Events**: The server sends events to clients to notify them of changes or updates (e.g., a game element moving). 
3. Clients handle these events to stay synchronized.

## 📤 Requests vs. Events

- **Request**: A message sent from a client to the server that expects a response. Used for actions where confirmation is needed.
- **Event**: A one-way message sent from the server to clients. No response is expected. Used for notifications or updates.

## 🕹️ What is a Controller and How Does it Work?

A `Controller` is a class that handles specific types of requests and events. 
It contains methods marked with `[OnRequest]` or `[OnEvent]` attributes to process incoming messages. 
When a request or event is received, the corresponding method in the controller is invoked to handle it.

---

## Example Usage in C#

### Server-Side Implementation

```csharp
internal class Program
{
    private static void RunServer()
    {
        var server = new SyncServer(5855);
        server.Start();
        Console.WriteLine("[Server] Listening for clients");

        server.OnClientConnectedEvent += client => {
            client.OnDisconnectedEvent += (c, info) => {
                Console.WriteLine("[Server] Client disconnected");
                server.Dispose();
            };

            Console.WriteLine("[Server] Client connected");

            var controller = new PlayerController();
            client.AddController(controller);

            Console.WriteLine("[Server] Listening for messages");
            while (client.IsConnected)
                client.Update();
        };

        while (server.IsRunning)
            Thread.Sleep(1000);
    }
}
```

### Client-Side Implementation

```csharp
private static void RunClient()
{
    Thread.Sleep(1000);

    Console.WriteLine("[Client] Connecting to server");
    var client = SyncClient.Connect("127.0.0.1", 5855);
    Console.WriteLine("[Client] Sending player join request");
    var request = new PlayerJoinRequest("Player1");

    // Player Join Response
    request.OnResponse(response => {
        if (response is PlayerJoinResponse playerJoinResponse)
            Console.WriteLine(
                $"[Client] Player joined: {playerJoinResponse.Name}, Level: {playerJoinResponse.Level}");

        for (var i = 0; i < 100; i++) {
            var info = new UtilEvent(i);
            Console.WriteLine($"[Client] Sending player info: {info}");
            client.Send(info);
        }
        client.Dispose();
    });

    client.Send(request);
    Console.WriteLine("[Client] Sent");

    while (client.IsConnected)
        client.Update();
    Console.WriteLine("[Client] Disconnected");
}

public static void Main()
{
    new Thread(RunClient).Start();
    RunServer();
}
```

### PlayerJoinRequest and PlayerJoinResponse

```csharp
public class PlayerJoinResponse : Response
{
    public string Name { get; }
    public int Level { get; }

    public PlayerJoinResponse(string name, int level)
    {
        Name = name;
        Level = level;
    }
}

public class PlayerJoinRequest : Request
{
    public string Name { get; }

    public PlayerJoinRequest(string name)
    {
        Name = name;
    }
}
```

### Controller Implementation

```csharp
public class PlayerController : Controller
{
    [OnRequest]
    private PlayerJoinResponse OnPlayerJoin(SyncClient client, PlayerJoinRequest request)
    {
        Console.WriteLine($"[Server] Received player join request: {request.Id}");
        Console.WriteLine($"[Server] Player joined: {request.Name}");
        return new PlayerJoinResponse(request.Name, 5);
    }

    [OnEvent]
    private void OnPlayerInfo(SyncClient client, UtilEvent @event)
    {
        Console.WriteLine($"[Server] Received player info: {@event.Value}");
    }
}
```

---

## 📁 Example Project: SimpleRepeat

The `SimpleRepeat` project demonstrates how to use the `BlueProtocol` library to create a network of bots that
communicate over TCP. This example showcases the basic functionalities and interactions possible with `BlueProtocol`.

### 🗂️ Project Structure
- **Program.cs**: Initializes and runs the bots.
- **Bot.cs**: Manages the bot behavior, including connecting to other bots, sending messages, and handling events.
- **Config.cs**: Contains configuration constants.
- **Requests**: Contains request and response classes used for bot communication.

For more details and to view the complete example, refer to the [SimpleRepeat directory](https://github.com/clemsix6/BlueProtocol/tree/master/SimpleRepeat) in the repository.