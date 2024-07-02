using BlueProtocol.Network.Communication.Requests;


namespace BlueProtocol.Network.Communication.System;


internal class CloseRequest : Request<CloseResponse>
{
    public required CloseReason Reason { get; init; }
}

internal class CloseResponse : Response
{
}