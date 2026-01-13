// Taken from https://github.com/emberfall-14/emberfall/pull/4/files with permission
using Content.Shared.TP14.Bell.Systems;
using Content.Shared.TP14.Bell;

namespace Content.Client.TP14.Bell.UI;

public sealed class BellBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private BellConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = new BellConsoleWindow(Owner);
        _window.OpenCentered();
        _window.OnClose += Close;
        _window.OnFTL += index => SendMessage(new DockingConsoleFTLMessage(index));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not BellConsoleState cast)
            return;

        _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Orphan();
    }
}
