using Robust.Shared.Serialization;

namespace Content.Shared._TP.Atmos;

/// <summary>
///     Event of when an entity enters or exits a gas.
/// </summary>
[Serializable, NetSerializable]
public sealed class InGasEvent : EntityEventArgs
{
    public NetEntity Entity { get; }
    public bool InWater { get; }

    public InGasEvent(NetEntity entity, bool inWater)
    {
        Entity = entity;
        InWater = inWater;
    }
}
