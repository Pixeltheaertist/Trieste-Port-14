using Content.Shared._TP.Damage.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;
using Content.Shared.FixedPoint;

namespace Content.Server._TP.EntityEffects.Effects
{

    [UsedImplicitly]
    public sealed partial class AberrantChange : EntityEffect
    {
        /// <summary>
        /// Aberrant damage adjustment
        /// </summary>
        [DataField(required: true)]
        public float AberrantDamage = 0;

        protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            return string.Empty;
        }

        public override void Effect(EntityEffectBaseArgs args)
        {
            args.EntityManager.System<AberrantSystem>().TryChangeAberrant(args.TargetEntity, AberrantDamage);
        }
    }
}
