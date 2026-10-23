using Robust.Shared.Configuration;

namespace Content.Shared._Starlight.CCVar;

public sealed partial class StarlightCCVars
{
    /// <summary>
    /// Restricts IC character custom species names so they cannot be others species.
    /// </summary>
    public static readonly CVarDef<bool> RestrictedCustomSpeciesNames =
        CVarDef.Create("ic.restricted_customspeciesnames", true, CVar.SERVER | CVar.REPLICATED);
}
