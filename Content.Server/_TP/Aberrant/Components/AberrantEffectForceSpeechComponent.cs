using Robust.Shared.Prototypes;
using Content.Shared.Dataset;

namespace Content.Server._TP.Aberrant.Components;

/// <summary>
/// Aberrant Forced Me Commands, used for emotes or speech.
/// </summary>
[RegisterComponent]
public sealed partial class AberrantEffectForceSpeechComponent : Component
{
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> SpeechDataset;

    /// <summary>
    /// Speech or Emote, defines what it actually does.
    /// </summary>
    [DataField]
    public string type = "Speech";
}
