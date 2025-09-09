using BepInEx;
using CG.Input;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VoidManager.MPModChecks;
using VoidManager.Utilities;
using static UnityEngine.GUILayout;
using static VoidManager.Utilities.GUITools;

namespace VoidManager.CustomGUI
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class GUIMain : MonoBehaviour, IShowCursorSource, IInputActionMapRequest
    {
        public static GUIMain Instance { get; internal set; }
        GameObject Background;
        GameObject MMCanvas;
        Image Image;
        public bool GUIActive = false;
        Rect Window;
        byte Tab = 0;

        internal List<VoidPlugin> mods = new();
        VoidPlugin selectedMod = null;

        List<VoidPlugin> NonVManMods = new();

        Rect ModListArea;
        Vector2 ModListScroll = Vector2.zero;

        Rect ModInfoArea;
        Vector2 ModInfoScroll = Vector2.zero;

        Rect PlayerListArea;
        Vector2 PlayerListScroll = Vector2.zero;
        public Player selectedPlayer { get; private set; }

        Rect PlayerModInfoArea;
        Vector2 PlayerModInfoScroll = Vector2.zero;
        internal List<PlayerSettingsMenu> playerSettings = new();

        Rect ModSettingsArea;
        Vector2 ModSettingsScroll = Vector2.zero;
        internal List<ModSettingsMenu> settings = new();
        ModSettingsMenu selectedSettings;


        internal void UpdateWindowSize()
        {
            float Height = Configs.MenuHeight.Value;
            float Width = Configs.MenuWidth.Value;
            float ModlistWidth = Configs.MenuListWidth.Value;
            float PlayerlistWidth = Configs.PlayerListWidth.Value;

            Window = new Rect((Screen.width * .5f - ((Screen.width * Width) / 2)), Screen.height * .5f - ((Screen.height * Height) / 2), Screen.width * Width, Screen.height * Height);
            ModListArea = new Rect(6, 43, Window.width * ModlistWidth, Screen.height * Height - 45);
            ModInfoArea = new Rect(ModListArea.width + 15, 43, (Screen.width * Width - (ModListArea.width + 11)) - 10, Screen.height * Height - 45);
            PlayerListArea = new Rect(6, 43, Window.width * PlayerlistWidth, Screen.height * Height - 45);
            PlayerModInfoArea = new Rect(PlayerListArea.width + 15, 43, (Screen.width * Width - (PlayerListArea.width + 11)) - 10, Screen.height * Height - 45);
            ModSettingsArea = new Rect(6, 43, Screen.width * Width - 12, Screen.height * Height - 45);
        }

        internal GUIMain()
        {
            Instance = this;
            MMCanvas = new GameObject("ModManagerCanvas", new Type[] { typeof(Canvas) });
            Canvas canvasComponent = MMCanvas.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.sortingOrder = 1000;
            canvasComponent.transform.SetAsLastSibling();
            DontDestroyOnLoad(MMCanvas);

            UpdateWindowSize();
            settings.Add(new VManSettings());
            playerSettings.Add(new VManPlayerSettings());

            //Background image to block mouse clicks passing IMGUI
            Background = new GameObject("GUIMainBG", new Type[] { typeof(GraphicRaycaster) });
            Image = Background.AddComponent<UnityEngine.UI.Image>();
            Image.color = Color.clear;
            Background.transform.SetParent(MMCanvas.transform);
            Background.SetActive(false);
        }

        void Awake()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        }

        void Update()
        {
            if (Configs.OpenMenu.IsDown())
            {
                GUIActive = !GUIActive;
                if (GUIActive)
                {
                    GUIOpen();
                }
                else
                {
                    GUIClose();
                }
            }
        }

        void GUIOpen()
        {
            selectedSettings?.OnOpen(); //Menu Opening and MM selected
            if (Tab == 2 && selectedPlayer != null)
            {
                SelectPlayer(selectedPlayer);
            }
            
            GUIToggleCursor(true);
            Background.SetActive(true);
        }

        void GUIClose()
        {
            LeaveSettingsMenu();

            GUIToggleCursor(false);
            Background.SetActive(false);
        }

        void OnGUI()
        {
            if (GUIActive)
            {
                GUI.skin = ChangeSkin();
                Window = GUI.Window(999910, Window, WindowFunction, $"{MyPluginInfo.USERS_PLUGIN_NAME} F5 Menu");

                //float y = Window.center.y * 2 * -1;
                Image.rectTransform.position = new Vector3(Window.center.x, (Window.center.y * -1) + Screen.height, 0);
                Image.rectTransform.sizeDelta = Window.size;
            }
        }

        void WindowFunction(int WindowID)
        {

            BeginHorizontal(); // TAB Start
            {
                if (GUITools.DrawButtonSelected("Mod Info", Tab == 0))
                    ChangeTab(0);
                if (GUITools.DrawButtonSelected("Mod Settings", Tab == 1))
                    ChangeTab(1);
                if (GUITools.DrawButtonSelected("Player List", Tab == 2))
                    ChangeTab(2);
            }
            EndHorizontal(); // TAB End
            switch (Tab)
            {
                #region ModList and ModInfo
                case 0:
                    BeginArea(ModListArea);
                    {
                        ModListScroll = BeginScrollView(ModListScroll);
                        {
                            if (GUITools.DrawButtonSelected(MyPluginInfo.USERS_PLUGIN_NAME, selectedMod == null))
                            {
                                selectedMod = null;
                            }
                            foreach (VoidPlugin vp in mods)
                            {
                                DrawModListModButton(vp);
                            }
                            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                            Label($"<color={SessionMPTypeColorCode}>Non-{MyPluginInfo.USERS_PLUGIN_NAME} Mods</color>");
                            foreach (VoidPlugin vp in NonVManMods)
                            {
                                DrawModListModButton(vp);
                            }
                            Label("Overall MPType: " + GetColoredMPTypeText(MPModCheckManager.Instance.HighestLevelOfMPMods));
                            Label("Session Status: " + (MPModCheckManager.IsMod_Session() ? $"<color={SessionMPTypeColorCode}>Mod_Session</color>" : $"<color={ClientMPTypeColorCode}>Mod_Local</color>"));
                        }
                        EndScrollView();
                    }
                    EndArea();
                    GUI.skin.label.alignment = Configs.ModInfoTextAnchor.Value;
                    BeginArea(ModInfoArea);
                    {
                        ModInfoScroll = BeginScrollView(ModInfoScroll);
                        {
                            if (selectedMod != null)
                            {
                                BepInPlugin bepInPlugin = selectedMod.BepinPlugin.Metadata;
                                Label($"Author: {selectedMod.Author}");
                                Label($"Name: {bepInPlugin.Name}");
                                if (Configs.DebugMode.Value)
                                    Label($"GUID: {bepInPlugin.GUID}");
                                Label($"Version: {bepInPlugin.Version}");
                                if (selectedMod.Description != string.Empty)
                                    Label($"Description: {selectedMod.Description}");
                                Label($"MPRequirement: {GetTextForMPType(selectedMod.MPType)}");
                                Label("\nSettings menus:");
                                foreach (ModSettingsMenu MSM in settings)
                                {
                                    if (MSM.MyVoidPlugin == selectedMod && Button(MSM.Name()))
                                    {
                                        OpenSettingsMenu(MSM);
                                    }
                                }
                            }
                            else
                            {
                                //VoidManager about page when no mod selected.
                                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                                Label($"{MyPluginInfo.USERS_PLUGIN_NAME} - BepInEx Plugin Manager for Void Crew.");
                                Label("Provides APIs to developers and multiplayer mod management.");
                                Label($"Version: {MyPluginInfo.PLUGIN_VERSION}");
                                Label($"\n\nDeveloped by {MyPluginInfo.PLUGIN_AUTHORS}");
                                Label($"Based on the 'Pulsar Mod Loader' developed by Tom Ritcher");
                                BeginHorizontal();
                                FlexibleSpace();
                                if (Button("Github"))
                                    Application.OpenURL("https://github.com/Void-Crew-Modding-Team/VoidManager");
                                if (Button("Discord"))
                                    Application.OpenURL("https://discord.gg/4QhRRBWsJz");
                                FlexibleSpace();
                                EndHorizontal();
                                if (Button($"{MyPluginInfo.USERS_PLUGIN_NAME} Settings"))
                                {
                                    OpenSettingsMenu(settings[0]);
                                }
                            }
                        }
                        EndScrollView();
                    }
                    EndArea();
                    break;
                #endregion
                #region ModSettings
                case 1:
                    GUI.skin.label.alignment = Configs.ModInfoTextAnchor.Value;
                    BeginArea(ModSettingsArea);
                    {
                        ModSettingsScroll = BeginScrollView(ModSettingsScroll);
                        {
                            if (selectedSettings == null)
                            {
                                foreach (ModSettingsMenu menu in settings)
                                {
                                    if (Button(menu.Name()))
                                    {
                                        OpenSettingsMenu(menu);
                                    }
                                }
                            }
                            else
                            {
                                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                                Label(selectedSettings.Name());
                                GUI.skin.label.alignment = Configs.ModInfoTextAnchor.Value;
                                if (Button("Back"))
                                {
                                    LeaveSettingsMenu();
                                    selectedSettings = null;
                                }
                                else
                                {
                                    selectedSettings.Draw();
                                }
                            }
                        }
                        EndScrollView();
                    }
                    EndArea();
                    break;
                #endregion
                #region Player List
                case 2:
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    BeginArea(PlayerListArea);
                    {
                        PlayerListScroll = BeginScrollView(PlayerListScroll);
                        {
                            if (!PhotonNetwork.InRoom || PhotonNetwork.PlayerList.Count() == 0)
                            {
                                GUILayout.Label("No room found.");
                                EndScrollView();
                                EndArea();
                                break;
                            }
                            foreach (Player player in PhotonNetwork.PlayerList)
                            {
                                /*if (player.IsLocal)
                                    continue;*/
                                if (GUITools.DrawButtonSelected(player.NickName, selectedPlayer == player))
                                    SelectPlayer(player);
                            }
                        }
                        EndScrollView();
                    }
                    EndArea();
                    BeginArea(PlayerModInfoArea);
                    {
                        PlayerModInfoScroll = BeginScrollView(PlayerModInfoScroll);
                        {
                            if (selectedPlayer != null)
                            {
                                BeginHorizontal();
                                Label($"Player: {selectedPlayer.NickName} {(selectedPlayer.IsMasterClient ? "(Host)" : string.Empty)}");
                                FlexibleSpace();
                                FlexibleSpace();
                                Configs.DisplayPlayerModList.Value = Toggle(Configs.DisplayPlayerModList.Value, "Mod List");
                                Configs.DisplayPlayerSettingsMenus.Value = Toggle(Configs.DisplayPlayerSettingsMenus.Value, "Mod Settings");
                                EndHorizontal();
                                if (Configs.DisplayPlayerModList.Value) DrawPlayerModList(selectedPlayer);
                                if (playerSettings.Count > 0 && Configs.DisplayPlayerSettingsMenus.Value)
                                {
                                    foreach (PlayerSettingsMenu menu in playerSettings)
                                    {
                                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                                        if (menu.Name() != string.Empty)
                                        { 
                                            BeginHorizontal();
                                            Label(menu.Name());/*, GUILayout.Width(_SelectedButtonStyle.CalcSize(new GUIContent(menu.Name())).x));*/
                                            FlexibleSpace();
                                            EndHorizontal();
                                        }
                                        else Space(20);
                                        BeginVertical();
                                        menu.Draw(selectedPlayer);
                                        EndVertical();
                                    }
                                }
                            }
                        }
                        EndScrollView();
                    }
                    EndArea();
                    break;
                    #endregion
            }
            GUI.DragWindow();
        }

        internal static GUISkin _cachedSkin;
        internal static GUIStyle _SelectedButtonStyle;
        internal static Texture2D _buttonBackground;
        internal static Texture2D _hbuttonBackground;
        private static readonly Color32 _classicMenuBackground = new Color32(32, 32, 32, 255);
        private static readonly Color32 _classicButtonBackground = new Color32(40, 40, 40, 255);
        //private static readonly Color32 _hoverButtonFromMenu = new Color32(18, 79, 179, 255);
        /// <summary>
        /// VoidManager's default GUISkin
        /// </summary>
        /// <returns></returns>
        public static GUISkin ChangeSkin()
        {
            if (_cachedSkin is null || _cachedSkin.window.active.background is null)
            {
                _cachedSkin = Instantiate(GUI.skin);
                Texture2D windowBackground = BuildTexFrom1Color(_classicMenuBackground);
                _cachedSkin.window.active.background = windowBackground;
                _cachedSkin.window.onActive.background = windowBackground;
                _cachedSkin.window.focused.background = windowBackground;
                _cachedSkin.window.onFocused.background = windowBackground;
                _cachedSkin.window.hover.background = windowBackground;
                _cachedSkin.window.onHover.background = windowBackground;
                _cachedSkin.window.normal.background = windowBackground;
                _cachedSkin.window.onNormal.background = windowBackground;

                _cachedSkin.window.hover.textColor = Color.white;
                _cachedSkin.window.onHover.textColor = Color.white;

                Color32 hoverbutton = new Color32(60, 60, 60, 255);

                _buttonBackground = BuildTexFrom1Color(_classicButtonBackground);
                _hbuttonBackground = BuildTexFrom1Color(hoverbutton);
                _cachedSkin.button.active.background = _buttonBackground;
                _cachedSkin.button.focused.background = _buttonBackground;
                _cachedSkin.button.hover.background = _hbuttonBackground;
                _cachedSkin.button.normal.background = _buttonBackground;
                //_cachedSkin.button.onActive.background = _buttonBackground;
                //_cachedSkin.button.onFocused.background = _buttonBackground;
                //_cachedSkin.button.onHover.background = _hbuttonBackground;
                //_cachedSkin.button.onNormal.background = _buttonBackground;

                //Remember to check out https://forum.unity.com/threads/focusing-gui-controls.20511/ and potentially replace this with better code.
                _SelectedButtonStyle = new GUIStyle(_cachedSkin.button);
                _SelectedButtonStyle.active.background = _hbuttonBackground;
                _SelectedButtonStyle.focused.background = _hbuttonBackground;
                _SelectedButtonStyle.normal.background = _hbuttonBackground;

                GUITools.ButtonMinSizeStyle = new GUIStyle(_cachedSkin.button);
                GUITools.ButtonMinSizeStyle.stretchWidth = false;

                Texture2D sliderBackground = BuildTexFrom1Color(new Color32(47, 79, 79, 255));
                _cachedSkin.horizontalSlider.active.background = sliderBackground;
                _cachedSkin.horizontalSlider.onActive.background = sliderBackground;
                _cachedSkin.horizontalSlider.focused.background = sliderBackground;
                _cachedSkin.horizontalSlider.onFocused.background = sliderBackground;
                _cachedSkin.horizontalSlider.hover.background = sliderBackground;
                _cachedSkin.horizontalSlider.onHover.background = sliderBackground;
                _cachedSkin.horizontalSlider.normal.background = sliderBackground;
                _cachedSkin.horizontalSlider.onNormal.background = sliderBackground;

                Texture2D sliderHandleBackground = BuildTexFrom1Color(new Color32(47, 79, 79, 255));
                _cachedSkin.horizontalSliderThumb.active.background = sliderHandleBackground;
                _cachedSkin.horizontalSliderThumb.onActive.background = sliderHandleBackground;
                _cachedSkin.horizontalSliderThumb.focused.background = sliderHandleBackground;
                _cachedSkin.horizontalSliderThumb.onFocused.background = sliderHandleBackground;
                _cachedSkin.horizontalSliderThumb.hover.background = sliderHandleBackground;
                _cachedSkin.horizontalSliderThumb.onHover.background = sliderHandleBackground;
                _cachedSkin.horizontalSliderThumb.normal.background = sliderHandleBackground;
                _cachedSkin.horizontalSliderThumb.onNormal.background = sliderHandleBackground;

                Texture2D textfield = BuildTexFromColorArray(new Color[] { _classicButtonBackground, _classicButtonBackground, _classicMenuBackground,
                _classicMenuBackground, _classicMenuBackground, _classicMenuBackground , _classicMenuBackground}, 1, 7);
                _cachedSkin.textField.active.background = textfield;
                _cachedSkin.textField.onActive.background = textfield;
                _cachedSkin.textField.focused.background = textfield;
                _cachedSkin.textField.onFocused.background = textfield;
                _cachedSkin.textField.hover.background = textfield;
                _cachedSkin.textField.onHover.background = textfield;
                _cachedSkin.textField.normal.background = textfield;
                _cachedSkin.textField.onNormal.background = textfield;

                _cachedSkin.textField.active.textColor = hoverbutton;
                _cachedSkin.textField.onActive.textColor = hoverbutton;
                _cachedSkin.textField.hover.textColor = hoverbutton;
                _cachedSkin.textField.onHover.textColor = hoverbutton;

                UnityEngine.Object.DontDestroyOnLoad(windowBackground);
                UnityEngine.Object.DontDestroyOnLoad(_buttonBackground);
                UnityEngine.Object.DontDestroyOnLoad(_hbuttonBackground);
                UnityEngine.Object.DontDestroyOnLoad(textfield);
                UnityEngine.Object.DontDestroyOnLoad(_cachedSkin);
                // TODO: Add custom skin for Toggle and other items
            }

            return _cachedSkin;
        }


        public const string AllMPTypeColorCode = "#FF3333";

        public const string SessionMPTypeColorCode = "#FFFF99";
        public const string HostMPTypeColorCode = "#00CC00";
        public const string ClientMPTypeColorCode = "#00CC00";

        /// <summary>
        /// HTML color codes for MPType colors.
        /// </summary>
        /// <param name="mptype"></param>
        /// <returns>HTML Color code</returns>
        public static string GetColorTextForMPType(MultiplayerType mptype)
        {
            switch (mptype)
            {
                case MultiplayerType.Host:
                    return HostMPTypeColorCode;
                case MultiplayerType.Client:
                    return ClientMPTypeColorCode;
                case MultiplayerType.Session:
                    return SessionMPTypeColorCode;
                case MultiplayerType.All:
                    return AllMPTypeColorCode;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Returns a string for the MPType in it's given color.
        /// </summary>
        /// <param name="mptype"></param>
        /// <returns></returns>
        public static string GetColoredMPTypeText(MultiplayerType mptype)
        {
            switch (mptype)
            {
                case MultiplayerType.All:
                    return $"<color={AllMPTypeColorCode}>All</color>";
                case MultiplayerType.Session:
                    return $"<color={SessionMPTypeColorCode}>Session</color>";
                case MultiplayerType.Host:
                    return $"<color={HostMPTypeColorCode}>Host</color>";
                case MultiplayerType.Client:
                    return $"<color={ClientMPTypeColorCode}>Client</color>";
                case MultiplayerType.Unmanaged:
                default:
                    return mptype.ToString();
            }
        }

        //Colored names and descriptions of MPTypes
        public static string GetTextForMPType(MultiplayerType mptype)
        {
            switch (mptype)
            {
                case MultiplayerType.All:
                    return $"<color={AllMPTypeColorCode}>All</color> - All Clients will be required to install this mod.";
                case MultiplayerType.Session:
                    return $"<color={SessionMPTypeColorCode}>Session</color> - Can only join/host Mod_Session, but doesn't require other players to install the mod.\n";
                case MultiplayerType.Host:
                    return $"<color={HostMPTypeColorCode}>Host</color> - General MPType for a host-side mod, allowed to join vanilla sessions.";
                case MultiplayerType.Client:
                    return $"<color={ClientMPTypeColorCode}>Client</color> - Client Side, allowed to join vanilla sessions.";
                case MultiplayerType.Unmanaged:
                    return $"<color=#FFFF99>Unmanaged</color> - This mod has not had it's multiplayer operations specified for {MyPluginInfo.USERS_PLUGIN_NAME}.";

                default:
                    return mptype.ToString();
            }
        }

        void DrawModListModButton(VoidPlugin voidPlugin)
        {
            if (voidPlugin.MPType >= MPModChecks.MultiplayerType.Session)
            {
                if (GUITools.DrawButtonSelected($"<color={GetColorTextForMPType(voidPlugin.MPType)}>{voidPlugin.BepinPlugin.Metadata.Name}</color>", selectedMod == voidPlugin)) //FFFF99
                    selectedMod = voidPlugin;
            }
            else
            {
                if (GUITools.DrawButtonSelected(voidPlugin.BepinPlugin.Metadata.Name, selectedMod == voidPlugin))
                    selectedMod = voidPlugin;
            }
        }

        void DrawPlayerModList(Player player)
        {
            MPUserDataBlock userData = NetworkedPeerManager.Instance.GetNetworkedPeerMods(player);
            if (userData != null)
            {
                Label($"User {MyPluginInfo.USERS_PLUGIN_NAME} version: {userData.VMVersion}");
                Label("ModList:");
                string ModListText = string.Empty;
                bool first = true;
                foreach (MPModDataBlock modData in userData.ModData)
                {
                    if (first)
                        first = false;
                    else
                        ModListText += "\n";

                    ModListText += $"- {modData.ModName} v{modData.Version}, MPType: {GetColoredMPTypeText(modData.MPType)}";
                }
                Label(ModListText);
            }
            else
            {
                Label("No Mod data.");
            }
        }

        public void OpenSettingsMenu(ModSettingsMenu menu)
        {
            if (Tab != 1)
                Tab = 1;
            else LeaveSettingsMenu();

            selectedSettings = menu;

            
            try
            {
                menu?.OnOpen();
            }
            catch (Exception e)
            {
                BepinPlugin.Log.LogError(e);
            }
        }

        public void LeaveSettingsMenu()
        {
            try
            {
                selectedSettings?.OnClose();
            }
            catch (Exception e)
            {
                BepinPlugin.Log.LogError(e);
            }
            GUITools.keybindToChange = null;
        }

        public void SelectPlayer(Player player)
        {
            selectedPlayer = player;
            foreach (PlayerSettingsMenu menu in playerSettings)
            {
                menu.Refresh(selectedPlayer);
            }
        }

        public void ChangeTab(byte tab)
        {
            if (Tab == 1 && tab != 1)
            {
                LeaveSettingsMenu();
            }

            Tab = tab;
        }

        /// <summary>
        /// Iterates through the current Plugin files and searches for gui menus.
        /// </summary>
        internal void DiscoverGUIMenus(System.Reflection.Assembly assembly, VoidPlugin voidPlugin)
        {
            mods.Add(voidPlugin);
            Type[] types = assembly.GetTypes();

            // Finds settings menu implementations from all the Assemblies in the same file location.
            IEnumerable<Type> modSettingsMenuInstances = types.Where(t => typeof(ModSettingsMenu).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            bool hasSettingsMenu = false;
            foreach (Type modType in modSettingsMenuInstances)
            { // Iterates through each discovered gui menu
                ModSettingsMenu MSMInstance = (ModSettingsMenu)Activator.CreateInstance(modType);
                MSMInstance.MyVoidPlugin = voidPlugin;
                settings.Add(MSMInstance);
                hasSettingsMenu = true;
            }
            if (hasSettingsMenu) BepinPlugin.Log.LogInfo($"[{voidPlugin.BepinPlugin.Metadata.Name}] detected settings menu(s)");

            // Finds player settings menu implementations from all the Assemblies in the same file location.
            IEnumerable<Type> playerSettingsMenuInstances = types.Where(t => typeof(PlayerSettingsMenu).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            hasSettingsMenu = false;
            foreach (Type modType in playerSettingsMenuInstances)
            { // Iterates through each discovered gui menu
                PlayerSettingsMenu PSMInstance = (PlayerSettingsMenu)Activator.CreateInstance(modType);
                PSMInstance.MyVoidPlugin = voidPlugin;
                playerSettings.Add(PSMInstance);
                hasSettingsMenu = true;
            }
            if (hasSettingsMenu) BepinPlugin.Log.LogInfo($"[{voidPlugin.BepinPlugin.Metadata.Name}] detected player settings menu(s)");
        }

        internal void DiscoverNonVoidManagerMod(VoidPlugin voidPlugin)
        {
            NonVManMods.Add(voidPlugin);
        }

        bool ShowingCursor;

        void GUIToggleCursor(bool enable)
        {
            if (!Configs.MenuUnlockCursor.Value && !(!enable && ShowingCursor))
            {
                return; // Stop early if unlocking cursor is disabled, but allow passthrough if cursor is enabled and is getting set to disabled.
            }

            ShowingCursor = enable;
            CursorUtility.ShowCursor(this, enable);

            if (ShowingCursor)
            {
                InputActionMapRequests.AddOrChangeRequest(this, InputStateRequestType.UI);
            }
            else
            {
                InputActionMapRequests.RemoveRequest(this);
            }
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
