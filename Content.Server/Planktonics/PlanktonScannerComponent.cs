using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Planktonics;

[RegisterComponent]
public sealed partial class PlanktonScannerComponent : Component
{

    [DataField]
    public bool AnalysisMode = false;

    [DataField("planktonReportEntityId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PlanktonReportEntityId = "Paper";

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    [DataField("planktonRewardEntityId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PlanktonRewardEntityId = "ResearchDisk5000";

    [DataField("planktonAdvancedRewardEntityId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PlanktonAdvancedRewardEntityId = "ResearchDisk10000";
}
