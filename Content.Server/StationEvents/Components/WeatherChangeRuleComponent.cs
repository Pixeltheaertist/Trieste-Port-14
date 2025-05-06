using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Content.Shared.Weather;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Audio;


namespace Content.Server.StationEvents.Components;


[RegisterComponent, Access(typeof(WeatherChangeRule))]
public sealed partial class WeatherChangeRuleComponent : Component
{
   // The weather that is enabled during the storm
   [DataField("targetWeather", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<WeatherPrototype>))]
   public string TargetWeather = "Storm";

   // The weather it will revert to at the end of the storm
   [DataField("returnWeather", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<WeatherPrototype>))]
   public string ReturnWeather = "Rain";

   // Is lightning enabled during the storm?
   [DataField("lightning")]
   public bool Lightning = true;

   [DataField("sunlight")]
   public bool Sunlight = false;

   [DataField("sunlightColor")]
   public Color SunlightColor = Color.FromHex("#D8B059");
}

