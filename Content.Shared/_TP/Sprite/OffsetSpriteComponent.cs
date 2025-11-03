using System.Numerics;

namespace Content.Shared._TP.Sprite;

/// <summary>
///     Allows sprites to have different offsets for different directions.
///     Created by Cookie for Trieste Port 14
/// </summary>
[RegisterComponent]
public sealed partial class OffsetSpriteComponent: Component
{
    /// <summary>
    ///     A dictionary of offsets for each direction.
    /// </summary>
    [DataField("offsets")]
    public Dictionary<Direction, Vector2> DirectionOffsets = new();
}
