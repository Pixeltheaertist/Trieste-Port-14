using Content.Server.Destructible;
using Content.Shared._TP.Forage;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._TP.Forage;

public sealed partial class ForageSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private DestructibleSystem _destructible = default!;
    [Dependency] private TagSystem _tagSystem = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SharedGameTicker _gameTicker = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForageComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ForageComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<ForageComponent> ent, ref ComponentInit args)
    {
        ent.Comp.LastForagedTime = -ent.Comp.RegrowTime;
        UpdateAppearance(ent, false);
    }

    private void OnActivate(Entity<ForageComponent> forageable, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        Forage(forageable, args.User);
        args.Handled = true;
    }

    private void Forage(Entity<ForageComponent> ent, EntityUid? forager = null)
    {
        if (IsRegrowing(ent.Comp))
            return;

        if (ent.Comp.DestroyOnForage)
            _destructible.DestroyEntity(ent.Owner);

        if (ent.Comp.Loot == null)
            return;

        var pos = _transform.GetMapCoordinates(ent);

        foreach (var (tag, table) in ent.Comp.Loot)
        {
            if (tag != "All")
            {
                if (forager != null && !_tagSystem.HasTag(forager.Value, tag))
                    continue;
            }

            var spawnLoot = _entityTable.GetSpawns(table);
            foreach (var loot in spawnLoot)
            {
                var spawnPos = pos.Offset(_random.NextVector2(ent.Comp.GatherOffset));
                Spawn(loot, spawnPos);
            }
        }

        ent.Comp.LastForagedTime = _gameTicker.RoundDuration();
        UpdateAppearance(ent, true);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ForageComponent>();
        while (query.MoveNext(out var ent, out var forage))
        {
            var isRegrowing = IsRegrowing(forage);
            if (isRegrowing)
                continue;

            UpdateAppearance((ent, forage), isRegrowing);
        }
    }

    private void UpdateAppearance(Entity<ForageComponent> ent, bool isRegrowing)
    {
        _appearance.SetData(ent, RegrowVisuals.Regrowing, isRegrowing);
    }

    private bool IsRegrowing(ForageComponent comp)
    {
        return _gameTicker.RoundDuration() < comp.LastForagedTime + comp.RegrowTime;
    }
}
