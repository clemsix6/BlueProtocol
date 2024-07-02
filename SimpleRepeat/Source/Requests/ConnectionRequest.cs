using BlueProtocol.Network.Requests;


namespace SimpleRepeat.Requests;


public class ConnectionRequest : Request
{
    public required int Port { get; init; }
}