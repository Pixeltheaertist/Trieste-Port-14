using Content.Shared.Gravity;
using Robust.Shared.Map;
using System.Linq;
using System.Numerics;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
//Summary
// This code controls the "thunder" systems on Trieste.
// Basically, every thunder frequency update (which CCVAR can modify if an admin turns on "CustomThunder") the game will spawn a "thunder" prototype near each thunder marker.
// The coordinates it spawns at are randomized each update, within a modifiable radius (either by admins or by events, see FlashStormRule)
// This creates the effect of flashing lightning. This also has a "storm mode", in which the thunder frequency gets increased and becomes a brighter prototype.
// It also rolls a 30% chance during storms to strike with a damaging prototype, which explodes and EMPs things near the strike zone.
// LightningMarkers can be modified to include the normal, storm, and strike prototypes. More lightning markers in an area = more general lightning.
// LightningMarkers themselves hold what prototype to use during normal lightning, as well as flash storms. They also have a field for what sound their lightning should make.
// The sound works by spawning an invisible marker at the origin point of the lightning, which exists for an extra .8 seconds after the lightning fades, allowing it to fully play the lightning sound effect without being cut off through deletion.
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
                } while (_entityManager.EntityQuery<UnderRoofComponent>()
                         .Any(marker =>
                             Vector2.DistanceSquared(Transform(marker.Owner).Coordinates.Position, newCoords.Position) <
                             4.5f));
                // Set default as thunder flash (no strike)

                if (entity.Cleared) // If the storm is currently cleared, no lightning
                {
                    return;
                }
                LightningSound = entity.LightningSoundPrototype;

                if (entity.StormMode) // If marker is currently in a "Flash Storm"
                {
                    LightningType = entity.StormStrikePrototype;
                    LightningSound = entity.LightningSoundPrototype;
                    var strikeChance = _random.Prob(0.3f); // Roll a =30% chance for lightning to strike
                    if (strikeChance)
                    {
                        LightningType = entity.StormLightningPrototype;
                        LightningSound = entity.LightningSoundPrototype;
                        //Log.Error("striking lightning fr fr");// Change lightning prototype to a strike prototype
                    }
                }

                Spawn(LightningType, newCoords); // Spawn lightning prototype
                Spawn(LightningSound, newCoords); // And sound too
            }

            _updateTimer = 0; // Reset lightning timer
        }
    }
}
