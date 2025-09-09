using BepInEx.Configuration;
using ExitGames.Client.Photon;
using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidManager
{
    internal class Configs
    {
        // General
        public static ConfigEntry<bool> DebugMode;
        internal static ConfigEntry<string> UnspecifiedModListOverride;
        internal static Dictionary<string, MPModChecks.MultiplayerType> ModOverrideDictionary;

        /// <summary>
        /// Safe way to retrieve current DebugMode status.
        /// </summary>
        public static bool IsDebugMode
        {
            get { return DebugMode.Value; }
        }

        public static void SetDefault()
        {
            ModInfoTextAnchor.Value = TextAnchor.UpperLeft;
        }

        //Menu
        internal static ConfigEntry<float> MenuHeight;
        internal static ConfigEntry<float> MenuWidth;
        internal static ConfigEntry<float> MenuListWidth;
        internal static ConfigEntry<float> PlayerListWidth;
        internal static ConfigEntry<bool> MenuUnlockCursor;
        public static ConfigEntry<UnityEngine.TextAnchor> ModInfoTextAnchor;

        internal static ConfigEntry<KeyboardShortcut> MenuOpenKeybind;
        internal static KeyboardShortcut OpenMenu = new KeyboardShortcut(KeyCode.F5);

        internal static ConfigEntry<bool> DisplayPlayerModList;
        internal static ConfigEntry<bool> DisplayPlayerSettingsMenus;

        //Photon Logging
        internal static ConfigEntry<PunLogLevel> PunLoggingSettingLevel;
        internal static ConfigEntry<DebugLevel> PunDebugLogLevel;

        //FMod
        internal static ConfigEntry<bool> EnableLiveUpdate;
        internal static ConfigEntry<ushort> LiveUpdatePort;

        internal static void LoadModListOverride()
        {
            ModOverrideDictionary = new Dictionary<string, MPModChecks.MultiplayerType>();
            if (UnspecifiedModListOverride.Value == string.Empty)
                return;
            string[] inputs = UnspecifiedModListOverride.Value.Split(',');
            foreach (string value in inputs)
            {
                if (value.EndsWith(":all", StringComparison.CurrentCultureIgnoreCase))
                {
                    ModOverrideDictionary.Add(value.Substring(0, value.Length - 4), MPModChecks.MultiplayerType.All);
                }
                else if (value.EndsWith(":client", StringComparison.CurrentCultureIgnoreCase))
                {
                    ModOverrideDictionary.Add(value.Substring(0, value.Length - 7), MPModChecks.MultiplayerType.Client);
                }
                else if (value.EndsWith(":host", StringComparison.CurrentCultureIgnoreCase))
                {
                    ModOverrideDictionary.Add(value.Substring(0, value.Length - 5), MPModChecks.MultiplayerType.Host);
                }
                else if (value.EndsWith(":h", StringComparison.CurrentCultureIgnoreCase))
                {
                    ModOverrideDictionary.Add(value.Substring(0, value.Length - 2), MPModChecks.MultiplayerType.Hidden);
                }
                else
                {
                    BepinPlugin.Log.LogError($"Unspecified Mod Override - '{value}' is not a valid input.");
                }
            }
        }

        internal static void Load(BepinPlugin plugin)
        {
            DebugMode = plugin.Config.Bind("General", "DebugMode", false, "");
            UnspecifiedModListOverride = plugin.Config.Bind("General", "Unspecified Mod Overrides", string.Empty, $"Insert mods (not configured for {MyPluginInfo.USERS_PLUGIN_NAME}) for which you would like to override the MPType. \nAvailable MPTypes: client,host,all \nFormat: 'ModNameOrGUID:MPType', delineated by ','. \nEx: {MyPluginInfo.USERS_PLUGIN_NAME}:all,Better Scoop:Host \n ModName/GUID can be gathered from log files and F5 menu.");

            ModInfoTextAnchor = plugin.Config.Bind("Menu", "ModInfoTextAnchor", TextAnchor.UpperLeft, "");

            MenuHeight = plugin.Config.Bind("Menu", "Height", .50f, "");
            MenuWidth = plugin.Config.Bind("Menu", "Width", .50f, "");
            MenuListWidth = plugin.Config.Bind("Menu", "List Width", .30f, "");
            PlayerListWidth = plugin.Config.Bind("Menu", "Player List Width", .30f, "");
            MenuUnlockCursor = plugin.Config.Bind("Menu", "Unlock Cursor", true, "");

            MenuOpenKeybind = plugin.Config.Bind("Menu", "Open Keybind", OpenMenu, "");
            DisplayPlayerModList = plugin.Config.Bind("Menu", "Player Mod List", false, "Display in the Player List GUI");
            DisplayPlayerSettingsMenus = plugin.Config.Bind("Menu", "Player Settings Menus", true, "Display in the Player List GUI");

            PunLoggingSettingLevel = plugin.Config.Bind("Debug", "PunLogLevel", PunLogLevel.ErrorsOnly);
            PunDebugLogLevel = plugin.Config.Bind("Debug", "PunDebugLevel", DebugLevel.ERROR);

            EnableLiveUpdate = plugin.Config.Bind("FMOD", "EnableLiveUpdate", false, "Enables FMOD live update service for connection with FMOD Studio");
            LiveUpdatePort = plugin.Config.Bind("FMOD", "LiveUpdatePort", (ushort)9264);
        }
    }
}
