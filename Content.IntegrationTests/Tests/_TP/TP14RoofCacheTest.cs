using Content.IntegrationTests.Fixtures;
using Content.Server._TP.Light;
using Content.Server.Light.EntitySystems;
using Content.Shared._TP.Light;
using Content.Shared.Gravity;
using Content.Shared.Light.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._TP;

/// <summary>
/// Checks that <see cref="TP14RoofCacheSystem"/> keeps the Trieste roof bitmask in sync as roof markers come and go.
/// </summary>
[TestFixture]
[TestOf(typeof(TP14RoofCacheSystem))]
public sealed class TP14RoofCacheTest : GameTest
{
    private const string Marker = "TP14MarkerInside";

    [Test]
    public async Task AddAndRemoveRoof()
    {
        var pair = Pair;
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();

        var entMan = server.EntMan;
        var roofSys = entMan.System<RoofSystem>();

        var grid = testMap.Grid;
        var index = Vector2i.Zero;
        var coords = new EntityCoordinates(grid.Owner, 0.5f, 0.5f);

        EntityUid first = default;
        EntityUid second = default;

        bool IsRooved()
        {
            var roof = entMan.GetComponent<RoofComponent>(grid.Owner);
            return roofSys.IsRooved((grid.Owner, grid.Comp, roof), index);
        }

        await server.WaitAssertion(() =>
        {
            entMan.AddComponent<TriesteComponent>(grid.Owner);

            first = entMan.SpawnEntity(Marker, coords);
            second = entMan.SpawnEntity(Marker, coords);

            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<RoofComponent>(grid.Owner), "Trieste grid didn't get a RoofComponent.");
                Assert.That(entMan.HasComponent<TP14BakedRoofComponent>(grid.Owner), "Trieste grid wasn't marked as baked.");
                Assert.That(IsRooved(), "Tile wasn't rooved after adding roof markers.");
            });
        });

        await server.WaitRunTicks(1);

        // Removing one of two markers on a tile should leave it rooved.
        await server.WaitAssertion(() =>
        {
            entMan.DeleteEntity(first);
            Assert.That(IsRooved(), "Tile lost its roof while another roof marker was still on it.");
        });

        await server.WaitRunTicks(1);

        // Removing the last one should clear it.
        await server.WaitAssertion(() =>
        {
            entMan.DeleteEntity(second);
            Assert.That(IsRooved(), Is.False, "Tile was still rooved after removing every roof marker.");
        });

        await server.WaitRunTicks(1);

        // Toggling a roof at runtime (e.g. a collapse) should go through the cache too.
        await server.WaitAssertion(() =>
        {
            var cache = entMan.System<TP14RoofCacheSystem>();
            var third = entMan.SpawnEntity(Marker, coords);
            Assert.That(IsRooved(), "Tile wasn't rooved after adding a roof marker again.");

            cache.SetRoofEnabled(third, false);
            Assert.That(IsRooved(), Is.False, "Tile was still rooved after disabling its roof.");

            cache.SetRoofEnabled(third, true);
            Assert.That(IsRooved(), "Tile wasn't rooved after re-enabling its roof.");
        });
    }
}
