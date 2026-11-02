using System.Linq;
using Content.Server.GameTicking;
using Content.Shared.Station.Components;
using Content.Shared._TP.Maps.Components;
using Content.Shared.GameTicking.Events;
using Content.Shared.Maps;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map.Components;

namespace Content.Server._TP.Maps.Systems;

public sealed partial class MultiMapManagerSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    /// <summary>
    ///     Loads multiple maps from one file.
    ///     Maps are listed under the <see cref="MultiMapManagerComponent"/>
    /// </summary>
    /// <param name="ev">RoundStartingEvent</param>
    [SubscribeLocalEvent]
    private void OnRoundStarting(RoundStartingEvent ev)
    {
        var query = EntityQueryEnumerator<MultiMapManagerComponent>();
        while (query.MoveNext(out _, out var mmComp))
        {
            // We start by making a "toInitialize" list of four values.
            // This is called OUTSIDE the component 'Maps' dictionary so they can load and init properly.
            List<(Entity<MapComponent>?, HashSet<Entity<MapGridComponent>>?, string, GameMapPrototype)> toInitialize = new();

            // Now we iterate the name and prototype IDs in the Component's maps field.
            // We try to index the prototype and export the game map prototype itself.
            // Then, we try to LOAD the map from the game map's path. Exporting an ent of it, and the grids.
            // Finally, in this section we set the map name and add it all to the "toInitialize" lists.
            foreach (var (mapName, mapProtoId) in mmComp.Maps)
            {
                if (!ProtoMan.TryIndex(mapProtoId, out var gameMapProto))
                {
                    Log.Error($"MultiMapManagerSystem: Failed to find GameMap prototype for {gameMapProto}");
                    continue;
                }

                if (!_mapLoader.TryLoadMap(gameMapProto.MapPath, out var mapEnt, out var gridSet))
                {
                    Log.Error($"MultiMapManagerSystem: Failed to load the map {mapProtoId}");
                    continue;
                }

                _metaData.SetEntityName(mapEnt.Value, mapName);
                toInitialize.Add((mapEnt, gridSet, mapName, gameMapProto));
            }

            // In this final section we iterate through "toInitialize".
            // Starting by checking if the maps or grids are null, we then try to initialize and unpause the maps.
            // Finally, we get the IDs from the maps, make a new "PostGameMapLoad" event, and trigger the event.
            // Triggering tells the game to initialize stuff such as jobs.
            foreach (var (mapEnt, gridSet, mapName, gameMapProto) in toInitialize)
            {
                if (mapEnt == null || gridSet == null)
                {
                    Log.Warning($"MultiMapManagerSystem: Failed to initialize map {mapName}");
                    Log.Warning("MultiMapManagerSystem: This was caused by a null or invalid map/grid!");
                    continue;
                }

                _map.InitializeMap(mapEnt.Value.Owner, unpause: true);
                Log.Info($"MultiMapManagerSystem: Loaded the map {mapName} as {mapEnt.Value}");

                // Maps without a BecomesStation grid (like Waste Water) are just plain maps, not stations.
                if (!gridSet.Any(g => HasComp<BecomesStationComponent>(g)))
                    continue;

                var mapId = Comp<MapComponent>(mapEnt.Value).MapId;
                var mapLoadEv = new PostGameMapLoad(gameMapProto, mapId, gridSet.Select(g => g.Owner).ToList(), mapName);
                RaiseLocalEvent(mapLoadEv);
            }
        }
    }
}