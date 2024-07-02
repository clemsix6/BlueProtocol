namespace BlueProtocol.Exceptions;


public class BlueProtocolConnectionRefused : BlueProtocolNetworkException
{
    public BlueProtocolConnectionRefused(string message) : base(message) { }

    public BlueProtocolConnectionRefused(string message, Exception innerException) : base(message, innerException) { }
}