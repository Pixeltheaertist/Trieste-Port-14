using Content.Server.Shuttles.Components;
using Content.Shared.Gravity;
using Robust.Shared.Map;
using Content.Server._TP.Shuttles_components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Prototypes;
using Content.Server.Shuttles.Events;
using Content.Server.Falling;
using System.Linq;
using System.Numerics;
using Content.Server.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Content.Server.GameTicking.Rules;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
//Summary
// This code controls the "thunder' systems on Trieste.
// Basically, every thunder frequency update (which can be modified by CCVAR if an admin turns on "CustomThunder") the game will spawn a "thunder" prototype near each thunder marker.
// The coordinates it spawns at is randomized each update, within a modifiable radius (either by admins or by events, see FlashStormRule)
// This creates the effect of flashing lightning. This also has a "storm mode", in which the thunder frequency gets increased, and becomes a brighter prototype.
// It also rolls a 30% chance during storms to strike with a damaging prototype, which explodes and EMPs things near the strike zone.
// LightningMarkers can be modified to include the normal, storm, and strike prototypes. More lightning markers in an area = more general lightning.
//Summary


namespace Content.Server._TP;

public sealed class ThunderSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] protected readonly IConfigurationManager CfgManager = default!;

    public float ThunderInterval = 10f;
    private float _updateTimer = 0f;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        var CustomThunder = CfgManager.GetCVar(CCVars.CustomThunder);

        base.Update(frameTime);

        _updateTimer += frameTime;

        if (_updateTimer >= ThunderInterval)
        {
            foreach (var entity in EntityManager.EntityQuery<LightningMarkerComponent>())
            {
                var entityUid = entity.Owner;
                var transform = Transform(entityUid);
                var coords = transform.Coordinates;
                var LightningType = entity.LightningPrototype;

                float ThunderRange;
                float ThunderInterval;

                if (CustomThunder)
                {
                    ThunderRange = CfgManager.GetCVar(CCVars.ThunderRange);
                    ThunderInterval = CfgManager.GetCVar(CCVars.ThunderFrequency);
                }
                else
                {
                    ThunderRange = entity.ThunderRange;
                    ThunderInterval = entity.ThunderFrequency;
                }

                this.ThunderInterval = ThunderInterval;

                Vector2 offset;
                EntityCoordinates newCoords;

                // Keep calculating coordinates until valid ones are found
                do
                {
                     offset = _random.NextVector2(ThunderRange);
                     newCoords = coords.Offset(offset);
                }
              while (_entityManager.EntityQuery<UnderRoofComponent>().Any(marker =>
              Vector2.DistanceSquared(Transform(marker.Owner).Coordinates.Position, newCoords.Position) < 4.5f));
              // Set default as thunder flash (no strike)

              if (entity.Cleared) // If the storm is currently cleared, no lightning
              {
                  return;
              }

              if (entity.StormMode) // If marker is currently in a "Flash Storm"
              {
                  LightningType = entity.StormStrikePrototype;
                  var strikeChance = _random.Prob(0.3f); // Roll a =30% chance for lightning to strike
                  if (strikeChance)
                  {
                      LightningType = entity.StormLightningPrototype;
                      Log.Error("striking lightning fr fr");// Change lightning prototype to a strike prototype
                  }
              }

                Spawn(LightningType, newCoords); // Spawn lightning prototype
            }

            _updateTimer = 0; // Reset lightning timer
        }
    }
}
