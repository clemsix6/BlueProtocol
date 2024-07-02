namespace BlueProtocol.Network.Messages;


internal class MessageQueue
{
    private readonly List<object> messages = [];


    public void Enqueue(object message)
    {
        lock(this.messages)
            this.messages.Add(message);
    }



    public object[] DequeueAll()
    {
        object[] messages;
        lock(this.messages) {
            messages = this.messages.ToArray();
            this.messages.Clear();
        }
        return messages;
    }


    public object Dequeue()
    {
        object message;
        lock(this.messages) {
            if (this.messages.Count == 0)
                return null;
            message = this.messages[0];
            this.messages.RemoveAt(0);
        }
        return message;
    }
}