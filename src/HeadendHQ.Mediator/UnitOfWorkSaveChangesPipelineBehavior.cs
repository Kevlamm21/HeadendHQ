using Mediator;
using HeadendHQ.Core.Shared;

namespace HeadendHQ.Mediator;

public class UnitOfWorkSaveChangesPipelineBehavior<TMessage, TResponse>(IUnitOfWork unitOfWork) : IPipelineBehavior<TMessage, TResponse>
	where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(message, cancellationToken);

        await unitOfWork.SaveChanges(cancellationToken);

        return response;
    }
}
