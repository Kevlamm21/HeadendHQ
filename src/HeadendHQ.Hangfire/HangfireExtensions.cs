using Hangfire;
using Hangfire.AspNetCore;
using Hangfire.Server;
using Hangfire.Storage.SQLite;
using HeadendHQ.Core.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;

namespace HeadendHQ.Hangfire;

public static class HangfireExtensions
{
    public static void ConfigureHangfire(this WebApplicationBuilder builder, string connectionString)
    {

        builder.Services.AddHangfire(config => config
            .UseSQLiteStorage(connectionString, new SQLiteStorageOptions
            {
                QueuePollInterval = TimeSpan.FromSeconds(1)
            }));

        builder.Services.AddSingleton<JobActivator, UnitOfWorkActivator>();
        builder.Services.AddHangfireServer();
    }

    public static void UseHangfireDashboard(this WebApplication app)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = []
        });
    }
}

public class UnitOfWorkActivator(IServiceScopeFactory serviceScopeFactory)
    : AspNetCoreJobActivator(serviceScopeFactory)
{
    public override JobActivatorScope BeginScope(PerformContext context)
    {
        var innerScope = base.BeginScope(context);
        return new UnitOfWorkScope(innerScope);
    }
}

public class UnitOfWorkScope(JobActivatorScope innerScope) : JobActivatorScope
{
    public override object Resolve(Type type) => innerScope.Resolve(type);

    public override void DisposeScope()
    {
#pragma warning disable CS0618
        if (Marshal.GetExceptionCode() == 0)
        {
            var unitOfWork = (IUnitOfWork)Resolve(typeof(IUnitOfWork));
            unitOfWork.SaveChanges(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }
#pragma warning restore CS0618

        innerScope.Dispose();
    }
}
