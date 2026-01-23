using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract class SharedHandCrankRechargerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandCrankRechargerComponent, GetVerbsEvent<ActivationVerb>>(AddCrankVerb);
    }

    private void AddCrankVerb(EntityUid uid, HandCrankRechargerComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        ActivationVerb verb = new()
        {
            Text = Loc.GetString("toggle-hand-crank-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/zap.svg.192dpi.png")),
            Act = () => StartDoAfter(uid, user, component),
            Priority = -1 // For things like PDA's, Open-UI, etc.
        };

        args.Verbs.Add(verb);
    }

    /// <remarks>
    ///     Predicted function. Overriden in server for all the actual doafter logic!
    /// </remarks>
    protected abstract void StartDoAfter(EntityUid uid, EntityUid user, HandCrankRechargerComponent component);
}
