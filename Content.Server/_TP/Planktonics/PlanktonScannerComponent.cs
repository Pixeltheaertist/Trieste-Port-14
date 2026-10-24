using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._TP.Planktonics;

[RegisterComponent]
public sealed partial class PlanktonScannerComponent : Component
{

    [DataField]
    public bool AnalysisMode = false;

    [DataField("planktonReportEntityId")]
    public EntProtoId PlanktonReportEntityId = "Paper";

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    [DataField("planktonRewardEntityId")]
    public EntProtoId PlanktonRewardEntityId = "ResearchDisk5000";

    [DataField("planktonAdvancedRewardEntityId")]
    public EntProtoId PlanktonAdvancedRewardEntityId = "ResearchDisk10000";
}
