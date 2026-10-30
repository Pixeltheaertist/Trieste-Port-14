using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._TP.Overlays;

public sealed partial class StaticViewerHudSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private InventorySystem _inventory = default!;

    private StaticViewerOverlay _staticViewerOverlay = null!;

    private static readonly ProtoId<ShaderPrototype> GrainyShader = "Grainy";

    public override void Initialize()
    {
        // The grain shader only draws noise on top of the screen, it never reads it, so the shared instance is fine.
        var staticViewerShader = _prototypeManager.Index(GrainyShader).Instance();

        _staticViewerOverlay = new StaticViewerOverlay(staticViewerShader, EntityManager, _player);

        SubscribeLocalEvent<StaticViewerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<StaticViewerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StaticViewerComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<StaticViewerComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<StaticViewerComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<StaticViewerComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<StaticViewerComponent> ent, ref GotEquippedEvent args)
    {
        EnsureComp<StaticViewerComponent>(args.EquipTarget);
    }

    private void OnUnequipped(Entity<StaticViewerComponent> ent, ref GotUnequippedEvent args)
    {
        // Keep the static if something else worn still has it, e.g. thermal goggles under the abyssal helmet.
        var slots = _inventory.GetSlotEnumerator(args.EquipTarget, SlotFlags.WITHOUT_POCKET);
        while (slots.NextItem(out var item))
        {
            if (item != args.Equipment && HasComp<StaticViewerComponent>(item))
                return;
        }

        RemComp<StaticViewerComponent>(args.EquipTarget);
    }

    private void OnPlayerAttached(Entity<StaticViewerComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_staticViewerOverlay);
    }

    private void OnPlayerDetached(Entity<StaticViewerComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_staticViewerOverlay);
    }

    private void OnInit(Entity<StaticViewerComponent> ent, ref ComponentInit args)
    {
        if (_player.LocalEntity == ent)
        {
            _overlayMan.AddOverlay(_staticViewerOverlay);
        }
    }

    private void OnShutdown(Entity<StaticViewerComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == ent)
        {
            _overlayMan.RemoveOverlay(_staticViewerOverlay);
        }
    }
}

public sealed class StaticViewerOverlay : Overlay
{
    private readonly ShaderInstance _shaderInstance;
    private readonly IEntityManager _entityManager;
    private readonly IPlayerManager _playerManager;

    public StaticViewerOverlay(ShaderInstance shaderInstance, IEntityManager entityManager, IPlayerManager playerManager)
    {
        _shaderInstance = shaderInstance;
        _entityManager = entityManager;
        _playerManager = playerManager;
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var playerEntity = _playerManager.LocalEntity;

        if (!playerEntity.HasValue)
            return false;

        if (!_entityManager.TryGetComponent(playerEntity.Value, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        handle.UseShader(_shaderInstance);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
