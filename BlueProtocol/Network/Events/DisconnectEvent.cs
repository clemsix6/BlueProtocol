namespace BlueProtocol.Network.Events;


public class DisconnectEvent : Event
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string Reason { get; }


    public DisconnectEvent(string reason)
    {
        this.Reason = reason;
    }
}