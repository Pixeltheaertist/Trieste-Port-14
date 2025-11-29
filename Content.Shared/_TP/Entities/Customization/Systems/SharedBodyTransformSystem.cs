using Content.Shared._TP.Entities.Customization.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;

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
        if (TryComp<HumanoidAppearanceComponent>(ent.Owner, out var humAppComp))
        {
            humAppComp.Species = ent.Comp.NewSpecies;
        }

        if (TryComp<InventoryComponent>(ent.Owner, out var invComp))
        {
            invComp.Displacements = ent.Comp.Displacements;
        }
    }
}
