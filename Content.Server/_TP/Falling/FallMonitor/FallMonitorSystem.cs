using System.Threading;
using Content.Server._TP.Falling.Components;
using Content.Server.Pinpointer;
using Content.Server.Power.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Gravity;
using Content.Shared.Radio.Components;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Server._TP.Falling.FallMonitor;

public sealed partial class FallMonitorSystem : EntitySystem
{
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FallSystemComponent, FallDetectedEvent>(OnFallDetected);
    }

    private void OnFallDetected(Entity<FallSystemComponent> ent, ref FallDetectedEvent args)
    {
        var monitors = EntityQueryEnumerator<FallMonitorComponent>();
        while (monitors.MoveNext(out var uid, out var comp1))
        {
            if (Comp<ApcPowerReceiverComponent>(uid).Powered && HasComp<TriesteComponent>(Transform(uid).ParentUid))
            {
                var positionName = _navMap.GetNearestBeaconString(args.DropLocation, true);
                foreach (var channel in Comp<EncryptionKeyHolderComponent>(uid).Channels) { _radio.SendRadioMessage(uid, Loc.GetString("fall-monitor-radio-alert", ("ent", ent), ("location", positionName)), channel, uid); }
            }
        }
    }
}
