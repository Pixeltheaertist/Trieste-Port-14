using Content.Server._TP.StationEvents.Events;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._TP.StationEvents.Components;


[RegisterComponent, Access(typeof(FlashStormRule))]
public sealed partial class FlashStormRuleComponent : Component
{

   // The weather that is enabled during the storm
   [DataField(required: true)]
   public EntProtoId StormWeather = "WeatherStorm";

   // The weather it will revert to at the end of the storm
   [DataField(required: true)]
   public EntProtoId NormalWeather = "WeatherRain";

   [DataField("flickering")]
   public bool Flickering;

   [DataField("station")]
   public EntityUid TrueStation = EntityUid.Invalid;

     [DataField("stormMusic")]
     public SoundSpecifier StormMusic = new SoundCollectionSpecifier("StormMusic");
}

