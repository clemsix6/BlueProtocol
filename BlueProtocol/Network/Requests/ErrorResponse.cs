namespace BlueProtocol.Network.Requests;


public class ErrorResponse : Response
{
    public ErrorResponse(string message) : base(null, -1, message) { }

    public ErrorResponse(int code, string message) : base(null, code, message) { }
}