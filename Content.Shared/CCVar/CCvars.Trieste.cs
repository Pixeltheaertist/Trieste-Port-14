using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<float> ThunderFrequency =
        CVarDef.Create("trieste.ThunderFrequency", 10f, CVar.SERVERONLY);

     public static readonly CVarDef<bool> CustomThunder =
        CVarDef.Create("trieste.CustomThunder", false, CVar.SERVERONLY);

    public static readonly CVarDef<float> ThunderRange =
        CVarDef.Create("trieste.ThunderRange", 50f, CVar.SERVERONLY);
}
