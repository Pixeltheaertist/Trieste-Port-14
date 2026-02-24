using System.Linq;
using Content.Shared.Speech;
using Robust.Shared.Random;
using System.Text.RegularExpressions;
using Content.Server._TP.Speech.Components;
using Content.Server.Speech.EntitySystems;

namespace Content.Server._TP.Speech.EntitySystems;

public sealed class ChristmasAccentSystem : EntitySystem
{
    private static readonly Regex FirstWordAllCapsRegex = new(@"^(\S+)");

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChristmasAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    // converts left word when typed into the right word.
    public string Accentuate(string message, ChristmasAccentComponent component)
    {
        var msg = _replacement.ApplyReplacements(message, "christmas");

        if (!_random.Prob(.5f))
            return msg;


        //Checks if the first word of the sentence is all caps
        //So the prefix can be allcapped and to not resanitize the captial

        var firstWordAllCaps = !FirstWordAllCapsRegex.Match(msg).Value.Any(char.IsLower);
        var pick = _random.Next(1, 2);

        // Reverse sanitize capital
        var prefix = Loc.GetString($"accent-christmas-prefix-{pick}");
        if (!firstWordAllCaps)
            msg = msg[0].ToString().ToLower() + msg.Remove(0, 1);
        else
            prefix = prefix.ToUpper();
        msg = prefix + " " + msg;


        return msg;
    }

    private void OnAccentGet(EntityUid uid, ChristmasAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}



