using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Ossendorf.Csla.PollingCommand.Server;

internal sealed class Commands : ICommandStarter, IWaitingCommands, IFinishedCommands, IProcessingCommands, IFinishCommands, IDisposable {
    private readonly ConcurrentDictionary<Guid, object?> _beingProcessed = new();
    private readonly MemoryCache _cache;
    private readonly IOptions<PollingCommandServerOptions> _options;
    private readonly Channel<QueuedCommand> _channel;

    public Commands(Channel<QueuedCommand> channel, IOptions<PollingCommandServerOptions> options) {
        _channel = channel;
        _options = options;
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public void Dispose() => _cache.Dispose();

    async ValueTask<Guid> ICommandStarter.Start(Type commandType, byte[] serializedParameters, byte[] principal) {
        var item = new QueuedCommand(commandType, serializedParameters, principal);

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

        if (_cache.TryGetValue(correlationId, out result) && result is not null) {
            _cache.Remove(correlationId);
            return true;
        }

        return false;
    }

    void IFinishCommands.Finish(FinishedCommand result) {
        _cache.Set(result.CorrelationId, result, new MemoryCacheEntryOptions {
            AbsoluteExpirationRelativeToNow = _options.Value.FinishedCommandTtl
        });
        _beingProcessed.Remove(result.CorrelationId, out _);
    }

    bool IProcessingCommands.IsBeingProcessed(Guid correlationId) => _beingProcessed.ContainsKey(correlationId);
}