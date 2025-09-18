using Csla;

namespace Ossendorf.Csla.PollingCommand.Tests.Client;

[CslaImplementProperties]
public partial class CommandReturnsFixedString : CommandBase<CommandReturnsFixedString> {
    public const string ReturnConstant = "FixedString";

    public partial string FixedString { get; set; }
}