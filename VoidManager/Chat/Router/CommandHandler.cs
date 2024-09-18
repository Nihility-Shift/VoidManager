﻿using System;
using System.Collections.Generic;
using System.Linq;
using VoidManager.MPModChecks;

namespace VoidManager.Chat.Router
{
    internal class CommandHandler
    {
        private static Dictionary<string, ChatCommand> chatCommands = new Dictionary<string, ChatCommand>();
        private static Dictionary<string, PublicCommand> publicCommands = new Dictionary<string, PublicCommand>();
        public static int chatCommandCount => chatCommands.Count;
        public static int publicCommandCount => publicCommands.Count;

        /// <summary>
        /// Executes chat command if found from alias with arguments.
        /// </summary>
        /// <param name="alias">Potential chat command alias</param>
        /// <param name="arguments">Arguments to use with command</param>
        /// <param name="publicCommand">Should Execute Public Commands</param>
        /// <param name="playerId">PlayerID of message sender (public commands)</param>
        internal static void ExecuteCommandFromAlias(string alias, string arguments, bool publicCommand = false, int playerId = -1)
        {
            alias = alias.ToLower();
            try
            {
                if (publicCommand) { if (Photon.Pun.PhotonNetwork.IsMasterClient && publicCommands.ContainsKey(alias) && MPModCheckManager.IsMod_Session()) publicCommands[alias].Execute(arguments, playerId); }
                else if (chatCommands.ContainsKey(alias)) chatCommands[alias].Execute(arguments);
                else BepinPlugin.Log.LogInfo($"'{(publicCommand ? "!" : "/")}{alias} {arguments}' cound not be found!");
            }
            catch (Exception ex)
            {
                BepinPlugin.Log.LogError($"'{(publicCommand ? "!" : "/")}{alias} {arguments}' failed! \nCommand Exception: {ex.Message}!\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Gets chat command from alias.
        /// </summary>
        /// <param name="alias"></param>
        /// <returns>ChatCommand reference from alias</returns>
        public static ChatCommand GetCommand(string alias)
        {
            if (chatCommands.ContainsKey(alias)) return chatCommands[alias];
            else return null;
        }

        /// <summary>
        /// Gets ordered list of chat commands.
        /// </summary>
        /// <returns>Ordered list of chat commands</returns>
        public static IOrderedEnumerable<ChatCommand> GetCommands()
        {
            return new HashSet<ChatCommand>(chatCommands.Values).OrderBy(t => t.CommandAliases()[0]);
        }

        /// <summary>
        /// Gets public command from alias.
        /// </summary>
        /// <param name="alias"></param>
        /// <returns>PublicCommand reference from alias</returns>
        public static PublicCommand GetPublicCommand(string alias)
        {
            if (publicCommands.ContainsKey(alias)) return publicCommands[alias];
            else return null;
        }

        /// <summary>
        /// Gets ordered list of public chat commands.
        /// </summary>
        /// <returns>Ordered list of public chat commands</returns>
        public static IOrderedEnumerable<PublicCommand> GetPublicCommands()
        {
            return new HashSet<PublicCommand>(publicCommands.Values).OrderBy(t => t.CommandAliases()[0]);
        }

        /// <summary>
        /// Iterates through the current Plugin files and searches for commands.
        /// </summary>
        public static void DiscoverCommands(System.Reflection.Assembly assembly, string ModName = "")
        {
            Type[] types = assembly.GetTypes();
            // Finds ChatCommand implementations from all the Assemblies in the same file location.
            IEnumerable<Type> chatCommandInstances = types.Where(t => typeof(ChatCommand).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            int commandCount = 0;
            foreach (Type modType in chatCommandInstances)
            { // Iterates through each discovered ChatCommand
                ChatCommand modInstance = (ChatCommand)Activator.CreateInstance(modType);
                foreach (string commandAlias in Array.ConvertAll(modInstance.CommandAliases(), d => d.ToLower()))
                {
                    if (chatCommands.ContainsKey(commandAlias))
                    {
                        BepinPlugin.Log.LogInfo($"[{ModName}] Found duplicate command alias {commandAlias}");
                        continue;
                    }
                    else
                    {
                        chatCommands.Add(commandAlias, modInstance);
                        commandCount++;
                    }
                }
            }
            if (commandCount != 0)
            {
                BepinPlugin.Log.LogInfo($"[{ModName}] Detected {commandCount} chat commands");
            }
        }
        /// <summary>
        /// Iterates through the current Plugin files and searches for public commands.
        /// </summary>
        public static void DiscoverPublicCommands(System.Reflection.Assembly assembly, string ModName = "")
        {
            Type[] types = assembly.GetTypes();
            // Finds PublicCommand implementations from all the Assemblies in the same file location.
            IEnumerable<Type> publicCommandInstances = types.Where(t => typeof(PublicCommand).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            int commandCount = 0;
            foreach (Type modType in publicCommandInstances)
            { // Iterates through each discovered PublicCommand
                PublicCommand modInstance = (PublicCommand)Activator.CreateInstance(modType);
                foreach (string commandAlias in Array.ConvertAll(modInstance.CommandAliases(), d => d.ToLower()))
                {
                    if (publicCommands.ContainsKey(commandAlias))
                    {
                        BepinPlugin.Log.LogInfo($"[{ModName}] Found duplicate public command alias {commandAlias}");
                        continue;
                    }
                    else
                    {
                        publicCommands.Add(commandAlias, modInstance);
                        commandCount++;
                    }
                }
            }
            if (commandCount != 0)
            {
                BepinPlugin.Log.LogInfo($"[{ModName}] Detected {commandCount} public commands");
            }
        }
    }
}
