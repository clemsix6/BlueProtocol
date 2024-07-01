using BlueProtocol.Network.Requests;


namespace SimpleRepeat.Requests;


public class ConnectionResponse : Response
{
    public required bool Authorized { get; init; }
    public required int Port { get; init; }
}