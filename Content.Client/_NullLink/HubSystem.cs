using Content.Client.Lobby;
using Content.Shared.CCVar;
using Content.Shared.NullLink;
using Content.Shared.NullLink.CCVar;
using Robust.Client.State;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using static Content.Shared.NullLink.NL;
namespace Content.Client.NullLink;

public sealed class HubSystem : EntitySystem
{
    private static readonly TimeSpan _delay = TimeSpan.FromSeconds(14);

    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _inLobby;
    private TimeSpan _lastSent;

    public Dictionary<string, NL.Server>? Servers { get; private set; }
    public Dictionary<string, NL.ServerInfo>? ServerInfo { get; private set; }
    public bool HubInitialized { get; private set; } = false;
    public string CurrentGameHostName => _cfg.GetCVar(NullLinkCCVars.Title);

    public event Action OnInitialized = delegate { };
    public event Action<string, NL.Server> OnServerUpdated = delegate { };
    public event Action<string, NL.ServerInfo> OnServerInfoUpdated = delegate { };
    public event Action<string> OnServersRemoved = delegate { };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<NL.ServerData>(OnSubscribed);
        SubscribeNetworkEvent<NL.AddOrUpdateServer>(OnAddOrUpdateServer);
        SubscribeNetworkEvent<NL.AddOrUpdateServerInfo>(OnAddOrUpdateServerInfo);
        SubscribeNetworkEvent<NL.RemoveServer>(OnRemoveServer);

        _state.OnStateChanged += OnStateChanged;
        _inLobby = _state.CurrentState is LobbyState;
        if (_inLobby)
        {
            RaiseNetworkEvent(new NL.Subscribe());
            _lastSent = _timing.RealTime;
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _state.OnStateChanged -= OnStateChanged;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_inLobby && _timing.RealTime - _lastSent > _delay)
        {
            RaiseNetworkEvent(new NL.Resubscribe());
            _lastSent = _timing.RealTime;
        }
    }

    private void OnStateChanged(StateChangedEventArgs args)
    {
        _inLobby = args.NewState is LobbyState;
        if (_inLobby)
        {
            _lastSent = _timing.RealTime;
            RaiseNetworkEvent(new NL.Subscribe());
        }
        else if (args.OldState is LobbyState)
        {
            RaiseNetworkEvent(new NL.Unsubscribe());
            HubInitialized = false;
        }
    }
    private void OnSubscribed(NL.ServerData ev)
    {
        Servers = ev.Servers;
        ServerInfo = ev.ServerInfo;
        HubInitialized = true;
        OnInitialized.Invoke();
    }
    private void OnAddOrUpdateServer(AddOrUpdateServer ev)
    {
        if (Servers is { } servers)
        {
            servers[ev.Key] = ev.Server;
            OnServerUpdated.Invoke(ev.Key, ev.Server);
        }
    }
    private void OnAddOrUpdateServerInfo(AddOrUpdateServerInfo ev)
    {
        if (ServerInfo is { } serverInfo)
        {
            serverInfo[ev.Key] = ev.ServerInfo;
            OnServerInfoUpdated.Invoke(ev.Key, ev.ServerInfo);
        }
    }
    private void OnRemoveServer(RemoveServer ev)
    {
        var removed = false;

        if (ServerInfo is { } serverInfo)
        {
            serverInfo.Remove(ev.Key);
            removed = true;
        }

        if (Servers is { } servers)
        {
            servers.Remove(ev.Key);
            removed = true;
        }

        if (removed)
            OnServersRemoved.Invoke(ev.Key);
    }
}
