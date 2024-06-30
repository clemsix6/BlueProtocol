using BlueProtocol.Network.Events;


namespace SimpleRepeat.Requests;


public class Message : Event
{
    public required string SenderId { get; init; }
    public required string Content { get; init; }
}