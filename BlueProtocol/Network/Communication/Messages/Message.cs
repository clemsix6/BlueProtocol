using System.Net.Sockets;
using System.Text;
using BlueProtocol.Exceptions;
using Newtonsoft.Json;


namespace BlueProtocol.Network.Communication.Messages;


internal class Message
{
    private string Type { get; }
    private string Body { get; }


    private Message(string type, string body)
    {
        this.Type = type;
        this.Body = body;
    }


    public static Message Create(object data)
    {
        var type = data.GetType().FullName;
        var serial = JsonConvert.SerializeObject(data);
        return new Message(type, serial);
    }


    private Type GetType(string fullName)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies) {
            var type = assembly.GetType(fullName);
            if (type != null) return type;
        }

        throw new BlueProtocolNetworkException($"Type {fullName} not found.");
    }


    public object Deserialize()
    {
        var type = GetType(this.Type);
        return JsonConvert.DeserializeObject(this.Body, type);
    }


    private void Write(NetworkStream stream, string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        var header = BitConverter.GetBytes(bytes.Length);

        try {
            stream.Write(header, 0, header.Length);
            stream.Write(bytes, 0, bytes.Length);
        } catch (ObjectDisposedException e) {
            throw new BlueProtocolConnectionClosed("The NetworkStream is closed.", e);
        } catch (InvalidOperationException e) {
            throw new BlueProtocolNetworkException("The NetworkStream is not writable.", e);
        } catch (IOException e) {
            throw new BlueProtocolConnectionClosed("An I/O error occurred while writing to the NetworkStream.", e);
        }
    }


    public void Send(NetworkStream stream)
    {
        Write(stream, this.Type);
        Write(stream, this.Body);
    }


    private static byte[] Read(NetworkStream stream, int length)
    {
        var buffer = new byte[length];
        var offset = 0;
        while (offset < length) {
            offset += stream.Read(buffer, offset, length - offset);
        }

        return buffer;
    }


    private static string Read(NetworkStream stream)
    {
        var header = Read(stream, 4);
        var length = BitConverter.ToInt32(header, 0);
        var body = Read(stream, length);
        return Encoding.UTF8.GetString(body);
    }


    public static Message Receive(NetworkStream stream)
    {
        var type = Read(stream);
        var body = Read(stream);
        return new Message(type, body);
    }
}