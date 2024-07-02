using BlueProtocol.Network.Sockets.Clients;


namespace BlueProtocol.Network.Sockets.Servers;


public class ServerShield
{
    /// <summary>
    /// The maximum number of connections per second.
    /// </summary>
    public int MaxConnectionsPerSecond { get; init; } = 1;
    internal List<long> ConnectionTimesSecond { get; } = [];

    /// <summary>
    /// The maximum number of connections per minute.
    /// </summary>
    public int MaxConnectionsPerMinute { get; init; } = 10;
    internal List<long> ConnectionTimesMinute { get; } = [];


    /// <summary>
    /// The default shield for clients.
    /// </summary>
    public ClientShield DefaultClientShield { get; init; } = new();
}