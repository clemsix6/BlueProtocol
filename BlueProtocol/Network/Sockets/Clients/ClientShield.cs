namespace BlueProtocol.Network.Sockets.Clients;


/// <summary>
/// The <c>Shield</c> class models the timeouts and rate limits for a client.
/// </summary>
public class ClientShield : ICloneable
{
    // Timeouts

    /// <summary>
    /// The maximum lifetime of the client in milliseconds.
    /// </summary>
    public int LifeTime { get; set; } = -1;
    internal long StartTime { get; set; }

    /// <summary>
    /// The maximum time to wait for a response in milliseconds.
    /// </summary>
    public int ResponseTimeout { get; set; } = 5000;


    // Rate limits

    /// <summary>
    /// The maximum number of requests per second.
    /// </summary>
    public int MaxRequestsPerSecond { get; set; } = 10;
    internal List<long> RequestTimesSecond { get; } = [];

    /// <summary>
    /// The maximum number of requests per minute.
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 600;
    internal List<long> RequestTimesMinute { get; } = [];


    public object Clone()
    {
        return new ClientShield {
            LifeTime = this.LifeTime,
            ResponseTimeout = this.ResponseTimeout,
            MaxRequestsPerSecond = this.MaxRequestsPerSecond,
            MaxRequestsPerMinute = this.MaxRequestsPerMinute
        };
    }
}