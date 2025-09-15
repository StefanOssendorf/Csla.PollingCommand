using Csla.Serialization;
using Ossendorf.Csla.PollingCommand.Client;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading.Channels;

namespace Ossendorf.Csla.PollingCommand.Server;

internal class Commands : ICommandStarter, IWaitingCommands, IFinishedCommands, IProcessingCommands, IFinishCommands {
    private readonly ConcurrentDictionary<Guid, object?> _beingProcessed = new();
    private readonly ConcurrentDictionary<Guid, FinishedCommand> _finishedCommands = new();
    private readonly Channel<QueuedCommand> _channel = Channel.CreateUnbounded<QueuedCommand>(new UnboundedChannelOptions {
        SingleReader = true,
        SingleWriter = true
    });

    async ValueTask<Guid> ICommandStarter.Start(Type commandType, IReadOnlyList<object?> parameters, byte[] principal) {
        var item = new QueuedCommand(commandType, parameters, principal);

        _beingProcessed.TryAdd(item.CorrelationId, null);
        await _channel.Writer.WriteAsync(item);
        return item.CorrelationId;
    }

    async IAsyncEnumerable<QueuedCommand> IWaitingCommands.ReadQueued([EnumeratorCancellation] CancellationToken cancellationToken) {
        await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken)) {
            yield return item;
        }
    }

    bool IFinishedCommands.TryTake(Guid correlationId, [NotNullWhen(true)] out FinishedCommand? result) {
        result = default;
        if (_beingProcessed.ContainsKey(correlationId)) {
            return false;
        }

        return _finishedCommands.TryRemove(correlationId, out result);
    }

    void IFinishCommands.Finish(FinishedCommand result) {
        _finishedCommands.TryAdd(result.CorrelationId, result);
        _beingProcessed.Remove(result.CorrelationId, out _);
    }

    bool IProcessingCommands.IsBeingProcessed(Guid correlationId) => _beingProcessed.ContainsKey(correlationId);
}