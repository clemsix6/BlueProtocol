namespace BlueProtocol.Exceptions;


public class BlueProtocolTimeoutException : BlueProtocolException
{
    public BlueProtocolTimeoutException(string message) : base(message) { }
}