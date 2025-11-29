using Content.Shared._TP.Entities.Customization.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._TP.Entities.Customization.Systems;

public sealed class SharedOrganReplacementSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganReplacementComponent, ComponentStartup>(OnComponentStartup);
    }

    /// <summary>
    ///     Called when the player spawns into the match.
    ///     NOTE: This doesn't work with admin spawn verbs.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnComponentStartup(Entity<OrganReplacementComponent> ent, ref ComponentStartup args)
    {
        // Gonna be honest this is a bit of a mess - Cookie (FatherCheese)
        if (!TryComp<BodyComponent>(ent.Owner, out var body))
            return;

        // So here we have to iterate through THREE FRIGGING LOOPS
        // First one is the components replacements
        // Second one is the body container
        // And the third one is the body component's organs.
        foreach (var (oldOrganProto, newOrganProto) in ent.Comp.Replacements)
        {
            foreach (var (partId, partComp) in _body.GetBodyChildren(ent.Owner, body))
            {
                foreach (var slot in partComp.Organs)
                {
                    // First, we get the container ID from the value pair in the body.
                    // Then we check if we can get the container from the ID. If not, we skip it.
                    var containerId = SharedBodySystem.GetOrganContainerId(slot.Key);
                    if (!_container.TryGetContainer(partId, containerId, out var container))
                        continue;

                    // Now we check each organ in the container.
                    // If we can't find the prototype ID, we skip it.
                    foreach (var organId in container.ContainedEntities)
                    {
                        var metaData = MetaData(organId).EntityPrototype?.ID;
                        if (metaData == null || metaData != oldOrganProto)
                            continue;

                        // At this point we remove the old organ, and spawn/insert a new one into the entity.
                        _body.RemoveOrgan(organId);
                        QueueDel(organId);

                        var newOrgan = Spawn(newOrganProto, Transform(ent.Owner).Coordinates);
                        _body.InsertOrgan(partId, newOrgan, slot.Key);

                        // Finally, we dirty the entity and body, then break the loop to move onto the next organ.
                        Dirty(ent.Owner, body);
                        break;
                    }
                }
            }
        }
    }
}
