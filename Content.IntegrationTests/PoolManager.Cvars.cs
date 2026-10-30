#nullable enable
using Content.Shared.CCVar;

namespace Content.IntegrationTests;

// Partial class containing test cvars
// This could probably be merged into the main file, but I'm keeping it separate to reduce
// conflicts for forks.
public static partial class PoolManager
{
    public static readonly (string cvar, string value)[] TestCvars =
    {
        // @formatter:off
        (CCVars.DatabaseSynchronous.Name,     "true"),
        (CCVars.DatabaseSnapshot.Name,        "true"),
        (CCVars.DatabaseSqliteDelay.Name,     "0"),
        (CCVars.HolidaysEnabled.Name,         "false"),
        (CCVars.GameMap.Name,                 TestMap),
        (CCVars.AdminLogsQueueSendDelay.Name, "0"),
        (CCVars.NPCMaxUpdates.Name,           "999999"),
        (CCVars.GameRoleTimers.Name,          "false"),
        (CCVars.GameRoleLoadoutTimers.Name,   "false"),
        (CCVars.GameRoleWhitelist.Name,       "false"),
        (CCVars.GridFill.Name,                "false"),
        (CCVars.PreloadGrids.Name,            "false"),
        (CCVars.ArrivalsShuttles.Name,        "false"),
        (CCVars.EmergencyShuttleEnabled.Name, "false"),
        (CCVars.ProcgenPreload.Name,          "false"),
        (CCVars.GameDummyTicker.Name, "true"),
        (CCVars.GameLobbyEnabled.Name, "false"),
        (CCVars.ConfigPresetDevelopment.Name, "false"),
        (CCVars.AdminLogsEnabled.Name, "false"),
        (CCVars.AutosaveEnabled.Name, "false"),
        (CCVars.InteractionRateLimitCount.Name, "9999999"),
        (CCVars.InteractionRateLimitPeriod.Name, "0.1"),
        (CCVars.MovementMobPushing.Name, "false"),
        (CCVars.ResourceUploadingStoreDeletionDays.Name, "0"),

        // TRIESTE: our atmos defaults are tuned for gameplay (see CCVars.Atmos.cs), but the atmos tests
        // are written against upstream's values, so run them with those.
        (CCVars.SpaceWindMaxVelocity.Name,    "30"),
        (CCVars.MonstermosEqualization.Name,  "true"),
        (CCVars.SuperconductionTileLoss.Name, "30"),
        (CCVars.AtmosMaxProcessTime.Name,     "3"),
        (CCVars.AtmosTickRate.Name,           "15"),
        (CCVars.AtmosSpeedup.Name,            "8"),
        (CCVars.AtmosHeatScale.Name,          "8"),
    };
}
