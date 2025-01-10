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
using Robust.Shared.Audio;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Devour.Components;
using Content.Server.Containers;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;


namespace Content.Server.GameTicking.Rules
{
    public sealed class TheThingyRuleSystem : GameRuleSystem<TheThingyRuleComponent>
    {
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
        [Dependency] private readonly AntagSelectionSystem _antag = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TheThingyRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);
            SubscribeLocalEvent<TheThingyComponent, ComponentInit>(OnCompInit);
            SubscribeLocalEvent<ReleaseStingerComponent, ComponentInit>(OnCompInitStinger);
        }

         private void OnCompInit(EntityUid uid, TheThingyComponent component, ComponentInit args)
        {

            Log.Info($"{uid} has become a Thingy and is now able to consume people.");
            var eater = EnsureComp<DevourerComponent>(uid);
            eater.StructureDevourTime = 100f;
            eater.FoodPreference = FoodPreference.Humanoid; // he likey the humanoids
            eater.SoundStructureDevour = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg") // replace this with a gross absorbing sound effect
            eater.Stomach = ContainerSystem.EnsureContainer<Container>(uid, "stomach");
            _actionsSystem.AddAction(uid, ref eater.DevourActionEntity, eater.DevourAction);
        }

         private void OnCompInit(EntityUid uid, ReleaseStingerComponent stinger, ComponentInit args)
        {
            _actionsSystem.AddAction(uid, ref stinger.ActionEntity, stinger.Action);
            _actionsSystem.AddAction(uid, ref stinger.ActionEntity, stinger.ActionWithdrawl);
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
             EnsureComp<TheThingyComponent>(player);
        }
    }
}
