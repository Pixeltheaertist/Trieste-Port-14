using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared._Moffstation.Cards;
using Content.Shared._Moffstation.Cards.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.Cards;

[UsedImplicitly]
public sealed class CardHandMenuBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        Update();
        _menu.OpenOverMouseScreenPosition();
    }

    public override void Update()
    {
        _menu?.SetButtons(
            EntMan.System<CardHandSystem>()
                .GetCards(Owner)
                .Select(card => new RadialMenuActionOption<Entity<CardComponent>>(OnPressed, card)
                    {
                        IconSpecifier = RadialMenuIconSpecifier.With(card),
                        ToolTip = Loc.GetString(card.Comp.Name),
                    }
                )
        );
    }

    private void OnPressed(Entity<CardComponent> card) =>
        SendPredictedMessage(new CardHandDrawMessage(EntMan.GetNetEntity(card)));
}
