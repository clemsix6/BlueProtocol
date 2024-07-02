using Newtonsoft.Json;


namespace BlueProtocol.Network.Communication.Requests;


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
    public Response WaitResult()
    {
        Response response = null;
        OnResponseEvent.Add(r => response = r);

        while (response == null)
            Thread.Sleep(1);
        return response;
    }


    /// <summary>
    /// Wait for a response with a timeout.
    /// </summary>
    /// <typeparam name="T">The type of response to wait for.</typeparam>
    /// <returns>Returns the response</returns>
    public T WaitResult<T>() where T : Response
    {
        T response = null;
        OnResponseEvent.Add(r => response = (T)r);

        while (response == null)
            Thread.Sleep(1);
        return response;
    }


    /// <summary>
    /// Wait for a response with a timeout.
    /// </summary>
    /// <param name="timeout">The timeout in milliseconds.</param>
    /// <returns>Returns the response or null if the timeout is reached.</returns>
    public Response WaitResult(int timeout)
    {
        Response response = null;
        OnResponseEvent.Add(r => response = r);

        var start = Environment.TickCount64;
        while (response == null && Environment.TickCount64 - start < timeout)
            Thread.Sleep(1);
        return response;
    }


    /// <summary>
    /// Wait for a response with a timeout.
    /// </summary>
    /// <param name="timeout">The timeout in milliseconds.</param>
    /// <typeparam name="T">The type of response to wait for.</typeparam>
    /// <returns>Returns the response or null if the timeout is reached.</returns>
    public T WaitResult<T>(int timeout) where T : Response
    {
        T response = null;
        OnResponseEvent.Add(r => response = (T)r);

        var start = Environment.TickCount64;
        while (response == null && Environment.TickCount64 - start < timeout)
            Thread.Sleep(1);
        return response;
    }
}