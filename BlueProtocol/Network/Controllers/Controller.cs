using System.Reflection;
using BlueProtocol.Exceptions;
using BlueProtocol.Network;
using BlueProtocol.Network.Events;
using BlueProtocol.Network.Requests;


namespace BlueProtocol.Controllers;


/// <summary>
/// Attribute to mark a method as handling a specific type of request.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class OnRequest : Attribute;


/// <summary>
/// Attribute to mark a method as handling a specific type of event.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class OnEvent : Attribute;


/// <summary>
/// The <c>Controller</c> class models the logic for handling requests and events.
/// </summary>
public class Controller
{
    private readonly List<ControllerRequest> requests = [];
    private readonly List<ControllerEvent> events = [];


    private void BuildRequest(Type type, MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != 1 && parameters.Length != 2)
            throw new BlueProtocolControllerException(
                $"The method {method.Name} in {type} must take either one or two parameters");

        Type clientType = null;
        Type inputType = null;
        foreach (var parameter in parameters) {
            if (typeof(BlueClient).IsAssignableFrom(parameter.ParameterType)) {
                if (clientType != null)
                    throw new BlueProtocolControllerException(
                        $"The method {method.Name} in {type} has multiple IClient parameters");
                clientType = parameter.ParameterType;
            } else if (parameter.ParameterType.IsSubclassOf(typeof(Request))) {
                if (inputType != null)
                    throw new BlueProtocolControllerException(
                        $"The method {method.Name} in {type} has multiple Request parameters");
                inputType = parameter.ParameterType;
            } else {
                throw new BlueProtocolControllerException(
                    $"The parameter {parameter.Name} in {method.Name} in {type} is not a valid type");
            }
        }

        if (clientType != null && parameters[0].ParameterType != clientType)
            throw new BlueProtocolControllerException(
                $"The first parameter in {method.Name} in {type} must be inherited from IClient"
            );

        if (inputType == null)
            throw new BlueProtocolControllerException(
                $"The method {method.Name} in {type} must have one parameter inherited from Request");

        var outputType = method.ReturnType;
        if (outputType != typeof(void) && outputType != typeof(Response) &&
            !outputType.IsSubclassOf(typeof(Response)))
            throw new BlueProtocolControllerException(
                $"The output type {outputType} in {type} must be inherited from Response or void");

        if (this.requests.Exists(x => x.InputType == inputType))
            throw new BlueProtocolControllerException(
                $"There are multiple methods with the same input type {inputType} in {type}");

        this.requests.Add(new ControllerRequest(inputType, clientType, method));
    }


    private void BuildEvent(Type type, MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != 1 && parameters.Length != 2)
            throw new BlueProtocolControllerException(
                $"The method {method.Name} in {type} must take either one or two parameters");

        Type clientType = null;
        Type inputType = null;
        foreach (var parameter in parameters) {
            if (typeof(BlueClient).IsAssignableFrom(parameter.ParameterType)) {
                if (clientType != null)
                    throw new BlueProtocolControllerException(
                        $"The method {method.Name} in {type} has multiple IClient parameters");
                clientType = parameter.ParameterType;
            } else if (parameter.ParameterType.IsSubclassOf(typeof(Event))) {
                if (inputType != null)
                    throw new BlueProtocolControllerException(
                        $"The method {method.Name} in {type} has multiple Events parameters");
                inputType = parameter.ParameterType;
            } else {
                throw new BlueProtocolControllerException(
                    $"The parameter {parameter.Name} in {method.Name} in {type} is not a valid type");
            }
        }

        if (clientType != null && parameters[0].ParameterType != clientType)
            throw new BlueProtocolControllerException(
                $"The first parameter in {method.Name} in {type} must be inherited from IClient"
            );

        if (inputType == null)
            throw new BlueProtocolControllerException(
                $"The method {method.Name} in {type} must have one parameter inherited from Event");

        if (this.requests.Exists(x => x.InputType == inputType))
            throw new BlueProtocolControllerException(
                $"There are multiple methods with the same input type {inputType} in {type}");

        this.events.Add(new ControllerEvent(inputType, clientType, method));
    }


    internal void Build()
    {
        if (this.requests.Count != 0 || this.events.Count != 0) return;

        var types = new List<Type> { this.GetType() };
        types.AddRange(this.GetType().Assembly.GetTypes().Where(t => t.IsSubclassOf(this.GetType())));

        foreach (var type in types) {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods) {
                if (method.GetCustomAttributes(typeof(OnRequest), false).Length != 0)
                    BuildRequest(type, method);
                else if (method.GetCustomAttributes(typeof(OnEvent), false).Length != 0)
                    BuildEvent(type, method);
            }
        }
    }


    internal bool OnRequest(BlueClient client, Request input, out object output)
    {
        output = null;
        var request = this.requests.Find(x => x.InputType == input.GetType());
        if (request == null) return false;

        var args = request.ClientType != null ? new object[] { client, input } : [input];
        output = request.Method.Invoke(this, args);
        if (output is Response response)
            response.RequestId = input.RequestId;
        return true;
    }


    internal bool OnEvent(BlueClient client, Event input)
    {
        var e = this.events.Find(x => x.InputType == input.GetType());
        if (e == null) return false;

        var args = e.ClientType != null ? new object[] { client, input } : [input];
        e.Method.Invoke(this, args);
        return true;
    }
}


internal class ControllerRequest
{
    public Type InputType { get; }
    public Type ClientType { get; }
    public MethodInfo Method { get; }


    public ControllerRequest(Type inputType, Type clientType, MethodInfo method)
    {
        this.InputType = inputType;
        this.ClientType = clientType;
        this.Method = method;
    }
}


internal class ControllerEvent
{
    public Type InputType { get; }
    public Type ClientType { get; }
    public MethodInfo Method { get; }


    public ControllerEvent(Type inputType, Type clientType, MethodInfo method)
    {
        this.InputType = inputType;
        this.ClientType = clientType;
        this.Method = method;
    }
}