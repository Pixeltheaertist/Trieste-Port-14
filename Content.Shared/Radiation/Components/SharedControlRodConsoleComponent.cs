using Robust.Shared.Serialization;
namespace Content.Shared.Radiation.Components
{
    [Serializable, NetSerializable]
    public sealed class ControlRodConsoleBoundUserInterfaceState : BoundUserInterfaceState
    {
        public List<ControlRodInfo> ControlRodInfos; //uid, in range, extension status
        public ControlRodConsoleBoundUserInterfaceState(List<ControlRodInfo> controlRodInfos)
        {
            ControlRodInfos = controlRodInfos;
        }
    }
    [Serializable, NetSerializable]
    public sealed class ControlRodInfo
    {
        public EntityUid Rod;
        public bool InRange;
        public float Extension;
        public ControlRodInfo(EntityUid rod, bool inRange, float extension)
        {
            Rod = rod;
            InRange = inRange;
            Extension = extension;
        }
    }
    [Serializable, NetSerializable]
    public enum ControlRodConsoleUiKey : byte
    {
        Key
    }
    [Serializable, NetSerializable]
    public enum UiButton : byte
    {
        Extend,
        Retract,
        Emergency,
        Stop
    }
    [Serializable, NetSerializable]
    public sealed class UiButtonPressedMessage : BoundUserInterfaceMessage
    {
        public readonly UiButton Button;
        public readonly EntityUid? Rod;
        public UiButtonPressedMessage(UiButton button, EntityUid? rod)
        {
            Button = button;
            Rod = rod;
        }
    }
}
