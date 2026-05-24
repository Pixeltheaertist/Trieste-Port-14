using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Guidebook.Richtext;
using Content.Shared.Chemistry.Reaction;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._TP;

/// <summary>
/// Control for listing chemical reactions in a guidebook by group.
/// </summary>
[UsedImplicitly]
public sealed partial class GuideReactionGroupEmbed : BoxContainer, IDocumentTag
{
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly ISawmill _sawmill;

    public GuideReactionGroupEmbed()
    {
        Orientation = LayoutOrientation.Vertical;
        IoCManager.InjectDependencies(this);
        _sawmill = _logManager.GetSawmill("guidebook.reaction_group");
        MouseFilter = MouseFilterMode.Stop;
    }

    public GuideReactionGroupEmbed(string group) : this()
    {
        CreateEntries(group);
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (!args.TryGetValue("Group", out var group))
        {
            _sawmill.Error("Reaction group embed tag is missing group argument");
            return false;
        }

        CreateEntries(group);

        control = this;
        return true;
    }

    private void CreateEntries(string group)
    {
        var reactions = _prototype.EnumeratePrototypes<ReactionPrototype>()
            .Where(p => p.Group.Equals(group))
            .OrderBy(p => p.ID);

        foreach (var reaction in reactions)
        {
            var embed = new GuideReactionEmbed(reaction);
            AddChild(embed);
        }
    }
}
