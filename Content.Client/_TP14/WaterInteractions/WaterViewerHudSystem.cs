using Content.Shared.CCVar;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.Overlays;
using Robust.Shared.Timing;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Content.Shared.Clothing;
using Robust.Shared.Configuration;

namespace Content.Client.Overlays
{
    public sealed class WaterViewerHudSystem : EntitySystem
{

    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;

private ShaderInstance _waterViewerShader = null!;
private WaterViewerOverlay _waterViewerOverlay = null!;
private readonly ShaderInstance _circleMaskShader;

private float _magnitude = 3.0f;
private float _correctionPower = 1.0f;
private const float Distortion_Pow = 2.0f; // Exponent for the distortion effect
private const float Cloudiness_Pow = 1.0f; // Exponent for the cloudiness effect
private const float NoMotion_Radius = 30.0f; // Base radius for the nomotion variant at its full strength
private const float NoMotion_Pow = 0.2f; // Exponent for the nomotion variant's gradient
private const float NoMotion_Max = 8.0f; // Max value for the nomotion variant's gradient
private const float NoMotion_Mult = 0.75f; // Multiplier for the nomotion variant


    public WaterViewerHudSystem()
    {
        IoCManager.InjectDependencies(this);
        _waterViewerShader = _prototypeManager.Index<ShaderPrototype>("Cataracts").InstanceUnique();

        _circleMaskShader = _prototypeManager.Index<ShaderPrototype>("CircleMask").InstanceUnique();
        _circleMaskShader.SetParameter("CircleMinDist", 0.0f);
        _circleMaskShader.SetParameter("CirclePow", NoMotion_Pow);
        _circleMaskShader.SetParameter("CircleMax", NoMotion_Max);
        _circleMaskShader.SetParameter("CircleMult", NoMotion_Mult);
    }

    public override void Initialize()
    {
        _waterViewerShader = _prototypeManager.Index<ShaderPrototype>("Cataracts").Instance();
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
    private ShaderInstance? _waterViewerShader;

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
        var playerEntity = _player.LocalEntity;

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

            var playerEntity = _player.LocalSession?.AttachedEntity;

            var worldHandle = args.WorldHandle;
            var viewport = args.WorldBounds;
            var strength = 5f;

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

                double Distortion_Pow = 5;
                _waterViewerShader.SetParameter("DistortionScalar", (float)Math.Pow(strength, Distortion_Pow));
                double Cloudiness_Pow = 5;
                _waterViewerShader.SetParameter("CloudinessScalar", (float)Math.Pow(strength, Cloudiness_Pow));

                worldHandle.UseShader(_waterViewerShader);
            }

            worldHandle.DrawRect(viewport, Color.White);
            worldHandle.UseShader(null);
    }
}
}
