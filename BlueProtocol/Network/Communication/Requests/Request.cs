using Newtonsoft.Json;


namespace BlueProtocol.Network.Communication.Requests;


public class Request<TResponse> : ARequest where TResponse : Response
{
    [JsonIgnore] private List<Action<TResponse>> OnResponseEvent { get; } = [];


    internal override void OnResponse(Response response)
    {
        foreach (var action in OnResponseEvent)
            action((TResponse) response);
    }


    /// <summary>
    /// Add an action to be executed when a response is received.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void OnResponse(Action<TResponse> action)
    {
        OnResponseEvent.Add(action);
    }


    /// <summary>
    /// Wait for a response.
    /// </summary>
    public TResponse WaitResult()
    {
        TResponse response = null;
        OnResponseEvent.Add(r => response = r);

        while (response == null)
            Thread.Sleep(1);
        return response;
    }


    /// <summary>
    /// Wait for a response with a timeout.
    /// </summary>
    /// <param name="timeout">The timeout in milliseconds.</param>
    /// <returns>Returns the response or null if the timeout is reached.</returns>
    public TResponse WaitResult(int timeout)
    {
        TResponse response = null;
        OnResponseEvent.Add(r => response = r);

        var start = Environment.TickCount64;
        while (response == null && Environment.TickCount64 - start < timeout)
            Thread.Sleep(1);
        return response;
    }
}