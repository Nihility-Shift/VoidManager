using ExitGames.Client.Photon;
using Gameplay.Chat;
using Photon.Pun;
using Photon.Realtime;
using System.Reflection;
using UI.Chat;
using UnityEngine.UIElements;
using VoidManager.Callbacks;

namespace VoidManager.Utilities
{
    /// <summary>
    /// Provides access to dislaying messages through various means
    /// </summary>
    public class Messaging
    {
        /// <summary>
        /// Inserts a line to text chat with reference to the executing assembly.<br/>
        /// Removes it after timeoutMs milliseconds
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="timeoutMs">Number of milliseconds to display the message before deleting it</param>
        /// <param name="noPrefix">Don't include the mod name at the start of the message</param>
        public static void Notification(string message, long timeoutMs, bool noPrefix = false)
        {
            if (TextChat.Instance == null) return;

            Notification(message, noPrefix);

            TextChatVE chatUI = TextChat.Instance._chatUI;
            ScrollView logView = chatUI.logView;
            VisualElement log = logView.ElementAt(logView.childCount - 1);
            chatUI.schedule.Execute(() => { if (logView.Contains(log)) { logView.Remove(log); } }).ExecuteLater(timeoutMs);
        }

        /// <summary>
        /// Inserts a line to text chat with reference to the executing assembly.
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="noPrefix">Don't include the mod name at the start of the message</param>
        public static void Notification(string message, bool noPrefix = false)
        {
            Assembly assembly = Assembly.GetCallingAssembly();
            TextChat.Instance?.AddLog(new LogEntry($"{(noPrefix ? "" : assembly.FullName.Split(',')[0])}", message));
        }

        /// <summary>
        /// Inserts a line to text chat.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="local"></param>
        public static void Echo(string message, bool local = true)
        {
            if (TextChat.Instance == null) return;
            if (local) TextChat.Instance.AddLog(new LogEntry($"", message));//fixme
            else
            {
                VoipService.Instance.SendTextMessage($"[{MyPluginInfo.USERS_PLUGIN_NAME}]: {message}");
            }
        }

        /// <summary>
        /// Inserts a line to text chat.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messagePrefix">Appears in [] before the message.<br/>E.g. [Void Manager]: message text</param>
        /// <param name="local"></param>
        public static void Echo(string message, string messagePrefix, bool local = true)
        {
            if (TextChat.Instance == null) return;
            if (local) TextChat.Instance.AddLog(new LogEntry($"", message));//fixme
            else
            {
                VoipService.Instance.SendTextMessage($"[{messagePrefix}]: {message}");
            }
        }

        /// <summary>
        /// Sends a message to a given client before kicking. Message should pop up for player after they reappear in the main menu
        /// </summary>
        /// <param name="title">Header</param>
        /// <param name="body">Message body</param>
        /// <param name="player">Target Player</param>
        public static void KickMessage(string title, string body, Player player)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                BepinPlugin.Log.LogInfo($"Sending kick message to {player.NickName}: {title}::{body}");
                PhotonNetwork.RaiseEvent(InRoomCallbacks.KickMessageEventCode, new object[] { title, body }, new RaiseEventOptions { TargetActors = new int[] { player.ActorNumber } }, SendOptions.SendUnreliable);
            }
            else
            {
                BepinPlugin.Log.LogWarning($"Cannot send kick message while not master client.");
            }
        }
    }
}
