# SimpleCount Documentation

## 📄 Introduction
**SimpleCount** demonstrates the use of the **BlueProtocol** library for TCP communication between a server and a client.
The server listens for incoming messages and logs each received message, while the client sends a series of count requests to the server.


## 🔍 Overview
1. **Run Server**: Initializes and starts the server.
2. **Run Client**: Initializes and starts the client after a delay, sending count requests.
3. **Message Handling**: Server logs each received message and client logs each sent message.

## 💻 Code Structure

### Class `Program`
- **Main Method**:
    - Executes `RunServer` and `RunClient` methods concurrently.

- **RunServer Method**:
    - Initializes the server on port 5055.
    - Adds a controller to handle incoming requests.
    - Starts the server and listens for client connections and disconnections.

- **RunClient Method**:
    - Delays startup to ensure the server is running.
    - Initializes the client and connects to the server.
    - Sends a series of count requests to the server and waits for each response.

### Class `CountRequest`
- Defines a request containing a count value.
- **Properties**:
    - `Count`: Integer representing the count value to be sent.

### Class `CountResponse`
- Defines a response with no additional properties.

### Class `ServerController`
- Inherits from `Controller`.
- Manages logic for handling incoming `CountRequest` messages.
- **Methods**:
    - `OnMessageReceived`: Logs the received count value and returns a `CountResponse`.

## 🛠️ How It Works

1. **Server Initialization**:
- `Program.RunServer` initializes an `AsyncServer` on port 5055.
- Adds `ServerController` to handle requests.
- Starts the server and logs client connection and disconnection events.

2. **Client Initialization**:
- `Program.RunClient` delays for 1 second to ensure server readiness.
- Initializes an `AsyncClient` and connects to the server on localhost, port 5055.
- Sends 100 sequential `CountRequest` messages, logging each sent message.

3. **Message Handling**:
- `ServerController.OnMessageReceived` logs each received count value from the client.
- Returns a `CountResponse` for each request.
- Server logs the connected and disconnected clients, including their remote endpoints.

## 📊 Example Output
```
Client connected: 127.0.0.1:51716
Sent: 0
Received: 0 from 127.0.0.1:51716
Sent: 1
Received: 1 from 127.0.0.1:51716
...
Sent: 99
Received: 99 from 127.0.0.1:51716
Client disconnected: 127.0.0.1:51716
```

## 🏁 Conclusion
This example project illustrates the basic usage of **BlueProtocol** for setting up a TCP server-client communication. It demonstrates handling of client connections, sending and receiving messages, and managing asynchronous operations within the network library.