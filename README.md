# 🔵 BlueProtocol

BlueProtocol is a C# library designed to provide a high-level interface for TCP communication, 
particularly recommended in domains where speed is a critical factor. It offers a lightweight, 
bidirectional request system that maintains a persistent connection between peers to minimize response times. 
The library is currently being developed to function in a decentralized network.

## ❓ Why BlueProtocol and not HTTP?

HTTP, while widely used, is not optimized for real-time communication required in many applications. 
BlueProtocol provides a more efficient, 
low-latency communication mechanism tailored for interactive and time-sensitive applications. 
By maintaining persistent connections and using lightweight requests and events, 
BlueProtocol reduces overhead and improves responsiveness.

## ⚙️ How does BlueProtocol work?

BlueProtocol allows communication between clients and servers through two main components: `Request` and `Event`. 
Clients send `Request` objects for specific actions to the server, 
which processes these requests and sends back `Response` objects. 
`Event` objects are used by the server to notify clients of changes, ensuring all clients remain synchronized.

## 🔄 Async vs. Sync: Differences and Recommendations

- **AsyncClient**: Used on the server-side, 
executes code asynchronously upon receiving a request using `Task.Run` to avoid blocking the main thread.
- **SyncClient**: Used on the client-side, 
stores incoming requests in a thread-safe list and processes them during the `Update` call.

### 💡 Recommendation:

Use `AsyncClient` for server-side operations to handle multiple client requests efficiently without blocking. 
On the client-side, use `SyncClient` to ensure smooth integration with single-threaded programs like a video game, 
or `AsyncClient` to handle multiple requests simultaneously.

## 🕹️ What is a Controller and How Does it Work?

A `Controller` is a class that handles specific types of requests and events. It contains methods marked with the
`[Route]` attribute to process incoming messages. When a request or event is received, 
the corresponding method in the controller is invoked to handle it.

---

## 📦 Installation

To install the BlueProtocol library, follow these steps:

1. **Using .NET CLI**: Run the following command to add the BlueProtocol package to your project.

    ```sh
    dotnet add package BlueProtocol --version latest
    ```

2. **Using GitHub Releases**: Visit the [Releases](https://github.com/clemsix6/BlueProtocol/releases) page on the BlueProtocol GitHub repository. Download the latest release package and follow the instructions provided in the release notes to add it to your project manually.

These methods ensure that you have the latest version of BlueProtocol installed and ready to use in your .NET project.

---

## 📚 Example Projects

### SimpleCount
**SimpleCount** demonstrates the use of the **BlueProtocol** library for TCP communication between a server and a client. The server listens for incoming messages and logs each received message, while the client sends a series of count requests to the server.

[SimpleCount Documentation](SimpleCount/README.md)

### SimpleRepeat
**SimpleRepeat** demonstrates the use of the **BlueProtocol** library for TCP communication between multiple "bots". Each bot connects directly to other bots, sends and receives messages, and prints the received messages to the console via a port scan starting from the `StartingPort` defined in the configuration.

[SimpleRepeat Documentation](SimpleRepeat/README.md)