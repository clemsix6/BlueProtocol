## SimpleRepeat Project Documentation

### Overview

The `SimpleRepeat` project demonstrates how to use the `BlueProtocol` library to create a network of bots that communicate via TCP. This example involves creating bots that read sentences from a file and exchange messages over a network.

### Components

#### 1. **Program Class**
- Reads sentences from `sentences.txt`.
- Initializes a list of bots using `CreateBots`.
- Starts the bots and waits for them to finish.

#### 2. **Config Class**
- Defines configuration constants:
    - `StartingPort`: Initial port number for the bots.
    - `SimulationDuration`: Duration of the simulation in milliseconds.

#### 3. **Bot Class**
- Inherits from `Controller`.
- Manages bot behavior including starting servers, connecting to other bots, sending messages, and handling events.

### Key Methods

#### `Program` Class
- `CreateBots(int count, string[] sentences, Semaphore outputSemaphore)`: Creates and configures the bots.
- `Main()`: Entry point for the application.

#### `Bot` Class
- `Start()`: Starts the server and the bot thread.
- `Loop()`: Main loop for the bot's activities, including searching for other bots and sending messages.
- `Search()`: Connects to other bots.
- `SendRandomMessage()`: Sends a random message to connected bots.
- `Print(string prefix, string message)`: Prints messages to the console with synchronization.
- `OnReceiveMessage(Message message)`: Event handler for received messages.
- `OnConnectionRequest(AsyncClient client, ConnectionRequest request)`: Handles incoming connection requests.

### Requests and Responses

#### `ConnectionRequest` Class
- Represents a connection request sent by a bot.
- Properties:
    - `Id`: Identifier of the bot.

#### `ConnectionResponse` Class
- Represents the response to a connection request.
- Properties:
    - `Authorized`: Indicates if the connection is authorized.
    - `Id`: Identifier of the bot.

#### `Message` Class
- Represents a message sent between bots.
- Properties:
    - `SenderId`: Identifier of the sender bot.
    - `Content`: The message content.

### Usage

1. Ensure `sentences.txt` is in the executable directory.
2. Run the application to see bots connecting and exchanging messages.
3. Observe the console output for connection statuses and messages.

### Example Console Output
![Screenshot of the output](Screenshot.png)

This documentation provides an overview and detailed information on how the `SimpleRepeat` project works, leveraging the `BlueProtocol` library for network communication between bots.