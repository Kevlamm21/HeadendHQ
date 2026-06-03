using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using HeadendHQ.Core;

namespace HeadendHQ.Mediator;

public static class MediatorExtensions
{
	// This one is a little funny looking because the Mediator library uses a source generator.  The action
	// that calls AddMediator has to come from the host application, since that is where the source generator
	// needs to run.
	public static void ConfigureMediator(this WebApplicationBuilder builder, Action<IServiceCollection> configure)
	{
		configure(builder.Services);

		builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkSaveChangesPipelineBehavior<,>));
		builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TracingPipelineBehavior<,>));

		ActivitySources.RegisterSource(TracingPipelineBehavior.ActivitySource);
	}

	private static readonly Type[] _mediatorTypes =
	[
		typeof(IRequest<>),
		typeof(ICommand<>),
		typeof(IQuery<>)
	];

	public static RouteHandlerBuilder Mediate<T>(this RouteGroupBuilder group, HttpMethod method, string pattern = "") where T : notnull
	{
		var routeHandler = group.MapMethods(pattern, new[] { method.Method }, async (HttpContext context, [FromServices] IMediator mediator, [AsParameters] T request, CancellationToken cancellationToken) =>
		{
			var response = await mediator.Send(request, cancellationToken);
			return response;
		});

		var responseType = typeof(T).GetInterfaces()
			.Where(i => i.IsGenericType && _mediatorTypes.Contains(i.GetGenericTypeDefinition()))
			.Select(i => i.GetGenericArguments().First())
			.FirstOrDefault();

		if (responseType is not null)
		{
			routeHandler = routeHandler.Produces(StatusCodes.Status200OK, responseType);
		}

		return routeHandler;
	}
}
