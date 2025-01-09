using Content.Server.Mind;
using Content.Shared.Mind;
using Content.Server.Roles;
using Content.Server.GameTicking.Rules;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using System.Text;


namespace Content.Server.GameTicking.Rules
{
    public sealed class TheThingyRuleSystem : GameRuleSystem<TheThingyRuleComponent>
    {
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
        [Dependency] private readonly AntagSelectionSystem _antag = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TheThingyRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);
        }

        private void AfterEntitySelected(Entity<TheThingyRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
        {
            AssignTheThingyRule(args.EntityUid, ent);
        }

        private void AssignTheThingyRule(EntityUid player, TheThingyRuleComponent component)
        {
            // Ensure the player has a mind
            if (!_mindSystem.TryGetMind(player, out var mindId, out var mind))
                return;

            // Custom briefing message to assign
            string customBriefing = "You are a Mimic. You found this body many years ago on the sea floor, limp and lifeless. You wandered the barren seas for decades, living off of fish and drifters, slowly learning these creatures' dialect through the radio. Now, you are here, your new feeding grounds.";

            _antag.SendBriefing(player, customBriefing, null, "sound/notification.ogg");

            _roleSystem.MindAddRole(mindId, new RoleBriefingComponent
            {
                Briefing = customBriefing
            }, mind, true);

            EnsureComp<TheThingyRuleComponent>(player);
        }
    }
}
