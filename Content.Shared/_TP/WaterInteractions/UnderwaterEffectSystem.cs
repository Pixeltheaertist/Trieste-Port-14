using Content.Shared._TP.Jellids;
using Content.Shared.Body.Components;
using Content.Shared.Inventory;
using Content.Shared.Overlays;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._TP.WaterInteractions;

public sealed class SharedUnderwaterEffectSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private EntityUid? _soundEntity;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<InGasEvent>(OnGasState);
    }

    private void OnGasState(InGasEvent msg, EntitySessionEventArgs args)
    {
        if (!TryGetEntity(msg.Entity, out var ent))
            return;

        var entUid = ent.Value;

        if (!TryComp<BodyComponent>(entUid, out _))
            return;

        if (msg.InWater && _soundEntity == null)
        {
            SoundSpecifier rumblingSound = new SoundPathSpecifier("/Audio/_TP/Ambience/Ocean/rumbling.ogg");
            var audio = _audio.PlayGlobal(rumblingSound, entUid, AudioParams.Default.WithVolume(-3f).WithLoop(true));
            _soundEntity = audio?.Entity;

            if (HasComp<JellidComponent>(entUid))
                return;

            if (_inventory.TryGetSlotEntity(entUid, "head", out var headEnt)
                && HasComp<AirtightHelmetComponent>(headEnt))
                return;

            EnsureComp<WaterViewerComponent>(entUid);
        }
        else if (!msg.InWater && _soundEntity != null)
        {
            _audio.Stop(_soundEntity);
            _soundEntity = null;
            RemComp<WaterViewerComponent>(entUid);
        }
    }
}
