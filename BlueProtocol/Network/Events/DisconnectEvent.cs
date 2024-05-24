namespace BlueProtocol.Network.Events
{
    public class DisconnectEvent : Event
    {
        public string Reason { get; }


        public DisconnectEvent(string reason)
        {
            this.Reason = reason;
        }
    }
}