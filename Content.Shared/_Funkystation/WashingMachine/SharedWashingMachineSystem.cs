using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Funkystation.WashingMachine;

public abstract class SharedWashingMachineSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = null!;
    [Dependency] protected readonly SharedAudioSystem Audio = null!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = null!;
    [Dependency] protected readonly SharedEntityStorageSystem Storage = null!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = null!;
    [Dependency] private readonly SharedPopupSystem _popup = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WashingMachineComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
        SubscribeLocalEvent<WashingMachineComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<WashingMachineComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
    }

    private void OnStorageOpenAttempt(Entity<WashingMachineComponent> ent, ref StorageOpenAttemptEvent args)
    {
        if (ent.Comp.State != WashingMachineState.Idle)
            args.Cancelled = true;
    }

    private void OnActivate(Entity<WashingMachineComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !TryStartWash(ent, args.User))
            return;

        args.Handled = true;
    }

    private void OnGetVerbs(Entity<WashingMachineComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanInteract || ent.Comp.State != WashingMachineState.Idle)
            return;

        var user = args.User;
        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("washing-machine-start"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png")),
            Act = () => TryStartWash(ent, user)
        });
    }

    protected virtual bool TryStartWash(Entity<WashingMachineComponent> ent, EntityUid user)
    {
        if (ent.Comp.State != WashingMachineState.Idle || !_power.IsPowered(ent.Owner) || Storage.IsOpen(ent.Owner))
            return false;

        if (Timing.CurTime < ent.Comp.NextWashAllowed)
        {
            _popup.PopupClient(Loc.GetString("washing-machine-cooldown"), ent.Owner, user);
            return false;
        }

        if (TryComp<EntityStorageComponent>(ent, out var storage) && storage.Contents.ContainedEntities.Count == 0)
            return false;

        ent.Comp.State = WashingMachineState.Washing;
        ent.Comp.WashFinishTime = Timing.CurTime + ent.Comp.WashTime;

        Dirty(ent.Owner, ent.Comp);
        Appearance.SetData(ent.Owner, WashingMachineVisuals.State, WashingMachineState.Washing);

        return true;
    }
}
