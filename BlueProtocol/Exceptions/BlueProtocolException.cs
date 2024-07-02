namespace BlueProtocol.Exceptions;


public class BlueProtocolException : Exception
{
    public BlueProtocolException(string message) : base(message) { }

    public BlueProtocolException(string message, Exception innerException) : base(message, innerException) { }
}