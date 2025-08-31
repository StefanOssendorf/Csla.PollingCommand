using Csla;
using Csla.Core;
using System.Diagnostics.CodeAnalysis;

namespace Ossendorf.Csla.PollingCommand.Server;

[CslaImplementProperties]
internal partial class InitiateCommandExecutionCommand : CommandBase<InitiateCommandExecutionCommand> {

    public partial Guid CorrelationId {
        get; private set;
    }

    [Execute]
    private async Task InitiateExecution(string fullTypeName, MobileList<object?> parameters, [Inject] ICommandStarter commandStarter) {
        var type = Type.GetType(fullTypeName) ?? throw new InvalidOperationException($"Type '{fullTypeName}' could not be loaded. Please make sure the assembly is referenced and available.");

        CorrelationId = await commandStarter.Start(type, parameters).ConfigureAwait(false);
    }
}

[CslaImplementProperties]
internal partial class PollStateOrResultCommand : CommandBase<PollStateOrResultCommand> {

    [MemberNotNullWhen(true, nameof(Result))]
    public bool IsFinished => State == ProcessingState.Finished;

    public bool IsRunning => State == ProcessingState.Running;

    private partial ProcessingState State { get; set; }

    public partial FinishedCommand? Result { get; private set; }

    [Execute]
    private void PollStateOrResult(Guid correlationId, [Inject] IProcessingCommands processingCommands, [Inject] IFinishedCommands finishedCommands) {
        if (processingCommands.IsBeingProcessed(correlationId)) {
            State = ProcessingState.Running;
            return;
        } else if (finishedCommands.TryTake(correlationId, out var result)) {
            State = ProcessingState.Finished;
            Result = result;
        } else {
            State = ProcessingState.Unknown;
        }
    }
}

internal enum ProcessingState {
    Unknown,
    Running,
    Finished
}