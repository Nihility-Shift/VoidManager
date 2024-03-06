﻿using BepInEx;
using System.Diagnostics;
using VoidManager.MPModChecks;

namespace VoidManager
{
    /// <summary>
    /// Used by V-Man to signify a mod. Must have a unique harmonyID
    /// </summary>
    public abstract class VoidPlugin
    {
        internal FileVersionInfo VersionInfo;

        /// <summary>
        /// Automatically assigned by VoidManager
        /// </summary>
        internal PluginInfo BepinPlugin;

        /// <summary>
        /// Automatically assigned by VoidManager
        /// </summary>
        public PluginInfo MyBepinPlugin
        {
            get => BepinPlugin;
        }

        /// <summary>
        /// Version of mod.
        /// </summary>
        public virtual string Version
        {
            get
            {
                return VersionInfo?.FileVersion;
            }
        }

        /// <summary>
        /// Author(s) of mod.
        /// </summary>
        public virtual string Author
        {
            get
            {
                return VersionInfo?.CompanyName;
            }
        }

        /// <summary>
        /// description of mod. Ideal for in-game readme or patch notes.
        /// </summary>
        public virtual string Description
        {
            get
            {
                return VersionInfo?.FileDescription;
            }
        }

        /// <summary>
        /// Name of mod.
        /// </summary>
        public virtual string Name
        {
            get
            {
                return MyBepinPlugin.Metadata.Name;
            }
        }

        /// <summary>
        /// Mod's multiplayer requirements. Use MPModChecks.MultiplayerType.<br/>
        /// Client: No requirement<br/>
        /// All: All players must have the mod installed
        /// Hidden: Hidden from mod lists<br/>
        /// </summary>
        public virtual MultiplayerType MPType
        {
            get
            {
                return MultiplayerType.Client;
            }
        }
    }
}
