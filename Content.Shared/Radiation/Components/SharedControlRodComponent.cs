using Robust.Shared.Serialization;
namespace Content.Shared.Radiation.Components
{
    [Serializable, NetSerializable]
    public enum ControlRodVisuals : byte
    {
        Status
    }
    [Serializable, NetSerializable]
    public enum ControlRodStatus : int
    {
        Stage0 = 0,
        Stage1,
        Stage2,
        Stage3,
        Stage4,
        Stage5,
        Stage6,
        Stage7,
        Stage8,
        Stage9,
        Stage10
    }
}
