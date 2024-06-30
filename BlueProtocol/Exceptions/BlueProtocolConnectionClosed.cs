namespace BlueProtocol.Exceptions;


public class BlueProtocolConnectionClosed : BlueProtocolNetworkException
{
    public BlueProtocolConnectionClosed(string message) : base(message) { }

    public BlueProtocolConnectionClosed(string message, Exception innerException) : base(message, innerException) { }
}