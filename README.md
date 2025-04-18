[![](https://img.shields.io/badge/-Void_Crew_Modding_Team-111111?style=just-the-label&logo=github&labelColor=24292f)](https://github.com/Void-Crew-Modding-Team)
[![](https://img.shields.io/github/v/release/Void-Crew-Modding-Team/VoidManager?include_prereleases&style=flat&label=Release%20Version&labelColor=24292f&color=111111)](https://github.com/Void-Crew-Modding-Team/VoidManager/releases/)
![](https://img.shields.io/badge/Game%20Version-1.1.0-111111?style=flat&labelColor=24292f&color=111111)
[![](https://img.shields.io/github/license/Void-Crew-Modding-Team/VoidManager?style=flat&label=License&labelColor=24292f&color=111111)](https://github.com/Void-Crew-Modding-Team/VoidManager/blob/master/LICENSE)
[![](https://img.shields.io/discord/1180651062550593536.svg?&logo=discord&logoColor=ffffff&style=flat&label=Discord&labelColor=24292f&color=111111)](https://discord.gg/g2u5wpbMGu "Void Crew Modding Discord")

# Void Manager

`BepInEx` Plugin Manager for `Void Crew`

Version 1.2.8  
For Game Version 1.1.0  
Developed by Mest, Dragon, and 18107  
Based on [Pulsar Mod Loader](https://github.com/PULSAR-Modders/pulsar-mod-loader)


---------------------

### 💡 Functions - **Various features to assist in mod management.**
- Handling for mods requiring installation by all users
- Manual configuration of mods not configured for Void Manager
- Viewing of other players' mod lists
- Viewing of room mod lists from MatchMaking Join Panels
- Player list from MatchMaking Join Panels.
- '[Mods Required]' added to applicable sessions names. VoidManager clients view as green, yellow, and red '[M]' based on session modding compatability.
- Mod settings GUI 'Void Manager F5 Menu'
- Hides Chainloader object for developers
- Unlocks mouse while using text chat
- Chat input history
- Command Auto-complete via tab key-press
- Disables Quick Join
- Escalation to `Mod_Session` option for a Void Manager host
- Configurable PUN Logging

## ⌨ API
- Mod MPType Specification
- OnSessionChanged callback with gamestate parameter inputs for enabling/disabling Mod_session features
- Progression disable call
- local and public chat commands
- Networked Mod to mod messages
- Detection of mods installed on other clients
- Networking events
- Recipe and unlock modifications
- Mod settings GUI
- Player Settings GUI
- User Notifications
- Harmony Transpiler Patching Tools
- Various Utilities

### 🎮 Client Usage

- `F5` ingame will bring up a menu which lists all installed `Void Manager` plugins and their mod settings.
- `/` is the prefix for client commands. `/help` lists all commands available.
- `!` is the prefix for public commands. `!help` lists all public commands available.

### 👥 Multiplayer Functionality 

**Complex** - Void Manager allows/disallows connection to rooms based on mod MPType configuration.

#### MPTypes

- **All** - Requires all clients to install the mod.
- **Session** - Requires the session to be marked as `Mod_Session`
- **Host** - - General MPType for a host-side mod, allowed to join vanilla sessions. Mods utilizing this MPType should disable `Mod_Session` features when applicable.
- **Client** - - Client Side, allowed to join vanilla sessions. Mods utilizing this MPType should disable `Mod_Session` features when applicable.
- **Unmanaged** - A mod loaded alongside but not configured for Void Manager. May be manually configured as above MPTypes.

---------------------

## 🔧 Install Instructions - **Install following the normal BepInEx procedure.**

Ensure that you have [BepInEx 5](https://thunderstore.io/c/void-crew/p/BepInEx/BepInExPack/) (stable version 5 **MONO**) and [VoidManager](https://thunderstore.io/c/void-crew/p/VoidCrewModdingTeam/VoidManager/) installed.

#### ✔️ Mod installation - **Unzip the contents into the BepInEx plugin directory**

Drag and drop `VoidManager.dll` into `Void Crew\BepInEx\plugins`

---------------------

### 🤔 Development Guide - **Documentation to create Void Manager mods is** [on the wiki](https://github.com/Void-Crew-Modding-Team/VoidManager/wiki)

---------------------

### Future Plans:

- API for permission requests from the session host, so that a mod may enable special features in a `Mod_Session`
- Mod whitelist/blacklisting
- Better detection of local od_Session mods.
