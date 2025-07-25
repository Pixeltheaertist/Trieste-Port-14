using Content.Server._TP.Aberrant.Components;
using Content.Server._TP.Aberrant.Events;
using Content.Server.Chat.Systems;

namespace Content.Server._TP.Aberrant.EntitySystems;


/// <summary>
/// This handles...
/// </summary>
public sealed class AberrantEffectForceSpeechSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<AberrantEffectForceSpeechComponent, AberrantTriggerEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, AberrantEffectForceSpeechComponent component, AberrantTriggerEvent args)
    {
        if (args.Target == null)
            return;
        //select thing to say
        if (component.type == "Speech")
        {
            //_chatSystem.TrySendInGameICMessage();
        }
        else if (component.type == "Emote")
        {
            _chatSystem.TryEmoteWithChat(args.Target.Value, component.SpeechDataset, ChatTransmitRange.Normal);
        }
        else
        {
            //unknown type, do nothing
            return;
        }

    }
}
