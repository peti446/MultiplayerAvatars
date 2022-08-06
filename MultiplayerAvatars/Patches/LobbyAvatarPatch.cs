using HarmonyLib;
using MultiplayerAvatars.Avatars;
using System;

namespace MultiplayerAvatars.Patches
{
    [HarmonyPatch(typeof(MultiplayerLobbyInstaller), nameof(MultiplayerLobbyInstaller.InstallBindings),
        MethodType.Normal)]
    internal class LobbyAvatarPatch
    {
        internal static void Prefix(ref MultiplayerLobbyAvatarController ____multiplayerLobbyAvatarControllerPrefab)
        {
            if (!____multiplayerLobbyAvatarControllerPrefab.gameObject.TryGetComponent<CustomAvatarController>(out _))
            {
                ____multiplayerLobbyAvatarControllerPrefab.gameObject.AddComponent<CustomAvatarController>();
            }
        }
    }
}