using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._TP.Objectives;
using Content.Server.Cuffs;
using Content.Server.Objectives.Systems;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Systems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._TP;

/// <summary>
/// Checks that <see cref="HostageConditionSystem"/> only completes when the target is alive, cuffed and close by.
/// </summary>
[TestFixture]
[TestOf(typeof(HostageConditionSystem))]
public sealed class HostageConditionTest : GameTest
{
    private const string Objective = "TP14NanoTraitorHostageRandomPersonObjective";

    [Test]
    public async Task HostageProgress()
    {
        var pair = Pair;
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.EntMan;

        await server.WaitAssertion(() =>
        {
            var mindSys = entMan.System<SharedMindSystem>();
            var objectivesSys = entMan.System<SharedObjectivesSystem>();
            var targetSys = entMan.System<TargetObjectiveSystem>();
            var cuffSys = entMan.System<CuffableSystem>();
            var xformSys = entMan.System<SharedTransformSystem>();
            var mobStateSys = entMan.System<MobStateSystem>();

            var coords = testMap.GridCoords;
            var captor = entMan.SpawnAtPosition("MobHuman", coords);
            var hostage = entMan.SpawnAtPosition("MobHuman", coords);
            var cuffs = entMan.SpawnAtPosition("Handcuffs", coords);

            var captorMind = mindSys.CreateMind(null);
            mindSys.TransferTo(captorMind, captor, mind: captorMind);
            var hostageMind = mindSys.CreateMind(null);
            mindSys.TransferTo(hostageMind, hostage, mind: hostageMind);

            // Spawned directly so we can pick the target ourselves instead of PickRandomPerson.
            var objective = entMan.SpawnAtPosition(Objective, coords);
            targetSys.SetTarget(objective, hostageMind);

            float? Progress() => objectivesSys.GetProgress(objective, captorMind);

            Assert.That(Progress(), Is.EqualTo(0f), "Uncuffed hostage should not count.");

            Assert.That(cuffSys.TryAddNewCuffs(hostage, captor, cuffs));
            Assert.That(Progress(), Is.EqualTo(1f), "Cuffed hostage next to the captor should count.");

            xformSys.SetWorldPosition(hostage, xformSys.GetWorldPosition(captor) + new Vector2(20f, 0f));
            Assert.That(Progress(), Is.EqualTo(0f), "Cuffed hostage far from the captor should not count.");

            xformSys.SetWorldPosition(hostage, xformSys.GetWorldPosition(captor));
            Assert.That(Progress(), Is.EqualTo(1f));

            mobStateSys.ChangeMobState(hostage, MobState.Dead);
            Assert.That(Progress(), Is.EqualTo(0f), "Dead hostage should not count.");
        });
    }
}
