namespace Content.Server.TP.Event.Components;

using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;

[RegisterComponent]
public sealed partial class PearlScannerComponent : Component
{


    [DataField("pearlReportEntityId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PearlReportEntityId = "Paper";

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");
}
