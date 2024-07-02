using Newtonsoft.Json;


namespace BlueProtocol.Network.Requests;


public class Request
{
    [JsonProperty] internal string RequestId { get; set; }
    [JsonIgnore] private List<Action<Response>> OnResponseEvent { get; } = [];


    internal void OnResponse(Response response)
    {
        foreach (var action in OnResponseEvent)
            action(response);
    }


    /// <summary>
    /// Add an action to be executed when a response is received.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void OnResponse(Action<Response> action)
    {
        OnResponseEvent.Add(action);
    }


    /// <summary>
    /// Wait for a response.
    /// </summary>
    public void Wait()
    {
        var received = false;
        OnResponseEvent.Add(_ => received = true);

        while (!received)
            Thread.Sleep(1);
    }


    /// <summary>
    /// Wait for a response with a timeout.
    /// </summary>
    /// <param name="timeout">The timeout in milliseconds.</param>
    /// <returns>True if the response was received, false otherwise.</returns>
    public bool Wait(int timeout)
    {
        var received = false;
        OnResponseEvent.Add(_ => received = true);

        var start = Environment.TickCount64;
        while (!received) {
            if (Environment.TickCount64 - start >= timeout)
                return false;
            Thread.Sleep(1);
        }

        return true;
    }
}