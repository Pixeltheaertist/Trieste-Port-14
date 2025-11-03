using Content.Client._TP14.Temperature.Components;
using Content.Client.Examine;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._TP14.Temperature.Systems;

/// <summary>
///     Client-side logic for the storm array.
///     This is essentially a carbon-copy of the TEG Circulator system.
///     Created by Cookie (Father Cheese) for Trieste Port 14.
/// </summary>
public sealed class StormArraySystem : EntitySystem
{
    private static readonly EntProtoId ArrowPrototype = "TP14StormArrayCirculatorArrow";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StormArrayComponent, ClientExaminedEvent>(OnExaminedEvent);
    }

    private void OnExaminedEvent(Entity<StormArrayComponent> arrayEnt, ref ClientExaminedEvent args)
    {
        Spawn(ArrowPrototype, new EntityCoordinates(arrayEnt.Owner, 0, 0));
    }
}
