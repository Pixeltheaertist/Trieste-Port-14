using Content.Shared.Dataset;
using Content.Shared.FixedPoint;
﻿using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(TraitorRuleSystem))]
public sealed partial class TraitorRuleComponent : Component
{
    // List to track traitor minds/entities
    public readonly List<EntityUid> TraitorMinds = new();

    // Data fields for ProtoIds that are serialized
    [DataField]
    public ProtoId<AntagPrototype> TraitorPrototypeId = "Traitor";

    [DataField]
    public ProtoId<NpcFactionPrototype> NanoTrasenFaction = "NanoTrasen";

    [DataField]
    public ProtoId<DatasetPrototype> CodewordstarSystems = "starSystems";

    [DataField]
    public ProtoId<NpcFactionPrototype> NanoTrasenTraitorFaction = "NanoTrasenTraitor";

    [DataField]
    public ProtoId<NpcFactionPrototype> SyndicateFaction = "Syndicate";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> CodewordAdjectives = "Adjectives";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> CodewordVerbs = "Verbs";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> ObjectiveIssuers = "TraitorCorporations";

    /// <summary>
    /// Give the Syndicate traitor an Uplink on spawn.
    /// </summary>
    [DataField]
    public bool GiveUplink = true;

    /// <summary>
    /// Give the NT traitors an Uplink on spawn.
    /// </summary>
    [DataField]
    public bool GiveUplinkNT = true;

    /// <summary>
    /// Give this traitor the codewords.
    /// </summary>
    [DataField]
    public bool GiveCodewords = true;

    /// <summary>
    /// Give this traitor a briefing in chat.
    /// </summary>
    [DataField]
    public bool GiveBriefing = true;

    // Codeword arrays
    public string[] SyndicateCodewords = new string[3];
    public string[] NanoTrasenCodewords = new string[3];

    // Total traitors
    public int TotalTraitors => TraitorMinds.Count;

    // Array of Codewords
    public string[] Codewords = new string[3];

    // Enum for traitor selection states
    public enum SelectionState
    {
        WaitingForSpawn = 0,
        ReadyToStart = 1,
        Started = 2,
    }

    // Current state of the traitor rule
    public SelectionState SelectionStatus = SelectionState.WaitingForSpawn;

    // TimeSpan when traitors should be selected and announced
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? AnnounceAt;

    // Sound that will play when a traitor is selected
    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_start.ogg");

    [DataField]
    public SoundSpecifier GreetSoundNotificationNT = new SoundPathSpecifier("/Audio/Ambience/Antag/NT_start.ogg");

    // The amount of codewords selected for traitors
    [DataField]
    public int CodewordCount = 4;

    /// <summary>
    /// The amount of TC traitors start with.
    /// </summary>
    [DataField]
    public FixedPoint2 StartingBalance = 20;
    // Starting TC for traitors

    // Constructor or methods for functionality
    // Add any additional methods you need to interact with these fields
}
