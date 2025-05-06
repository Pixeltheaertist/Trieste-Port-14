using Robust.Shared.Utility;
using Content.Server.Popups;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Content.Server.Paper;
using Robust.Server.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Examine;
using Content.Shared.Plankton;
using System.Linq;
using Content.Shared.Paper;
using Content.Server._TP;
using Content.Server.TP.Event.Components;

namespace Content.Server.TP.Event.Systems;

public sealed class PearlScannerSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PearlScannerComponent, BeforeRangedInteractEvent>(OnBeforeRangedInteract);
        SubscribeLocalEvent<PearlScannerComponent, GetVerbsEvent<UtilityVerb>>(AddScanVerb);
    }

    private void OnBeforeRangedInteract(EntityUid uid, PearlScannerComponent component, BeforeRangedInteractEvent args)
{
    if (args.Handled || !args.CanReach || !args.Target.HasValue)
        return;

    var target = args.Target.Value; // Safe to use Value here
    if (!TryComp<PearlComponent>(target, out var pearl))
        return;

    CreatePopup(uid, target, pearl, component); // Now passing a non-null EntityUid

    args.Handled = true;
}


    private void AddScanVerb(EntityUid uid, PearlScannerComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess)
            return;

        if (!TryComp<PearlComponent>(args.Target, out var pearl))
            return;

        var verb = new UtilityVerb()
        {
            Act = () =>
            {
                CreatePopup(uid, args.Target, pearl, component);
            },
            Text = Loc.GetString("pearl-scan-tooltip")
        };

        args.Verbs.Add(verb);
    }

    private void CreatePopup(EntityUid uid, EntityUid target, PearlComponent pearl, PearlScannerComponent scanner)
    {
        if (TryComp(uid, out UseDelayComponent? useDelay)
            && !_useDelay.TryResetDelay((uid, useDelay), true))
            return;

        // Header for the paper report
        var message = pearl.PearlMessage;

        var report = Spawn(scanner.PearlReportEntityId, Transform(uid).Coordinates);
        _metaSystem.SetEntityName(report, Loc.GetString("pearl-analysis-report-title", ("id", $"Pearl Analysis Report")));
        _audioSystem.PlayPvs(scanner.PrintSound, uid);

        if (TryComp<PaperComponent>(report, out var paperComp))
            _paper.SetContent((report, paperComp), message);
        }
    }
