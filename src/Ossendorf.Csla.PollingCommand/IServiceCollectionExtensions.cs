using Microsoft.Extensions.DependencyInjection;
using Ossendorf.Csla.PollingCommand.Client;
using Ossendorf.Csla.PollingCommand.Server;

namespace Ossendorf.Csla.PollingCommand;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/>
/// </summary>
public static class IServiceCollectionExtensions {
    public static IServiceCollection AddPollingCommandClient(this IServiceCollection services) {
        return services.AddTransient<IPollingCommand, DefaultPollingCommand>();
    }

    public static IServiceCollection AddPollingCommandServer(this IServiceCollection services) {
        return services.AddHostedService<CommandExecutionHostedService>()
            .AddSingleton<Commands>()
            .AddSingleton<ICommandStarter>(sp => sp.GetRequiredService<Commands>())
            .AddSingleton<IFinishedCommands>(sp => sp.GetRequiredService<Commands>())
            .AddSingleton<IWaitingCommands>(sp => sp.GetRequiredService<Commands>());
    }
}
