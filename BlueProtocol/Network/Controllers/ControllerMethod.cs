using System.Reflection;
using BlueProtocol.Exceptions;
using BlueProtocol.Network.Communication.Events;
using BlueProtocol.Network.Communication.Requests;
using BlueProtocol.Network.Sockets.Clients;


namespace BlueProtocol.Controllers;


internal class ControllerMethod
{
    public object Object { get; }
    public Type InputType { get; }
    public Type ClientType { get; }
    public MethodInfo Method { get; }


    private ControllerMethod(object obj, Type inputType, Type clientType, MethodInfo method)
    {
        this.Object = obj;
        this.InputType = inputType;
        this.ClientType = clientType;
        this.Method = method;
    }


    public static ControllerMethod BuildMethod(object obj, MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != 1 && parameters.Length != 2)
            throw new BlueProtocolControllerException(
                $"The method \"{method.Name}\" must take either one or two parameters");

        Type clientType = null;
        Type inputType = null;
        foreach (var parameter in parameters) {
            var parameterType = parameter.ParameterType;
            if (typeof(BlueClient).IsAssignableFrom(parameterType)) {
                if (clientType != null)
                    throw new BlueProtocolControllerException(
                        $"The method \"{method.Name}\" has multiple Client parameters");
                clientType = parameterType;
            } else if (parameterType == typeof(ARequest) ||
                       parameterType.IsSubclassOf(typeof(ARequest)) ||
                       parameterType == typeof(Event) ||
                       parameterType.IsSubclassOf(typeof(Event))) {
                if (inputType != null)
                    throw new BlueProtocolControllerException(
                        $"The method \"{method.Name}\" has multiple Request / Response parameters");
                inputType = parameterType;
            } else {
                throw new BlueProtocolControllerException(
                    $"The type ({parameterType}) the parameter \"{parameter.Name}\" in \"{method.Name}\" is not a valid type");
            }
        }

        if (clientType != null && parameters[0].ParameterType != clientType)
            throw new BlueProtocolControllerException(
                $"The first parameter in \"{method.Name}\" must be inherited from IClient"
            );

        if (inputType == null)
            throw new BlueProtocolControllerException(
                $"The method \"{method.Name}\" must have one parameter inherited from Request or Event");

        var outputType = method.ReturnType;
        if (inputType.IsSubclassOf(typeof(Event)) && outputType != typeof(void))
            throw new BlueProtocolControllerException(
                $"The output type ({outputType}) must be void for an event method");
        if (inputType.IsSubclassOf(typeof(ARequest))) {
            if (outputType != typeof(Response) && !outputType.IsSubclassOf(typeof(Response)))
                throw new BlueProtocolControllerException(
                    $"The output type ({outputType}) must be inherited from Response for a request method");

            var genericTypes = inputType.GetGenericArguments().ToList();
            if (inputType.BaseType != null)
                genericTypes.AddRange(inputType.BaseType.GetGenericArguments());
            genericTypes.AddRange(genericTypes.Select(x => x.BaseType).Where(x => x != null).ToList());
            if (!genericTypes.Contains(outputType))
                throw new BlueProtocolControllerException(
                    $"The input type ({inputType}) and output type ({outputType}) must have the same base type");
        }

        return new ControllerMethod(obj, inputType, clientType, method);
    }
}