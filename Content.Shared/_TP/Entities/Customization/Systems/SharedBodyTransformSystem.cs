using Content.Shared._TP.Entities.Customization.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Entities.Customization.Systems;

public sealed class SharedBodyTransformSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyTransformComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(Entity<BodyTransformComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent.Owner, out var humAppComp))
            return;

        if (humAppComp.Species != ent.Comp.TargetSpecies)
            return;

        humAppComp.Species = ent.Comp.NewSpecies;
        Dirty(ent.Owner, humAppComp);

        if (TryComp<InventoryComponent>(ent.Owner, out var invComp))
        {
            if (ent.Comp.Displacements == null)
                return;

            invComp.Displacements = ent.Comp.Displacements;
            invComp.FemaleDisplacements = ent.Comp.Displacements;
            invComp.MaleDisplacements = ent.Comp.Displacements;
            Dirty(ent.Owner, invComp);
        }
    }
}
