using Content.Shared._TP.Bells.Misc;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._TP.Bells.UI;

[UsedImplicitly]
public sealed class BellConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private BellConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<BellConsoleWindow>();
        _window.OnDestinationSelected += door =>
        {
            Logger.Debug($"[Bell] BUI relaying travel request for {door}");
            SendMessage(new BellConsoleTravelMessage(door));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is BellConsoleBoundUserInterfaceState bellState)
            _window?.UpdateState(bellState);
    }
}