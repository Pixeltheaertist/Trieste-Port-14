using Content.Shared.Kitchen.Components;
using Content.Shared.Kitchen.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._TP.Kitchen.UI;

/// <summary>
///     A copy of <see cref="Client.Kitchen.UI.MicrowaveBoundUserInterface"/>.
///     Same messages and logic, but drives <see cref="OvenMenu"/>'s slot grid.
/// </summary>
[UsedImplicitly]
public sealed partial class OvenBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private MicrowaveSystem _microwave = default!;

    [ViewVariables]
    private OvenMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<OvenMenu>();
        _menu.StartButton.OnPressed += _ => SendPredictedMessage(new MicrowaveStartCookMessage());
        _menu.EjectButton.OnPressed += _ => SendPredictedMessage(new MicrowaveEjectMessage());
        _menu.OnSlotPressed += ent =>
            SendPredictedMessage(new MicrowaveEjectSolidIndexedMessage(EntMan.GetNetEntity(ent)));

        _menu.OnCookTimeSelected += (name, index, cookTime) =>
        {
            SendPredictedMessage(new MicrowaveSelectCookTimeMessage(index, cookTime));
            _menu.CookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-cook-time-label",
                ("time", name));
        };

        Update();
    }

    public override void Update()
    {
        base.Update();

        if (_menu is null || !EntMan.TryGetComponent<MicrowaveComponent>(Owner, out var comp))
            return;

        RefreshContentsDisplay(comp);
        UpdateActiveDisplay(comp);

        var buttonIndex = comp.CurrentCookTimeButtonIndex;
        if (_menu.TryGetCookTimeButton(buttonIndex, out var button))
            button.Pressed = true;

        var timeLabel = buttonIndex == 0
            ? Loc.GetString("microwave-menu-instant-button")
            : comp.CurrentCookTimerTime.ToString();

        _menu.CookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-cook-time-label",
            ("time", timeLabel));
    }

    private void UpdateActiveDisplay(MicrowaveComponent comp)
    {
        if (_menu is null)
            return;

        var isActive = EntMan.TryGetComponent<ActiveMicrowaveComponent>(Owner, out var activeComp);
        var isEmpty = !_microwave.HasContents((Owner, comp));
        _menu.IsBusy = isActive;
        _menu.ToggleBusyDisableOverlayPanel(isActive);
        _menu.StartButton.Disabled = isActive || isEmpty;
        _menu.EjectButton.Disabled = isActive || isEmpty;

        if (activeComp != null)
            _menu.CurrentCooktimeEnd = activeComp.CookTimeEnd;

        _menu.SetIngredientPanelLight(isActive && !isEmpty);
    }

    /// <summary>
    ///     Fill the slot grid with the oven's contents; leftover slots are shown empty.
    /// </summary>
    private void RefreshContentsDisplay(MicrowaveComponent comp)
    {
        if (_menu == null)
            return;

        var contents = new List<EntityUid>();
        foreach (var entity in _microwave.GetMicrowaveContents(Owner))
        {
            if (!EntMan.Deleted(entity))
                contents.Add(entity);
        }

        // One slot per unit of capacity (12 -> three rows of four).
        _menu.EnsureSlotCount(Math.Max(comp.Capacity, contents.Count));

        for (var i = 0; i < _menu.SlotCount; i++)
        {
            if (i < contents.Count)
            {
                var ent = contents[i];
                _menu.SetSlot(i, ent, EntMan.GetComponent<MetaDataComponent>(ent).EntityName);
            }
            else
            {
                _menu.SetSlot(i, null, null);
            }
        }
    }
}
