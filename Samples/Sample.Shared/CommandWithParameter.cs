using Csla;

namespace Sample.Shared;

[CslaImplementProperties]
public partial class CommandWithParameter : CommandBase<CommandWithParameter> {

    public partial string Result { get; private set; }

    [Execute]
    private void Execute(Foo foo) {
        Result = foo.Random;
    }
}