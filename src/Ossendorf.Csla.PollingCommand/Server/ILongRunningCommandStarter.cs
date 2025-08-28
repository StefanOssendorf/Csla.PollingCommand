using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Ossendorf.Csla.PollingCommand.Server;
internal interface ICommandStarter {
    ValueTask<Guid> Start(Type commandType, IReadOnlyCollection<object?>? parameters);
}

internal interface IFinishedCommands {
    bool TryGet(Guid correlationId, out object result);
}

internal interface IWaitingCommands {
    IAsyncEnumerable<QueuedCommand> ReadQueued(CancellationToken cancellationToken);
}

internal interface IFinishCommand {

}

internal class Commands : ICommandStarter, IFinishedCommands, IWaitingCommands, IFinishCommand {

    private readonly Channel<QueuedCommand> _channel = Channel.CreateUnbounded<QueuedCommand>(new UnboundedChannelOptions {
        SingleReader = true,
        SingleWriter = true
    });

    async ValueTask<Guid> ICommandStarter.Start(Type commandType, IReadOnlyCollection<object?>? parameters) {

        var item = new QueuedCommand(commandType, parameters);
        await _channel.Writer.WriteAsync(item);
        return item.CorrelationId;
    }

    async IAsyncEnumerable<QueuedCommand> IWaitingCommands.ReadQueued([EnumeratorCancellation] CancellationToken cancellationToken) {
        await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken)) {
            yield return item;
        }
    }

    bool IFinishedCommands.TryGet(Guid correlationId, out object result) => throw new NotImplementedException();
}

internal record QueuedCommand(Type Command, IReadOnlyCollection<object?>? Parameters) {
    public Guid CorrelationId { get; } = Guid.NewGuid();
}