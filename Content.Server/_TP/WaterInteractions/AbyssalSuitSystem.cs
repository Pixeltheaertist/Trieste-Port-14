using Content.Shared._TP.WaterInteractions;
using Content.Shared.Inventory.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

// Summary//
// This system controls abyssal pressure suits protecting users from crush depths when equipped.
// Summary//
namespace Content.Server._TP.WaterInteractions
{
    public sealed class AbyssalSuitSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<AbyssalSuitComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<AbyssalSuitComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<AbyssalSuitComponent> ent, ref GotEquippedEvent args)
    {
        // On equip, protect the user from abyssal pressures.
        EnsureComp<AbyssalProtectedComponent>(args.Equipee);
    }

    private void OnUnequipped(Entity<AbyssalSuitComponent> ent, ref GotUnequippedEvent args)
    {
        // On unequip, make the user able to be crushed.
        _entityManager.RemoveComponent<AbyssalProtectedComponent>(args.Equipee);
    }
}
}
