nukeops-title = Cascade Operatives
nukeops-description = Nanotrasen has targeted the Platform's artifact. Defend the planet from destruction by keeping them from getting the access frequency!

nukeops-welcome =
    You are a Cascade Operative. A specially trained team of elite NT soldiers, whose goal is simple: get the access frequency from the Overseer, load it into your delaminator, attatch it to the Artifact.
    Desperate times call for desperate measures. Operation {$name} is a go ! This is the last day they choose to mess with Nanotrasen.

nukeops-opsmajor = [color=crimson]Nanotrasen major victory![/color]
nukeops-opsminor = [color=crimson]Nanotrasen minor victory![/color]
nukeops-neutral = [color=yellow]Neutral outcome![/color]
nukeops-crewminor = [color=green]Trieste minor victory![/color]
nukeops-crewmajor = [color=green]Trieste major victory![/color]

nukeops-cond-nukeexplodedoncorrectstation = The cascade operatives managed to destroy the planet.
nukeops-cond-nukeexplodedonnukieoutpost = If this happens something broke.
nukeops-cond-nukeexplodedonincorrectlocation = If this happens something broke.
nukeops-cond-nukeactiveinstation = The planet was destroyed, but the citizens escaped.
nukeops-cond-nukeactiveatcentcom = WHAT THE HELL DID YOU GUYS DO!
nukeops-cond-nukediskoncentcom = The citizens managed to evacuate the frequency back to SolGov, but lost Trieste to Nanotrasen.
nukeops-cond-nukedisknotoncentcom = The citizens left the frequency behind during evacuation. Trieste is under Nanotrasen control.
nukeops-cond-nukiesabandoned = The cascade operatives were abandoned.
nukeops-cond-allnukiesdead = All cascade operatives have been killed.
nukeops-cond-somenukiesalive = Some cascade operatives died.
nukeops-cond-allnukiesalive = No cascade operatives died.

nukeops-disk-location-title = Final location of Disk:
nukeops-disk-carried-by = {" "}carried by [color=White]{$name}[/color], [color=orange]{$job}[/color], {$location} { $user ->
    [unknown] { "" }
    *[other] ([color=gray]{$user}[/color])
}

storage-hierarchy-list = { $items-left ->
  [0] { $existing-text } { $item },
  *[other] { $existing-text } { $item }, in
}

nukeops-list-start = The operatives were:
nukeops-list-name = - [color=White]{$name}[/color]
nukeops-list-name-user = - [color=White]{$name}[/color] ([color=gray]{$user}[/color])
nukeops-not-enough-ready-players = Not enough players readied up for the game! There were {$readyPlayersCount} players readied up out of {$minimumPlayers} needed. Can't start Cascops.
nukeops-no-one-ready = No players readied up! Can't start Cascops.

nukeops-role-commander = Admiral
nukeops-role-agent = Sargeant
nukeops-role-operator = Corporal
