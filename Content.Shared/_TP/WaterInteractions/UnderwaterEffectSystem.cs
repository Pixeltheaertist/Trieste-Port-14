using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._TP.WaterInteractions;

public sealed class SharedUnderwaterEffectSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private EntityUid? _soundEntity;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<InGasEvent>(OnGasState);
    }

    private void OnGasState(InGasEvent msg, EntitySessionEventArgs args)
    {
        if (msg.InWater && _soundEntity == null)
        {
            SoundSpecifier rumblingSound = new SoundPathSpecifier("/Audio/_TP/Ambience/Ocean/rumbling.ogg");
            var audio = _audio.PlayGlobal(rumblingSound, GetEntity(msg.Entity), AudioParams.Default.WithVolume(-3f).WithLoop(true));
            _soundEntity = audio?.Entity;
        }
        else if (!msg.InWater && _soundEntity != null)
        {
            _audio.Stop(_soundEntity);
            _soundEntity = null;
        }
    }
}
