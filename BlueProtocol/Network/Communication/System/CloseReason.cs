namespace BlueProtocol.Network.Communication.System;


public enum CloseCode
{
    ConnectionRefused,
    InternalError,
    RateLimited,
    LifetimeExceeded,
    Timeout,
    Custom
}


public class CloseReason
{
    public required CloseCode SystemCode { get; init; }
    public required int CustomCode { get; init; }
    public required string Message { get; init; }


    internal static CloseReason ConnectionRefused(string message = "Connection refused")
    {
        return new CloseReason { SystemCode = CloseCode.ConnectionRefused, CustomCode = -1, Message = message };
    }


    internal static CloseReason InternalError(string message = "Internal error")
    {
        return new CloseReason { SystemCode = CloseCode.InternalError, CustomCode = -1, Message = message };
    }


    internal static CloseReason RateLimited(string message = "Rate limited")
    {
        return new CloseReason { SystemCode = CloseCode.RateLimited, CustomCode = -1, Message = message };
    }


    internal static CloseReason Timeout(string message = "Timed out")
    {
        return new CloseReason { SystemCode = CloseCode.Timeout, CustomCode = -1, Message = message };
    }


    internal static CloseReason LifeTimeExceeded(string message = "Lifetime exceeded")
    {
        return new CloseReason { SystemCode = CloseCode.LifetimeExceeded, CustomCode = -1, Message = message };
    }


    public static CloseReason Custom(string message)
    {
        return new CloseReason { SystemCode = CloseCode.Custom, CustomCode = 0, Message = message };
    }


    public static CloseReason Custom(int customCode, string message)
    {
        return new CloseReason { SystemCode = CloseCode.Custom, CustomCode = customCode, Message = message };
    }


    public static CloseReason NoReason()
    {
        return new CloseReason { SystemCode = CloseCode.Custom, CustomCode = 0, Message = "No reason" };
    }


    public override string ToString()
    {
        return $"[{this.SystemCode}] {this.Message}";
    }
}