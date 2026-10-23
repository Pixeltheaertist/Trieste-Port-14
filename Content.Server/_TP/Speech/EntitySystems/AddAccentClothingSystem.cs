using Content.Server.Speech.Components;
using Content.Shared.Clothing;

namespace Content.Server._TP.Speech.EntitySystems;

public sealed partial class AddAccentClothingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<Components.AddAccentClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<Components.AddAccentClothingComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid uid, Components.AddAccentClothingComponent component, ref ClothingGotEquippedEvent args)
    {
        // does the user already has this accent?
        var componentType = Factory.GetRegistration(component.Accent).Type;
        if (HasComp(args.Wearer, componentType))
            return;

        // add accent to the user
        var accentComponent = (Component) Factory.GetComponent(componentType);
        AddComp(args.Wearer, accentComponent);

        // snowflake case for replacement accent
        if (accentComponent is ReplacementAccentComponent rep)
            rep.Accent = component.ReplacementPrototype!;

        component.IsActive = true;
    }

    private void OnGotUnequipped(EntityUid uid, Components.AddAccentClothingComponent component, ref ClothingGotUnequippedEvent args)
    {
        if (!component.IsActive)
            return;

        // try to remove accent
        var componentType = Factory.GetRegistration(component.Accent).Type;
        RemComp(args.Wearer, componentType);

        component.IsActive = false;
    }
}
