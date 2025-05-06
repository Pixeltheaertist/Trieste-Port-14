bounty-console-menu-title = Requisitions console
bounty-console-label-button-text = Print requisition
bounty-console-skip-button-text = Ignore
bounty-console-time-label = Time: [color=orange]{$time}[/color]
bounty-console-reward-label = Reward: [color=limegreen]${$reward}[/color]
bounty-console-manifest-label = Manifest: [color=orange]{$item}[/color]
bounty-console-manifest-entry =
    { $amount ->
        [1] {$item}
        *[other] {$item} x{$amount}
    }
bounty-console-manifest-reward = Reward: ${$reward}
bounty-console-description-label = [color=gray]{$description}[/color]
bounty-console-id-label = ID#{$id}

bounty-console-flavor-left = Communications uplink online. Scanning for requisitions.
bounty-console-flavor-right = Active transponder: TRIESTE_PORT_AUTHORITY

bounty-manifest-header = [font size=14][bold]Official requisitions manifest[/bold] (ID#{$id})[/font]
bounty-manifest-list-start = Item manifest:
