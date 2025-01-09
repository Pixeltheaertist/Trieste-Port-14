using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.GameTicking.Rules;

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
            // Ensure only one player is selected for the rule
            AssignTheThingyRule(args.EntityUid, ent);
        }

        private void AssignTheThingyRule(EntityUid player, TheThingyRuleComponent component)
        {
            // Ensure the player has a mind
            if (!_mindSystem.TryGetMind(player, out var mindId, out var mind))
                return;

            // Custom briefing message to assign
            string customBriefing = "You have been selected for a special task by TheThingy Corporation!";

            // Send the custom briefing message to the player
            _antag.SendBriefing(player, customBriefing, null, "sound/notification.ogg");

            // Assign a role with the briefing
            _roleSystem.MindAddRole(mindId, new RoleBriefingComponent
            {
                Briefing = customBriefing
            }, mind, true);

            // Assign the rule to the player
            component.AssignedPlayers.Add(player);
        }
    }

    public class TheThingyRuleComponent : Component
    {
        public List<EntityUid> AssignedPlayers = new();
    }
}
