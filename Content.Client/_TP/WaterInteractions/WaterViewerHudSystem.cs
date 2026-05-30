using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._TP.WaterInteractions;

public sealed class WaterViewerHudSystem : EntitySystem
{

    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private ShaderInstance _waterViewerShader;
    private WaterViewerOverlay _waterViewerOverlay = null!;

    private const float NoMotionPow = 0.2f; // Exponent for the nomotion variant's gradient
    private const float NoMotionMax = 8.0f; // Max value for the nomotion variant's gradient
    private const float NoMotionMult = 0.75f; // Multiplier for the nomotion variant

    private readonly ProtoId<ShaderPrototype> _waterViewerShaderId = "Cataracts";
    private readonly ProtoId<ShaderPrototype> _circleMaskShaderId = new("CircleMask");

    public WaterViewerHudSystem()
    {
        IoCManager.InjectDependencies(this);
        _waterViewerShader = _prototypeManager.Index(_waterViewerShaderId).InstanceUnique();

        var circleMaskShader = _prototypeManager.Index(_circleMaskShaderId).InstanceUnique();
        circleMaskShader.SetParameter("CircleMinDist", 0.0f);
        circleMaskShader.SetParameter("CirclePow", NoMotionPow);
        circleMaskShader.SetParameter("CircleMax", NoMotionMax);
        circleMaskShader.SetParameter("CircleMult", NoMotionMult);
    }

    public override void Initialize()
    {
        _waterViewerShader = _prototypeManager.Index(_waterViewerShaderId).InstanceUnique();
        _waterViewerOverlay = new WaterViewerOverlay(_waterViewerShader, _entityManager, _player, _waterViewerShader);

        SubscribeLocalEvent<WaterViewerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<WaterViewerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WaterBlockerComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<WaterBlockerComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<WaterViewerComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<WaterViewerComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(Entity<WaterViewerComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_waterViewerOverlay);
    }

    private void OnPlayerDetached(Entity<WaterViewerComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_waterViewerOverlay);
    }


    private void OnInit(Entity<WaterViewerComponent> ent, ref ComponentInit args)
    {
        if (_player.LocalEntity == ent)
        {
            _overlayMan.AddOverlay(_waterViewerOverlay);
        }
    }

    private void OnEquipped(Entity<WaterBlockerComponent> ent, ref GotEquippedEvent args)
    {
        EnsureComp<WaterBlockerComponent>(args.Equipee);
    }

    private void OnUnequipped(Entity<WaterBlockerComponent> ent, ref GotUnequippedEvent args)
    {
        _entityManager.RemoveComponent<WaterBlockerComponent>(args.Equipee);
    }

    private void OnShutdown(Entity<WaterViewerComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == ent)
        {
            _overlayMan.RemoveOverlay(_waterViewerOverlay);
        }
    }
}

public sealed class WaterViewerOverlay : Overlay
{
    private readonly ShaderInstance _shaderInstance;
    private readonly IEntityManager _entityManager;
    private readonly IPlayerManager _player;
    private readonly ShaderInstance? _waterViewerShader;

    public WaterViewerOverlay(ShaderInstance shaderInstance, IEntityManager entityManager, IPlayerManager playerManager, ShaderInstance? waterViewerShader)
    {
        _shaderInstance = shaderInstance;
        _entityManager = entityManager;
        _player = playerManager;
        _waterViewerShader = waterViewerShader;
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var playerEntity = _player.LocalSession?.AttachedEntity;

        if (!_entityManager.TryGetComponent(playerEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        if (!_entityManager.HasComponent<WaterViewerComponent>(playerEntity))
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var playerEntity = _player.LocalSession?.AttachedEntity;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;

        var zoom = 1.0f;
        if (_entityManager.TryGetComponent<EyeComponent>(playerEntity, out var eyeComponent))
        {
            zoom = eyeComponent.Zoom.X;
        }

        if (_waterViewerShader != null)
        {
            _waterViewerShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _waterViewerShader.SetParameter("LIGHT_TEXTURE",
                args.Viewport.LightRenderTarget
                    .Texture);

            _waterViewerShader.SetParameter("Zoom", zoom);

            const float strength = 0.15f;
            const int distortPow = 2;
            const int cloudinessPow = 2;
            _waterViewerShader.SetParameter("DistortionScalar", (float)Math.Pow(strength, distortPow));
            _waterViewerShader.SetParameter("CloudinessScalar", (float)Math.Pow(strength, cloudinessPow));

            worldHandle.UseShader(_waterViewerShader);
        }

        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}
