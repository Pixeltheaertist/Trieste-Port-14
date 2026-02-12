using Content.Server._TP.Aberrant.Components;
using Content.Server._TP.Aberrant.Events;
using Content.Server.Chat.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;


namespace Content.Server._TP.Aberrant.EntitySystems;


/// <summary>
/// This handles...
/// </summary>
public sealed class AberrantEffectForceSpeechSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<AberrantEffectForceSpeechComponent, AberrantTriggerEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, AberrantEffectForceSpeechComponent component, AberrantTriggerEvent args)
    {
        //select thing to say
        string speech = _random.Pick(_prototypeManager.Index(component.SpeechDataset));
        switch (component.type)
        {
            case "Speech":
                _chatSystem.TrySendInGameICMessage(uid, speech, InGameICChatType.Whisper, false, false);
                break;
            case "Emote":
                _chatSystem.TryEmoteWithChat(uid, speech, ChatTransmitRange.Normal);
                break;
            default:
                //unknown type, do nothing
                return;
        }
        RemComp<AberrantEffectForceSpeechComponent>(uid);
    }
}
