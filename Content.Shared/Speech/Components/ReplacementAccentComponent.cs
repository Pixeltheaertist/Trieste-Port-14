using Content.Shared._TP.Speech.EntitySystems;
using Content.Shared.Speech.Prototypes;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Replaces full sentences or words within sentences with new strings.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ReplacementAccentSystem), typeof(AddAccentClothingSystem))] // TRIESTE: Re-added 'AddAccentClothingSystem'
public sealed partial class ReplacementAccentComponent : BaseAccentComponent
{
    [DataField(required: true)]
    public ProtoId<ReplacementAccentPrototype> Accent;
}
