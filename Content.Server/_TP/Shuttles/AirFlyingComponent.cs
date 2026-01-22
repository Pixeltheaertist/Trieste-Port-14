namespace Content.Server._TP.Shuttles;

[RegisterComponent]
public sealed partial class AirFlyingComponent : Component
{
    [DataField]
    public bool IsFlying = true; // Is the shuttle flying?

    [DataField]
    public bool DockedToFlier; // Is it docked to another Flyer?

    [DataField]
    public bool FirstTimeLoad = true; // Enabled if a ship has just been loaded for the first time. Prevents falling immediately.




    // IN LOOPS
    // Continue: Moves forward without ending loop, skipping the current loop
    // Break: Ends the loop


    // IN NORMAL CODE
    // Return: Stops the code from running any further down.

}
