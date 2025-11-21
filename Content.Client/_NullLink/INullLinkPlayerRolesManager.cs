namespace Content.Client._NullLink;

public interface INullLinkPlayerRolesManager
{
    event Action PlayerRolesChanged;

    bool ContainsAny(ulong[] roles);
    string? GetDiscordLink();
    void Initialize();
}
