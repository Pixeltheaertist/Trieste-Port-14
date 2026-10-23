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
    [Dependency] private IEntityManager _entityManager = default!;

    private ShaderInstance _staticViewerShader = null!;
    private StaticViewerOverlay _staticViewerOverlay = null!;

    private static readonly ProtoId<ShaderPrototype> GrainyShader = "Grainy";

    public StaticViewerHudSystem()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void Initialize()
    {

        _staticViewerShader = _prototypeManager.Index(GrainyShader).Instance();

        _staticViewerOverlay = new StaticViewerOverlay(_staticViewerShader, _entityManager, _player);

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
        _entityManager.RemoveComponent<StaticViewerComponent>(args.EquipTarget);
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
    public override bool RequestScreenTexture => true;

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
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;

        // Duplicate the shader instance to make it mutable
        var mutableShaderInstance = _shaderInstance.Duplicate();

        mutableShaderInstance.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        // Use the duplicated mutable shader
        handle.UseShader(mutableShaderInstance);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
