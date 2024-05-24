using System.Collections.Generic;


namespace BlueProtocol.Network.Messages
{
    public class MessageQueue
    {
        private readonly List<object> messages = new List<object>();


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
    }
}