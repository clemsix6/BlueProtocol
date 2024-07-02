namespace BlueProtocol.Network.Sockets.Clients;


/// <summary>
/// The <c>Shield</c> class models the timeouts and rate limits for a client.
/// </summary>
public class ClientShield
{
    // Timeouts

    /// <summary>
    /// The maximum lifetime of the client in milliseconds.
    /// </summary>
    public int LifeTime { get; init; } = -1;
    internal long StartTime { get; set; }

    /// <summary>
    /// The maximum time to wait for a response in milliseconds.
    /// </summary>
    public int ResponseTimeout { get; init; } = 5000;


    // Rate limits

    /// <summary>
    /// The maximum number of requests per second.
    /// </summary>
    public int MaxRequestsPerSecond { get; init; } = 10;
    internal List<long> RequestTimesSecond { get; } = [];

    /// <summary>
    /// The maximum number of requests per minute.
    /// </summary>
    public int MaxRequestsPerMinute { get; init; } = 600;
    internal List<long> RequestTimesMinute { get; } = [];
}