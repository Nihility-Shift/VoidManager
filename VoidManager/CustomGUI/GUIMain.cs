﻿using BepInEx;
using CG.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VoidManager.MPModChecks;
using static UnityEngine.GUILayout;

namespace VoidManager.CustomGUI
{
    class GUIMain : MonoBehaviour, IShowCursorSource, IInputActionMapRequest
    {
        public static GUIMain Instance { get; internal set; }
        GameObject Background;
        GameObject MMCanvas;
        UnityEngine.UI.Image Image;
        public bool GUIActive = false;
        Rect Window;
        byte Tab = 0;

        List<VoidPlugin> mods = new();
        VoidPlugin selectedMod = null;

        List<VoidPlugin> NonVManMods = new();

        Rect ModListArea;
        Vector2 ModListScroll = Vector2.zero;

        Rect ModInfoArea;
        Vector2 ModInfoScroll = Vector2.zero;

        Rect ModSettingsArea;
        Vector2 ModSettingsScroll = Vector2.zero;
        List<ModSettingsMenu> settings = new();
        ushort selectedSettings = ushort.MaxValue;

        internal void updateWindowSize()
        {
            float Height = BepinPlugin.Bindings.MenuHeight.Value;
            float Width = BepinPlugin.Bindings.MenuWidth.Value;
            float ModlistWidth = BepinPlugin.Bindings.MenuListWidth.Value;

            Window = new Rect((Screen.width * .5f - ((Screen.width * Width) / 2)), Screen.height * .5f - ((Screen.height * Height) / 2), Screen.width * Width, Screen.height * Height);
            ModListArea = new Rect(6, 43, Window.width * ModlistWidth, Screen.height * Height - 45);
            ModInfoArea = new Rect(ModListArea.width + 15, 43, (Screen.width * Width - (ModListArea.width + 11)) - 10, Screen.height * Height - 45);
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

            updateWindowSize();
            settings.Add(new VManSettings());

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
            if (BepinPlugin.Bindings.OpenMenu.IsDown())
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
            if (selectedSettings != ushort.MaxValue) //Menu Opening and MM selected
            {
                settings[selectedSettings].OnOpen();
            }
            GUIToggleCursor(true);
            Background.SetActive(true);
        }

        void GUIClose()
        {
            if (selectedSettings != ushort.MaxValue) //Menu Closing and MM Selected
            {
                settings[selectedSettings].OnClose();
            }
            GUIToggleCursor(false);
            Background.SetActive(false);
        }

        void OnGUI()
        {
            if (GUIActive)
            {
                GUI.skin = ChangeSkin();
                Window = GUI.Window(999910, Window, WindowFunction, "ModManager");

                //float y = Window.center.y * 2 * -1;
                Image.rectTransform.position = new Vector3(Window.center.x, (Window.center.y * -1) + Screen.height, 0);
                Image.rectTransform.sizeDelta = Window.size;
            }
        }

        void WindowFunction(int WindowID)
        {

            BeginHorizontal(); // TAB Start
            {
                if (Button("Mod Info"))
                    Tab = 0;
                if (Button("Mod Settings"))
                    Tab = 1;
                if (Button("About"))
                    Tab = 2;
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
                            foreach (VoidPlugin vp in mods)
                            {
                                DrawModButton(vp);
                            }
                            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                            Label("<color=yellow>Non-VoidManager Mods</color>");
                            foreach (VoidPlugin vp in NonVManMods)
                            {
                                DrawModButton(vp);
                            }
                        }
                        EndScrollView();
                    }
                    EndArea();
                    GUI.skin.label.alignment = BepinPlugin.Bindings.ModInfoTextAnchor.Value;
                    BeginArea(ModInfoArea);
                    {
                        if (selectedMod != null)
                        {
                            ModInfoScroll = BeginScrollView(ModInfoScroll);
                            {
                                BepInPlugin bepInPlugin = selectedMod.BepinPlugin.Metadata;
                                Label($"Author: {selectedMod.Author}");
                                Label($"Name: {bepInPlugin.Name}");
                                Label($"Version: {bepInPlugin.Version}");
                                if (selectedMod.Description != string.Empty)
                                    Label($"Description: {selectedMod.Description}");
                                Label($"MPRequirement: {GetTextForMPType(selectedMod.MPType)}");
                            }
                            EndScrollView();
                        }
                    }
                    EndArea();
                    break;
                #endregion
                #region ModSettings
                case 1:
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                    BeginArea(ModSettingsArea);
                    {
                        ModSettingsScroll = BeginScrollView(ModSettingsScroll);
                        {
                            if (selectedSettings == ushort.MaxValue)
                            {
                                for (ushort msm = 0; msm < settings.Count; msm++)
                                {
                                    if (Button(settings[msm].Name()))
                                    {
                                        settings[msm].OnOpen();
                                        selectedSettings = msm;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (Button("Back"))
                                {
                                    settings[selectedSettings].OnClose();
                                    selectedSettings = ushort.MaxValue;
                                }
                                else
                                {
                                    settings[selectedSettings].Draw();
                                }
                            }
                        }
                        EndScrollView();
                    }
                    EndArea();
                    break;
                #endregion
                #region About
                case 2:
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    Label($"VoidManager - BepInEx Plugin Manager for Void Crew.");
                    Label($"Version: {MyPluginInfo.PLUGIN_VERSION}");
                    Label($"\n\nDeveloped by Mest and Dragon");
                    Label($"Based on the 'Pulsar Mod Loader' developed by Tom Ritcher");
                    BeginHorizontal();
                    if (Button("Github"))
                        Application.OpenURL("https://github.com/Void-Crew-Modding-Team/VoidManager");
                    if (Button("Discord"))
                        Application.OpenURL("https://discord.gg/4QhRRBWsJz");
                    EndHorizontal();
                    break;
                    #endregion
            }
            GUI.DragWindow();
        }

        internal static GUISkin _cachedSkin;
        private static readonly Color32 _classicMenuBackground = new Color32(32, 32, 32, 255);
        private static readonly Color32 _classicButtonBackground = new Color32(40, 40, 40, 255);
        private static readonly Color32 _hoverButtonFromMenu = new Color32(18, 79, 179, 255);
        GUISkin ChangeSkin()
        {
            if (_cachedSkin is null || _cachedSkin.window.active.background is null)
            {
                _cachedSkin = GUI.skin;
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

                Color32 hoverbutton = new Color32(255, 255, 0, 255);

                Texture2D buttonBackground = BuildTexFrom1Color(_classicButtonBackground);
                Texture2D hbuttonBackground = BuildTexFrom1Color(hoverbutton);
                _cachedSkin.button.active.background = buttonBackground;
                _cachedSkin.button.onActive.background = buttonBackground;
                _cachedSkin.button.focused.background = buttonBackground;
                _cachedSkin.button.onFocused.background = buttonBackground;
                _cachedSkin.button.hover.background = hbuttonBackground;
                _cachedSkin.button.onHover.background = hbuttonBackground;
                _cachedSkin.button.normal.background = buttonBackground;
                _cachedSkin.button.onNormal.background = buttonBackground;


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
                UnityEngine.Object.DontDestroyOnLoad(buttonBackground);
                UnityEngine.Object.DontDestroyOnLoad(hbuttonBackground);
                UnityEngine.Object.DontDestroyOnLoad(textfield);
                UnityEngine.Object.DontDestroyOnLoad(_cachedSkin);
                // TODO: Add custom skin for Toggle and other items
            }

            return _cachedSkin;
        }

        static string GetTextForMPType(MultiplayerType mptype)
        {
            switch (mptype)
            {
                case MultiplayerType.All:
                    return "All - All Clients will be required to install this mod.";
                case MultiplayerType.Client:
                    return "Client - This mod is client-side, but might have special behavior.";
                case MultiplayerType.Unspecified:
                    return "Unspecified - This mod has not had it's multiplayer operations specified for VoidManager.\n- If the host has VoidManager and this mod, Connection will be allowed.\n- If the host has VoidManager but not this mod, they can optionally trust Unspecified Mods.\n- If the host does not have VoidManager, Connection will be disallowed.\n- If the local client is hosting, vanilla clients will be allowed to join the session.";
                default:
                    return mptype.ToString();
            }
        }

        void DrawModButton(VoidPlugin voidPlugin)
        {
            if (voidPlugin.MPType > MPModChecks.MultiplayerType.Client)
            {
                if (Button($"<color=#FFFF99>{voidPlugin.BepinPlugin.Metadata.Name}</color>"))
                    selectedMod = voidPlugin;
            }
            else
            {
                if (Button(voidPlugin.BepinPlugin.Metadata.Name))
                    selectedMod = voidPlugin;
            }
        }

        Texture2D BuildTexFrom1Color(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        Texture2D BuildTexFromColorArray(Color[] color, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(color);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Iterates through the current Plugin files and searches for gui menus.
        /// </summary>
        public void DiscoverGUIMenus(System.Reflection.Assembly assembly, VoidPlugin voidPlugin)
        {
            mods.Add(voidPlugin);
            var types = assembly.GetTypes();
            // Finds gui menu implementations from all the Assemblies in the same file location.
            var chatCommandInstances = types.Where(t => typeof(ModSettingsMenu).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            bool hasSettingsMenu = false;
            foreach (var modType in chatCommandInstances)
            { // Iterates through each discovered gui menu
                ModSettingsMenu modInstance = (ModSettingsMenu)Activator.CreateInstance(modType);
                settings.Add(modInstance);
                hasSettingsMenu = true;
            }
            if (hasSettingsMenu) BepinPlugin.Log.LogInfo($"[{voidPlugin.BepinPlugin.Metadata.Name}] detected settings menu");
        }

        public void DiscoverNonVManMod(VoidPlugin voidPlugin)
        {
            NonVManMods.Add(voidPlugin);
        }

        bool ShowingCursor;

        void GUIToggleCursor(bool enable)
        {
            if (!BepinPlugin.Bindings.MenuUnlockCursor.Value && !(!enable && ShowingCursor))
            {
                return; // Stop early if unlocking cursor is disabled, but allow passthrough if cursor is enabled and is getting set to disabled.
            }

            ShowingCursor = enable;
            CursorUtility.ShowCursor(this, enable);

            if (ShowingCursor)
            {
                InputActionMapRequests.AddOrChangeRequestAllMaps(this, false);
                InputActionMapRequests.AddOrChangeRequest(this, "GlobalBindings", true);
                InputActionMapRequests.AddOrChangeRequest(this, "Debug", true);
            }
            else
            {
                InputActionMapRequests.RemoveRequestAllMaps(this);
            }
        }
    }
}
