using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Content.Shared.Weather;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Audio;


namespace Content.Server.StationEvents.Components;


[RegisterComponent, Access(typeof(FlashStormRule))]
public sealed partial class FlashStormRuleComponent : Component
{

   // The weather that is enabled during the storm
   [DataField("stormWeather", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<WeatherPrototype>))]
   public string StormWeather = "Storm";

   // The weather it will revert to at the end of the storm
   [DataField("normalWeather", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<WeatherPrototype>))]
   public string NormalWeather = "Rain";

   [DataField("flickering")]
   public bool Flickering = false;

   [DataField("station")]
   public EntityUid trueStation = EntityUid.Invalid;

     [DataField("stormMusic")]
     public SoundSpecifier StormMusic = new SoundCollectionSpecifier("StormMusic");
}

