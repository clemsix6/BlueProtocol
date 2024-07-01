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


    public void OnResponse(Action<Response> action)
    {
        OnResponseEvent.Add(action);
    }


    public void Wait()
    {
        var received = false;
        OnResponseEvent.Add(_ => received = true);

        while (!received)
            Thread.Sleep(1);
    }
}