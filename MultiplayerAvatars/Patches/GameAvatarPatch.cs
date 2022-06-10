using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using IPA.Utilities;
using MultiplayerAvatars.Avatars;

namespace MultiplayerAvatars.Patches
{
    [HarmonyPatch(typeof(MultiplayerPlayersManager), nameof(MultiplayerPlayersManager.SpawnPlayers), MethodType.Normal)]
    internal class GameAvatarPatch
    {
        internal static void Prefix(MultiplayerPlayersManager __instance)
        {
            MultiplayerAvatarPoseController avatarPoseController = __instance.GetField<MultiplayerConnectedPlayerFacade, MultiplayerPlayersManager>("_connectedPlayerControllerPrefab").GetComponentInChildren<MultiplayerAvatarPoseController>();
            MultiplayerAvatarPoseController avatarDuelPoseController = __instance.GetField<MultiplayerConnectedPlayerFacade, MultiplayerPlayersManager>("_connectedPlayerDuelControllerPrefab").GetComponentInChildren<MultiplayerAvatarPoseController>();
            if (avatarPoseController.gameObject.GetComponents<CustomAvatarController>().Length == 0)
            {
                avatarPoseController.gameObject.AddComponent<CustomAvatarController>();
            }

            if (avatarDuelPoseController.gameObject.GetComponents<CustomAvatarController>().Length == 0)
            {
                avatarDuelPoseController.gameObject.AddComponent<CustomAvatarController>();
            }
        }
    }
}