using Robust.Shared.Configuration;

namespace Content.Shared._NullLink.CCVar;
public sealed partial class NullLinkCCVars
{
    // U’ve got some confusion because both the Discord role and the in-game role (like Captain, for example) are called “role.”

    public static readonly CVarDef<string> TitleBuild =
        CVarDef.Create("nulllink.roles_req.title_builder", "TriestePort", CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<string> RoleReqWithAccessToAllRoles =
        CVarDef.Create("nulllink.roles_req.all-roles", "", CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<string> RoleReqMentors =
        CVarDef.Create("nulllink.roles_req.mentors", "", CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<string> RoleReqPeacefulBypass =
        CVarDef.Create("nulllink.roles_req.peaceful_bypass", "", CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<string> BunkerBypass =
        CVarDef.Create("nulllink.roles_req.panic_bunker_bypass", "", CVar.SERVER | CVar.REPLICATED);
}
