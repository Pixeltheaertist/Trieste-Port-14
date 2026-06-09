using System.Numerics;
using Content.Shared._TP.Weather;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Random;

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


namespace Content.Server._TP.Weather;

public sealed partial class ThunderSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _entLookup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IConfigurationManager _cfgManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LightningMarkerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<LightningMarkerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextStrike = ent.Comp.ThunderFrequency + _random.NextFloat(-4f, 4f);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var customThunder = _cfgManager.GetCVar(CCVars.CustomThunder);
        var thunderRange = customThunder ? _cfgManager.GetCVar(CCVars.ThunderRange) : 0f;
        var thunderInterval = customThunder ? _cfgManager.GetCVar(CCVars.ThunderFrequency) : 0f;

        var query = EntityQueryEnumerator<LightningMarkerComponent>();
        while (query.MoveNext(out var markerUid, out var markerComp))
        {
            markerComp.NextStrike -= frameTime;
            if (markerComp.NextStrike > 0f)
                continue;

            var coords = Transform(markerUid).Coordinates;
            var lightning = markerComp.LightningPrototype;

            if (!customThunder)
            {
                thunderRange = markerComp.ThunderRange;
                thunderInterval = markerComp.ThunderFrequency;
            }

            Vector2 offset;
            EntityCoordinates newCoords;

            // Keep calculating coordinates until valid ones are found.
            do
            {
                offset = _random.NextVector2(thunderRange);
                newCoords = coords.Offset(offset);
            } while (_entLookup.GetEntitiesInRange<UnderRoofComponent>(newCoords, 2.12F).Count != 0);

            // The default lightning is a thunder flash (no strike).
            // If the storm is currently cleared, no lightning at all.
            if (markerComp.Cleared)
                continue;

            // Roll a =30% chance for lightning to strike.
            // Also change the lightning prototype to a strike prototype.
            if (markerComp.StormMode)
            {
                lightning = markerComp.StormStrikePrototype;
                var strikeChance = _random.Prob(0.3f);
                if (strikeChance)
                    lightning = markerComp.StormLightningPrototype;
            }

            Spawn(lightning, newCoords);

            markerComp.NextStrike = thunderInterval + _random.NextFloat(-4f, 4f);
        }
    }
}
