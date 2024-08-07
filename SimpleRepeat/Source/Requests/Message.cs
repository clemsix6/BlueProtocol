using BlueProtocol.Network.Communication.Events;


namespace SimpleRepeat.Requests;


public class Message : Event
{
    public required int SenderPort { get; init; }
    public required string Content { get; init; }
}