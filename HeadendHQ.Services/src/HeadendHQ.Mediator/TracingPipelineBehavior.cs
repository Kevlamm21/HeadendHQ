using Mediator;
using System.Diagnostics;
using HeadendHQ.Core;

namespace HeadendHQ.Mediator;

public static class TracingPipelineBehavior
{
	public static readonly ActivitySource ActivitySource = new("Mediator");
}

public class TracingPipelineBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
	where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        var name = typeof(TMessage).Name
            .Replace("Command", string.Empty)
            .Replace("Query", string.Empty)
            .ToTitleCase();

        using var activity = TracingPipelineBehavior.ActivitySource.StartActivity(name);
        activity?.SetTag("RequestType", typeof(TMessage).Name);

        var response = await next(message, cancellationToken);

        if (response is not null)
        {
            activity?.SetTag("ResponseType", response.GetType().Name);
        }

        return response;
    }
}