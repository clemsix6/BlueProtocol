using BlueProtocol.Network.Communication.Requests;


namespace SimpleRepeat.Requests;


public class ConnectionRequest : Request<ConnectionResponse>
{
    public required int Port { get; init; }
}


public class ConnectionResponse : Response
{
    public required bool Authorized { get; init; }
    public required int Port { get; init; }
}