using Csla;
using System.Diagnostics.CodeAnalysis;

namespace Ossendorf.Csla.PollingCommand.Server;

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
        } else if (finishedCommands.TryTake(correlationId, out var result)) {
            State = ProcessingState.Finished;
            if (!result.HasResult) {
                result.Error.Throw();
            }

            Result = result;
        } else {
            State = ProcessingState.Unknown;
        }
    }
}