using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._TP.NanoTraitor;

/// <summary>
///     Companion to <see cref="Content.Shared.GameTicking.Rules.Components.TraitorRuleComponent"/> that turns a traitor
///         rule into a NanoTrasen traitor rule.
///     The paired TraitorRule should have <c>giveUplink</c> and <c>giveBriefing</c> set to false,
///         as this component handles the uplink and briefing instead, after the upstream traitor rule has run.
/// </summary>
[RegisterComponent, Access(typeof(NanoTraitorRuleSystem))]
public sealed partial class NanoTraitorRuleComponent : Component
{
    /// <summary>
    ///     Should the traitor get an uplink?
    /// </summary>
    [DataField]
    public bool GiveUplink = true;

    /// <summary>
    ///     Should the traitor get a briefing in chat?
    /// </summary>
    [DataField]
    public bool GiveBriefing = true;

    /// <summary>
    ///     Remote store spawned in nullspace, opened by a PDA ringer code or the implant.
    ///     Must have a RingerAccessUplink component.
    /// </summary>
    [DataField]
    public EntProtoId<StoreComponent> UplinkStore = "TP14StorePresetRemoteUplinkNT";

    /// <summary>
    ///     Implant given when the traitor has no PDA.
    ///     Must have a RemoteStore component.
    /// </summary>
    [DataField]
    public EntProtoId UplinkImplant = "TP14UplinkImplantNT";

    [DataField]
    public ProtoId<CurrencyPrototype> Currency = "TP14CurrencyBluespaceCrystal";

    /// <summary>
    ///     Starting currency, before the job's AntagAdvantage is subtracted.
    /// </summary>
    [DataField]
    public FixedPoint2 StartingBalance = 20;

    [DataField]
    public bool GiveDiscounts = true;

    [DataField]
    public SoundSpecifier GreetSound = new SoundPathSpecifier("/Audio/Ambience/Antag/NT_start.ogg");

    [DataField]
    public Color CodewordColor = Color.FromHex("#5fa8d3");

    [DataField]
    public LocId GreetingLoc = "traitor-role-greeting-nt";

    [DataField]
    public LocId CodewordsLoc = "traitor-role-codewords-nt";

    [DataField]
    public LocId UplinkCodeLoc = "traitor-role-uplink-code-nt";

    [DataField]
    public LocId UplinkImplantLoc = "traitor-role-uplink-implant-nt";

    [DataField]
    public LocId CodewordsShortLoc = "traitor-role-codewords-short-nt";

    [DataField]
    public LocId UplinkCodeShortLoc = "traitor-role-uplink-code-short-nt";

    [DataField]
    public LocId UplinkImplantShortLoc = "traitor-role-uplink-implant-short-nt";
}
