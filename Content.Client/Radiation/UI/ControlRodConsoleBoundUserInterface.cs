using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Content.Shared.Radiation.Components;
namespace Content.Client.Radiation.UI
{
    [UsedImplicitly]
    public sealed class ControlRodConsoleBoundUserInterface : BoundUserInterface
    {
        private ControlRodConsoleWindow? _window;
        public ControlRodConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }
        protected override void Open()
        {
            base.Open();
            _window = new ControlRodConsoleWindow
            {
                Title = "Control Rod Console" //TODO loc
            };
            _window.OnClose += Close;
            _window.OnUiButtonPressed += (args, button) => SendMessage(new UiButtonPressedMessage(button.Function,button.Rod));
            _window.OpenCentered();
        }
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            _window?.Populate((ControlRodConsoleBoundUserInterfaceState) state);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;
            if (_window != null)
            {
                _window.OnClose -= Close;
                _window.OnUiButtonPressed += (args, button) => SendMessage(new UiButtonPressedMessage(button.Function, button.Rod));
            }
            _window?.Dispose();
        }
    }
}
