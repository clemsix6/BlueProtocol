using System;
using System.Collections.Generic;


namespace BlueProtocol.Network
{
    public class ClientMemory<T>
    {
        private readonly List<T> memory = new List<T>();
        private readonly Dictionary<T, DateTime> timedMemory = new Dictionary<T, DateTime>();


        public List<T> Items => this.memory;


        public void Add(T item)
        {
            lock (this.memory)
                this.memory.Add(item);
            lock (this.timedMemory)
                this.timedMemory.Add(item, DateTime.Now);
        }


        public void Remove(T item)
        {
            lock (this.memory)
                this.memory.Remove(item);
            lock (this.timedMemory)
                this.timedMemory.Remove(item);
        }


        public List<T> GetTimedOutItems(int seconds)
        {
            var result = new List<T>();

            lock (this.timedMemory)
            {
                foreach (var pair in this.timedMemory)
                {
                    if ((DateTime.Now - pair.Value).TotalSeconds >= seconds)
                        result.Add(pair.Key);
                }
            }
            return result;
        }
    }
}