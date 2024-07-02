namespace BlueProtocol.Network;


public class ClientMemory<T>
{
    private readonly Dictionary<T, DateTime> timedMemory = new();


    public List<T> Items { get; } = [];


    public void Add(T item)
    {
        lock (this.Items)
            this.Items.Add(item);
        lock (this.timedMemory)
            this.timedMemory.Add(item, DateTime.Now);
    }


    public void Remove(T item)
    {
        lock (this.Items)
            this.Items.Remove(item);
        lock (this.timedMemory)
            this.timedMemory.Remove(item);
    }


    public List<T> GetTimedOutItems(int seconds)
    {
        var result = new List<T>();

        lock (this.timedMemory) {
            foreach (var pair in this.timedMemory) {
                if ((DateTime.Now - pair.Value).TotalSeconds >= seconds)
                    result.Add(pair.Key);
            }
        }

        return result;
    }
}