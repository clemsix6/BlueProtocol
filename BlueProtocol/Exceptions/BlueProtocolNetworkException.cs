namespace BlueProtocol.Exceptions;


public class BlueProtocolNetworkException : BlueProtocolException
{
    public BlueProtocolNetworkException(string message) : base(message) { }

    public BlueProtocolNetworkException(string message, Exception innerException) : base(message, innerException) { }
}