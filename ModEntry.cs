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
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            if (Config.NightLightEnabled) {
                /*
                 ### Toggle NightLight if configured hotkey button is pressed ###
                */

                // Outdoors only 
                if (this.Config.NightLightToggleOutdoorsKey.JustPressed()) {
                    if (this.Config.NightLightOutdoors == true) {
                        this.Config.NightLightOutdoors = false;
                    } else {
                        this.Config.NightLightOutdoors = true;
                    }

                    this.Helper.WriteConfig(this.Config);
                }

                // Underground only
                if (this.Config.NightLightToggleUndergroundKey.JustPressed()) {
                    if (this.Config.NightLightUnderground == true) {
                        this.Config.NightLightUnderground = false;
                    } else {
                        this.Config.NightLightUnderground = true;
                    }

                    this.Helper.WriteConfig(this.Config);
                }

                // Outdoors & Underground
                if (this.Config.NightLightToggleAllKey.JustPressed()) {
                    // Outdoors
                    if (this.Config.NightLightOutdoors == true) {
                        this.Config.NightLightOutdoors = false;
                    } else {
                        this.Config.NightLightOutdoors = true;
                    }

                    // Underground
                    if (this.Config.NightLightUnderground == true) {
                        this.Config.NightLightUnderground = false;
                    } else {
                        this.Config.NightLightUnderground = true;
                    }

                    this.Helper.WriteConfig(this.Config);

                }
            }
        }

        private void handleLighting() {

            // If mod is disabled, don't do anything
            if (!Config.NightLightEnabled) {
                return;
            }

            // Handle underground lighting
            if (Config.NightLightUnderground) {
                handleUnderground();
            }
            if (Config.NightLightOutdoors && Game1.currentLocation.IsOutdoors) {
                applyLighting();
            }
        }

        private void handleUnderground()
        {
            // Toggle lighting within the mines and farm cave
            if (Config.NightLightUnderground)
            {
                if (Game1.currentLocation.Name.StartsWith("UndergroundMine") || Game1.currentLocation.Name == "FarmCave")
                {
                    Game1.drawLighting = false;
                }
            }
        }

        private void applyLighting() {
            Color baseLight = Game1.outdoorLight;
            float darknessIntensity = baseLight.A / 255f;
            float userFactor = (float)Config.DarknessPercentage / 100f;

            // If it is daytime, don't apply any changes to the ambient light
            if (darknessIntensity < 0.01f || baseLight.R > 250 && baseLight.G > 250 && baseLight.B > 250) return;

            // Don't apply changes to the ambient light if the player is in a cutscene, warping, or if the HUD is frozen or hidden, as this can cause visual issues
            if (Game1.eventUp || Game1.isWarping || Game1.viewportFreeze || Game1.displayHUD == false) return;

            // Calculate the final factor to apply to the ambient light based on the user's desired darkness percentage and the current darkness intensity
            float finalFactor = 1f + (userFactor - 1f) * darknessIntensity;

            // Multiply the current ambient light by the factor to get the new ambient light color
            Color adjustedLight = new Color(
                (byte)(baseLight.R * finalFactor),
                (byte)(baseLight.G * finalFactor),
                (byte)(baseLight.B * finalFactor),
                baseLight.A // Don't change the transparency
            );

            Game1.outdoorLight = adjustedLight;
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
                tooltip: () => "Toggle which areas you would like to light up."
            );

            // Outdoors NightLight
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Outdoors",
                tooltip: () => "Enables light while outside at all times.",
                getValue: () => this.Config.NightLightOutdoors,
                setValue: value => this.Config.NightLightOutdoors = value
            );

            // Underground NightLight
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Underground",
                tooltip: () => "Enables light on all floors of the mines (most notable on floors 30-39 of the regular mines) and inside the farm cave.",
                getValue: () => this.Config.NightLightUnderground,
                setValue: value => this.Config.NightLightUnderground = value
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
                tooltip: () => "Set the keybind to toggle NightLight on/off for both outdoors and underground areas.",
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
        }
    }
}