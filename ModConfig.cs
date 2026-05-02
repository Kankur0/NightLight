using StardewModdingAPI.Utilities;

namespace NightLight
{
    internal class ModConfig
    {
        public bool NightLightEnabled { get; set; } = true;
        public bool NightLightOutdoors {  get; set; } = true;
        public bool NightLightUnderground { get; set; } = true;
        public int OutdoorsDarkness { get; set; } = 0;
        public int UndergroundDarkness { get; set; } = 0;
        public KeybindList NightLightToggleAllKey { get; set; } = KeybindList.Parse("LeftAlt + L");
        public KeybindList NightLightToggleOutdoorsKey { get; set; } = KeybindList.Parse("LeftAlt + O");
        public KeybindList NightLightToggleUndergroundKey { get; set; } = KeybindList.Parse("LeftAlt + U");
    }
}
