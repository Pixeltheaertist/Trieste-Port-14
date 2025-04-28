using System.Diagnostics.CodeAnalysis;
using Content.Server.Cargo.Components;
using Content.Server.Labels.Components;
using Content.Server.Station.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Content.Shared.Emag.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.TP.Components;

namespace Content.Server.TP14
{
    public sealed partial class CargoSystem
    {
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

        /// <summary>
        /// Keeps track of how much time has elapsed since last balance increase.
        /// </summary>
        private float _timer;

        private void InitializeConsole()
        {
            SubscribeLocalEvent<MoneyVaultComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<MoneyVaultComponent, InteractUsingEvent>(OnInteractUsing);
            Reset();
        }

         private void OnExamine(EntityUid uid, MoneyVaultComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var currentAmount = component.VaultAmount;
        var text = "money-vault-amount";

        args.PushMarkup(Loc.GetString(text));
    }

        private void OnInteractUsing(EntityUid uid, MoneyVaultComponent component, ref InteractUsingEvent args)
        {
            if (!HasComp<CashComponent>(args.Used))
                return;

            var price = _pricing.GetPrice(args.Used);

            if (price == 0)
                return;

            component.VaultAmount = price;
            QueueDel(args.Used);
        }
    }
}
