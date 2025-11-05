using Csla.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace Ossendorf.Csla.PollingCommand.Server;

[AutoSerializable]
internal partial class FinishedCommand {

    public static FinishedCommand Fail(Guid correlationId, ExceptionDispatchInfo exception) => new() { CorrelationId = correlationId, Error = exception };
    public static FinishedCommand Success(Guid correlationId, object result) => new() { CorrelationId = correlationId, Result = result };

    public FinishedCommand() { }

    public Guid CorrelationId { get; private set; }
    public object? Result { get; private set; }
    public ExceptionDispatchInfo? Error { get; private set; }

    [MemberNotNullWhen(true, nameof(Result))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool HasResult => Error is null;
}