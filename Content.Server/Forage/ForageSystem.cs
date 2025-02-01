using Content.Server.Destructible;
using Content.Shared.Forage;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Forage;

public sealed class ForageSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForageComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ForageComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<ForageComponent> ent, ref ComponentInit args)
    {
        UpdateAppearance(ent);
    }

    private void OnActivate(Entity<ForageComponent> forageable, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        Forage(forageable, args.User);
        args.Handled = true;
    }

    private void Forage(EntityUid uid, EntityUid? forager = null, ForageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Regrowing)
            return;

        if (component.DestroyOnForage)
            _destructible.DestroyEntity(uid);

        if (component.Loot == null)
            return;

        var pos = _transform.GetMapCoordinates(uid);

        foreach (var (tag, table) in component.Loot)
        {
            if (tag != "All")
            {
                if (forager != null && !_tagSystem.HasTag(forager.Value, tag))
                    continue;
            }

            var getLoot = _proto.Index(table);
            var spawnLoot = getLoot.GetSpawns(_random);
            var spawnPos = pos.Offset(_random.NextVector2(component.GatherOffset));
            Spawn(spawnLoot[0], spawnPos);
        }

        component.Regrowing = true;
        UpdateAppearance((uid, component));
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ForageComponent>();
        while (query.MoveNext(out var ent, out var forage))
        {
            if (!forage.Regrowing)
                continue;

            forage.TimeSinceForage += frameTime;
            if (forage.TimeSinceForage < forage.RegrowTime)
                continue;

            forage.TimeSinceForage = 0.0f;
            forage.Regrowing = false;
            UpdateAppearance((ent, forage));
        }
    }

    private void UpdateAppearance(Entity<ForageComponent> ent)
    {
        _appearance.SetData(ent, RegrowVisuals.Regrowing, ent.Comp.Regrowing);
    }
}
