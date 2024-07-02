using BlueProtocol.Exceptions;
using BlueProtocol.Network.Communication.Events;
using BlueProtocol.Network.Communication.Requests;
using BlueProtocol.Network.Sockets.Clients;


namespace BlueProtocol.Controllers;


/// <summary>
/// Class <c>RequestHandler</c> models a handler for requests and events.
/// </summary>
/// <typeparam name="T"></typeparam>
public class RequestHandler<T> : ICloneable where T : BlueClient
{
    public delegate Response RequestAction<in TRequest>(T client, TRequest request) where TRequest : ARequest;

    public delegate void EventAction<in TEvent>(T client, TEvent e) where TEvent : Event;


    private readonly List<ControllerMethod> methods;


    internal RequestHandler()
    {
        this.methods = [];
    }


    private RequestHandler(List<ControllerMethod> methods)
    {
        this.methods = methods;
    }


    /// <summary>
    /// Register a controller to the handler.
    /// </summary>
    /// <param name="controller"></param>
    /// <exception cref="BlueProtocolControllerException"></exception>
    public void RegisterController(Controller controller)
    {
        var controllerMethods = controller.Build();

        lock (this.methods) {
            foreach (var controllerMethod in controllerMethods) {
                if (this.methods.Any(x => x.InputType == controllerMethod.InputType))
                    throw new BlueProtocolControllerException(
                        $"A method with the same input type {controllerMethod.InputType} already exists");
                this.methods.Add(controllerMethod);
            }
        }
    }


    /// <summary>
    /// Register a request action to the handler.
    /// </summary>
    /// <param name="requestAction">The target method to be invoked when the request is received</param>
    /// <typeparam name="TRequest">The type of the request</typeparam>
    /// <exception cref="BlueProtocolControllerException"></exception>
    public void RegisterRequestHandler<TRequest>(RequestAction<TRequest> requestAction) where TRequest : ARequest
    {
        lock (this.methods) {
            if (this.methods.Any(x => x.InputType == typeof(TRequest)))
                throw new BlueProtocolControllerException(
                    $"A method with the same input type {typeof(TRequest)} already exists");
            this.methods.Add(ControllerMethod.BuildMethod(requestAction.Target, requestAction.Method));
        }
    }


    /// <summary>
    /// Register an event action to the handler.
    /// </summary>
    /// <param name="requestAction">The target method to be invoked when the event is received</param>
    /// <typeparam name="TEvent">The type of the event</typeparam>
    /// <exception cref="BlueProtocolControllerException"></exception>
    public void RegisterEventHandler<TEvent>(EventAction<TEvent> requestAction) where TEvent : Event
    {
        lock (this.methods) {
            if (this.methods.Any(x => x.InputType == typeof(TEvent)))
                throw new BlueProtocolControllerException(
                    $"A method with the same input type {typeof(TEvent)} already exists");
            this.methods.Add(ControllerMethod.BuildMethod(requestAction.Target, requestAction.Method));
        }
    }


    internal bool OnRequest(BlueClient client, ARequest input, out object output)
    {
        output = null;

        lock (this.methods) {
            var request = this.methods.Find(x => x.InputType == input.GetType());
            if (request == null) return false;

            var args = request.ClientType != null ? new object[] { client, input } : [input];
            output = request.Method.Invoke(request.Object, args);
            if (output is Response response)
                response.RequestId = input.RequestId;
            return true;
        }
    }


    internal bool OnEvent(BlueClient client, Event input)
    {
        lock (this.methods) {
            var e = this.methods.Find(x => x.InputType == input.GetType());
            if (e == null) return false;

            var args = e.ClientType != null ? new object[] { client, input } : [input];
            e.Method.Invoke(e.Object, args);
            return true;
        }
    }


    public object Clone()
    {
        lock (this.methods)
            return new RequestHandler<T>([..this.methods]);
    }
}