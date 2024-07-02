using Newtonsoft.Json;


namespace BlueProtocol.Network.Requests;


public class Response
{
    [JsonProperty] public string RequestId { get; internal set; }
    [JsonProperty] public int Code { get; internal set; }
    [JsonProperty] public string Message { get; internal set; }

    public bool Success => this.Code == 0;


    public Response() { }


    public Response(int code)
    {
        this.RequestId = null;
        this.Code = code;
        this.Message = null;
    }


    public Response(int code, string message)
    {
        this.RequestId = null;
        this.Code = code;
        this.Message = message;
    }


    internal Response(Request request, int code, string message)
    {
        this.RequestId = request.RequestId;
        this.Code = code;
        this.Message = message;
    }


    [JsonConstructor]
    private Response(string requestId, int code, string message)
    {
        this.RequestId = requestId;
        this.Code = code;
        this.Message = message;
    }


    public static Response Ok(Response response)
    {
        response.Code = 0;
        response.Message = string.Empty;
        return response;
    }


    public static Response Ok()
    {
        var response = new Response {
            Code = 0,
            Message = string.Empty
        };
        return response;
    }


    public static Response Error(Response response, int code = 1, string message = "")
    {
        response.Code = code;
        response.Message = message;
        return response;
    }


    public static Response Error(int code = 1, string message = "")
    {
        var response = new Response {
            Code = code,
            Message = message
        };
        return response;
    }
}