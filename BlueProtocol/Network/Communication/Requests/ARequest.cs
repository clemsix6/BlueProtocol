using Newtonsoft.Json;


namespace BlueProtocol.Network.Communication.Requests;


public abstract class ARequest
{
    [JsonProperty] internal string RequestId { get; set; }

    internal abstract void OnResponse(Response response);
}