using Microsoft.Extensions.DependencyInjection;
using Ossendorf.Csla.PollingCommand.Client;
using Ossendorf.Csla.PollingCommand.Server;
using System.Threading.Channels;

namespace Ossendorf.Csla.PollingCommand;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/>
/// </summary>
public static class IServiceCollectionExtensions {
    /// <summary>
    /// Registers the client-side polling command infrastructure into the <paramref name="services"/> container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="pollingInterval">The interval between polling attempts. Must not be <see cref="Timeout.InfiniteTimeSpan"/>.</param>
    /// <returns>The <paramref name="services"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pollingInterval"/> is <see cref="Timeout.InfiniteTimeSpan"/>.</exception>
    public static IServiceCollection AddPollingCommandClient(this IServiceCollection services, TimeSpan pollingInterval) {
        if (pollingInterval == Timeout.InfiniteTimeSpan) {
            throw new ArgumentOutOfRangeException(nameof(pollingInterval), $"The polling interval must not be {nameof(Timeout)}.{nameof(Timeout.InfiniteTimeSpan)}");
        }

        services.AddTransient<IPollingCommand, DefaultPollingCommand>();
        services.AddOptions<DefaultPollingOptions>().Configure(o => o.Interval = pollingInterval);
        return services;
    }

    /// <summary>
    /// Registers the server-side command execution infrastructure into the <paramref name="services"/> container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <paramref name="services"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddPollingCommandServer(this IServiceCollection services) {
        return services.AddHostedService<CommandExecutionHostedService>()
            .AddSingleton<ICommandExecutionProcessor, CommandExecutionProcessor>()
            .AddSingleton<Commands>()
            .AddSingleton<ICommandStarter>(sp => sp.GetRequiredService<Commands>())
            .AddSingleton<IFinishedCommands>(sp => sp.GetRequiredService<Commands>())
            .AddSingleton<IWaitingCommands>(sp => sp.GetRequiredService<Commands>())
            .AddSingleton<IFinishCommands>(sp => sp.GetRequiredService<Commands>())
            .AddSingleton<IProcessingCommands>(sp => sp.GetRequiredService<Commands>())
            .AddSingleton(Channel.CreateUnbounded<QueuedCommand>(new UnboundedChannelOptions {
                SingleReader = true,
                SingleWriter = false
            }));
    }
}
