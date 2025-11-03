using Robust.Shared.Configuration;

namespace Content.Shared.NullLink.CCVar;
public sealed partial class NullLinkCCVars
{
    /// <summary>
    /// Discord oAuth
    /// </summary>

    public static readonly CVarDef<string> DiscordCallback =
        CVarDef.Create("discord.callback", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> Secret =
        CVarDef.Create("discord.secret", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> DiscordKey =
        CVarDef.Create("discord.api_key", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);
}
