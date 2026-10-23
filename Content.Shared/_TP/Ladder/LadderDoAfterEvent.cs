using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._TP.Ladder;

[Serializable, NetSerializable]
public sealed partial class LadderDoAfterEvent : SimpleDoAfterEvent;
