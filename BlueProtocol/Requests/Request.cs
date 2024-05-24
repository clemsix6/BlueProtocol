using System;
using System.Collections.Generic;
using Newtonsoft.Json;


namespace BlueProtocol.Requests
{
    public class Request
    {
        [JsonProperty] public string Id { get; internal set; }
        [JsonIgnore] private List<Action<Response>> OnResponseEvent { get; } = new List<Action<Response>>();


        internal void OnResponse(Response response)
        {
            foreach (var action in OnResponseEvent)
                action(response);
        }


        internal bool IsWaitingForResponse()
        {
            return OnResponseEvent.Count > 0;
        }


        public void OnResponse(Action<Response> action)
        {
            OnResponseEvent.Add(action);
        }
    }
}