using StardewModdingAPI.Utilities;

namespace NightLight
{
    public class ModConfig
    {
        public bool NightLightEnabled { get; set; } = true;
        public int DarknessPercentage { get; set; } = 50;
        public bool NightLightOutdoors {  get; set; } = true;
        public bool NightLightUnderground { get; set; } = true;
        public KeybindList NightLightToggleAllKey { get; set; } = KeybindList.Parse("LeftAlt + L");
        public KeybindList NightLightToggleOutdoorsKey { get; set; } = KeybindList.Parse("LeftAlt + O");
        public KeybindList NightLightToggleUndergroundKey { get; set; } = KeybindList.Parse("LeftAlt + U");
    }
}
