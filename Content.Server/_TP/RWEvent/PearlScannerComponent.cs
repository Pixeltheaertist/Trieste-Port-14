using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._TP.RWEvent;

[RegisterComponent]
public sealed partial class PearlScannerComponent : Component
{


    [DataField("pearlReportEntityId")]
    public EntProtoId PearlReportEntityId = "Paper";

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");
}
