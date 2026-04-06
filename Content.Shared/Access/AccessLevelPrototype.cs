using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Access
{
    /// <summary>
    ///     Defines a single access level that can be stored on ID cards and checked for.
    /// </summary>
    [Prototype]
    public sealed partial class AccessLevelPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     The player-visible name of the access level, in the ID card console and such.
        /// </summary>
        [DataField]
        public string? Name { get; set; }

        /// <summary>
        ///     Denotes whether this access level is intended to be assignable to a crew ID card.
        /// </summary>
        [DataField]
        public bool CanAddToIdCard = true;

        /// <summary>
        ///     The department(s) this access level belongs to, for display in the ID card console.
        /// </summary>
        [DataField]
        public List<ProtoId<DepartmentPrototype>> Departments { get; set; } = new();

        public string GetAccessLevelName()
        {
            if (Name is { } name)
                return Loc.GetString(name);

            return ID;
        }
    }
}
