using BlueProtocol.Network.Communication.Requests;


namespace SimpleRepeat.Requests;


public class ConnectionRequest : Request
{
    public required int Port { get; init; }
}