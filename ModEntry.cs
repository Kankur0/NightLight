using System;
using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace NightLight
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        // Initialize the GMCM object
        private ModConfig Config = new();

        /*********
        ** Public methods
        *********/
        // <summary>The mod entry point, called after the mod is first loaded.</summary>
        // <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {

            // Set the mod's config
            Config = helper.ReadConfig<ModConfig>();

            // Set up events
            helper.Events.GameLoop.GameLaunched += GameLaunched;
            helper.Events.Input.ButtonsChanged += ButtonsChanged;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (Config.NightLightEnabled) {
                handleLighting();
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player pressed/released any buttons on the keyboard, mouse, or controller. This includes mouse clicks. If the player pressed/released multiple keys at once, this is only raised once.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void ButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            // Ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady || !Config.NightLightEnabled) return;

            // Toggle all locations
            if (this.Config.NightLightToggleAllKey.JustPressed()) {
                // If any of the three location types are currently disabled, enable all of them
                bool allOn = this.Config.NightLightOutdoors && this.Config.NightLightUnderground && this.Config.NightLightIndoors;

                bool targetState = !allOn;

                // Set all location types to the target state
                this.Config.NightLightOutdoors = targetState;
                this.Config.NightLightUnderground = targetState;
                this.Config.NightLightIndoors = targetState;

                // Save the config after toggling all
                this.Helper.WriteConfig(this.Config);
            }
            else {
                // Toggle outdoors only
                if (this.Config.NightLightToggleOutdoorsKey.JustPressed()) {
                    ToggleFeature(this.Config.NightLightOutdoors, val => this.Config.NightLightOutdoors = val);
                }
                // Toggle underground only
                if (this.Config.NightLightToggleUndergroundKey.JustPressed()) {
                    ToggleFeature(this.Config.NightLightUnderground, val => this.Config.NightLightUnderground = val);
                }
                // Toggle indoors only
                if (this.Config.NightLightToggleIndoorsKey.JustPressed()) {
                    ToggleFeature(this.Config.NightLightIndoors, val => this.Config.NightLightIndoors = val);
                }
            }
            
        }

        private void ToggleFeature(bool currentValue, Action<bool> setter) {
            setter(!currentValue);
            this.Helper.WriteConfig(this.Config);
        }

        private void handleLighting() {

            // If mod is disabled, don't do anything
            if (!Config.NightLightEnabled) return;

            // Handle underground lighting
            if (Config.NightLightUnderground) {
                handleUnderground();
            }

            var location = Game1.currentLocation;
            bool isOutdoors = location.IsOutdoors;
            bool isUnderground = location.Name.StartsWith("UndergroundMine") || location.Name == "FarmCave";
            bool isIndoors = !isOutdoors && !isUnderground;

            if ((this.Config.NightLightOutdoors && isOutdoors) || (this.Config.NightLightIndoors && isIndoors)) {
                applyLighting();
            }
        }

        private void handleUnderground()
        {
            // Toggle lighting within the mines and farm cave
            if (Game1.currentLocation.Name.StartsWith("UndergroundMine") || Game1.currentLocation.Name == "FarmCave")
            {
                Game1.drawLighting = false;
            }
        }

        private void applyLighting() {
            // Get the base light color so we can apply the darkness factor to it, but only at night
            Color baseLight = Game1.outdoorLight.A > 0 ? Game1.outdoorLight : Game1.ambientLight;
            // Calculate the intensity of the darkness with a value between 0 and 1, where 0 is daytime and 1 is the darkest night
            float darknessIntensity = baseLight.A / 255f;
            // Convert the user's preferred to a percentage factor where 1 is the default night and 0 is no darkness at all
            float userFactor = (float)Config.DarknessPercentage / 100f;

            // If it is daytime, don't apply any changes to the ambient light
            if (darknessIntensity < 0.01f) return;

            // Don't apply changes to the ambient light if the player is in a cutscene, warping, or if the HUD is frozen or hidden, as this can cause visual issues
            if (Game1.eventUp || Game1.isWarping || Game1.viewportFreeze) return;

            // Calculate the final factor to apply to the ambient light based on the user's desired darkness percentage and the current darkness intensity
            float finalFactor = 1f + (userFactor - 1f) * darknessIntensity;

            // Multiply the base light color by the final factor to get the adjusted light color, while ensuring that the RGB values don't exceed 255
            Color adjustedLight = new Color(
                (byte)(MathHelper.Clamp(baseLight.R * finalFactor, 0, 255)),
                (byte)(MathHelper.Clamp(baseLight.G * finalFactor, 0, 255)),
                (byte)(MathHelper.Clamp(baseLight.B * finalFactor, 0, 255)),
                baseLight.A // Don't change the transparency
            );

            // Apply the adjusted light color to the ambient light
            Game1.ambientLight = adjustedLight;
        }

        private void GameLaunched(object sender, GameLaunchedEventArgs e) {
            // Get GMCM API
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            // Make sure it was found
            if (configMenu is null) {
                return;
            }

            // Register NightLight Mod
            configMenu.Register(
                    mod: this.ModManifest,
                    reset: () => this.Config = new ModConfig(),
                    save: () => this.Helper.WriteConfig(this.Config)
            );

            // Config Options
            // Enable/Disable
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Enable NightLight",
                tooltip: () => "Enables NightLight",
                getValue: () => this.Config.NightLightEnabled,
                setValue: value => this.Config.NightLightEnabled = value
            );

            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => (float)this.Config.DarknessPercentage,
                setValue: value => this.Config.DarknessPercentage = (int)value,
                name: () => "Darkness",
                tooltip: () => "Changes how much darkness to retain at night time (100 is default night)",
                min: 0f,
                max: 100f,
                interval:1f
            );

            // Section Title For Areas
            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => "Areas To Light",
                tooltip: () => "Toggle which areas you would like to apply the darkness setting to."
            );

            // Outdoors NightLight
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Outdoors",
                tooltip: () => "Applies darkness setting while outside at night.",
                getValue: () => this.Config.NightLightOutdoors,
                setValue: value => this.Config.NightLightOutdoors = value
            );

            // Underground NightLight
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Underground",
                tooltip: () => "Applies darkness setting to all floors of the mines (most notable on floors 30-39 of the regular mines) and inside the farm cave.",
                getValue: () => this.Config.NightLightUnderground,
                setValue: value => this.Config.NightLightUnderground = value
            );

            // Indoors NightLight
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Indoors",
                tooltip: () => "Applies darkness setting to indoor areas if they are darkened at night (Like the farmhouse)",
                getValue: () => this.Config.NightLightIndoors,
                setValue: value => this.Config.NightLightIndoors = value
            );

            // Section Title For Hotkeys
            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => "Hotkeys",
                tooltip: () => "View or change the hotkeys to light up one or more areas."
            );

            // Toggle All Hotkey
            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => "Toggle All",
                tooltip: () => "Set the keybind to toggle NightLight on/off for all areas.",
                getValue: () => this.Config.NightLightToggleAllKey,
                setValue: value => this.Config.NightLightToggleAllKey = value
            );

            // Toggle Outdoors Only Hotkey
            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => "Toggle Outdoors",
                tooltip: () => "Set the keybind to toggle NightLight on/off for outdoors only.",
                getValue: () => this.Config.NightLightToggleOutdoorsKey,
                setValue: value => this.Config.NightLightToggleOutdoorsKey = value
            );

            // Toggle Underground Only Hotkey
            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => "Toggle Underground",
                tooltip: () => "Set the keybind to toggle NightLight on/off for underground areas only.",
                getValue: () => this.Config.NightLightToggleUndergroundKey,
                setValue: value => this.Config.NightLightToggleUndergroundKey = value
            );

            // Toggle Indoors Only Hotkey
            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => "Toggle Indoors",
                tooltip: () => "Set the keybind to toggle NightLight on/off for iondoor areas only.",
                getValue: () => this.Config.NightLightToggleIndoorsKey,
                setValue: value => this.Config.NightLightToggleIndoorsKey = value
            );
        }
    }
}