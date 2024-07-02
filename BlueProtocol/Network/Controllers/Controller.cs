using System.Reflection;
using BlueProtocol.Exceptions;
using BlueProtocol.Network.Communication.Events;
using BlueProtocol.Network.Communication.Requests;
using BlueProtocol.Network.Sockets.Clients;


namespace BlueProtocol.Controllers;


/// <summary>
/// Attribute to mark a method as handling a specific type of event.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class Route : Attribute;


/// <summary>
/// The <c>Controller</c> class models the logic for handling requests and events.
/// </summary>
public class Controller
{
    internal List<ControllerMethod> Build()
    {
        var result = new List<ControllerMethod>();
        var types = new List<Type> { this.GetType() };
        types.AddRange(this.GetType().Assembly.GetTypes().Where(t => t.IsSubclassOf(this.GetType())));

        foreach (var type in types) {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods) {
                if (method.GetCustomAttributes(typeof(Route), false).Length != 0)
                    result.Add(ControllerMethod.BuildMethod(this, method));
            }
        }

        return result;
    }
}