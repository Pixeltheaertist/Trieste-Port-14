using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Codewords;
using Content.Server.Traitor.Components;
using Content.Shared.Paper;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Traitor.Systems;

public sealed class TraitorCodePaperSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly CodewordSystem _codewordSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TraitorCodePaperComponent, MapInitEvent>(OnMapInit);
    }

    /// <summary>
    ///     This method sets everything up when the paper is spawned.
    /// </summary>
    /// <param name="codePaperUid">TraitorCodePaper Uid</param>
    /// <param name="codePaperComp">TraitorCodePaper Comp</param>
    /// <param name="args">MapInitEvent Arguments</param>
    private void OnMapInit(EntityUid codePaperUid, TraitorCodePaperComponent codePaperComp, MapInitEvent args)
    {
        SetupPaper(codePaperUid, codePaperComp);
    }

    /// <summary>
    ///     This method sets up the traitor paper contents.
    /// </summary>
    /// <param name="codePaperUid"></param>
    /// <param name="codePaperComp"></param>
    private void SetupPaper(EntityUid codePaperUid, TraitorCodePaperComponent? codePaperComp = null)
    {
        if (!Resolve(codePaperUid, ref codePaperComp))
            return;

        if (TryComp(codePaperUid, out PaperComponent? paperComp))
        {
            if (TryGetTraitorCode(out var paperContent, codePaperComp))
            {
                _paper.SetContent((codePaperUid, paperComp), paperContent);
            }
        }
    }

    /// <summary>
    ///     Try to get the traitor codewords from the game rules.
    /// </summary>
    /// <param name="traitorCode">Traitor Code (out)</param>
    /// <param name="codePaperComp">TraitorCodePaper Component</param>
    /// <returns></returns>
    private bool TryGetTraitorCode([NotNullWhen(true)] out string? traitorCode, TraitorCodePaperComponent codePaperComp)
    {
        traitorCode = null;

        var codesMessage = new FormattedMessage();
        var codeList = _codewordSystem.GetCodewords(codePaperComp.CodewordFaction).ToList();

        if (codeList.Count == 0)
        {
            codeList = codePaperComp.FakeCodewords
                ? _codewordSystem.GenerateCodewords(codePaperComp.CodewordGenerator).ToList()
                : [Loc.GetString("traitor-codes-none")];
        }

        _random.Shuffle(codeList);

        var i = 0;
        foreach (var code in codeList)
        {
            i++;
            if (i > codePaperComp.CodewordAmount && !codePaperComp.CodewordShowAll)
                break;

            codesMessage.PushNewline();
            codesMessage.AddMarkupOrThrow(code);
        }

        // Finally, we check if the codeword count is one or more,
        // and based on that we set the paper to be plural or singular.
        if (!codesMessage.IsEmpty)
        {
            if (i == 1)
                traitorCode = Loc.GetString("traitor-codes-message-singular") + codesMessage;
            else
                traitorCode = Loc.GetString("traitor-codes-message-plural") + codesMessage;
        }

        return !codesMessage.IsEmpty;
    }
}
