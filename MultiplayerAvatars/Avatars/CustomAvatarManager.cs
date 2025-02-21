﻿using System;
using Zenject;
using CustomAvatar.Avatar;
using CustomAvatar.Player;
using System.Collections.Generic;
using MultiplayerCore.Players;
using MultiplayerAvatars.Providers;
using MultiplayerCore.Networking;
using SiraUtil.Logging;

namespace MultiplayerAvatars.Avatars
{
    internal class CustomAvatarManager : IInitializable
    {
        private readonly MpPacketSerializer _packetSerializer;
        private readonly PlayerAvatarManager _avatarManager;
        private readonly IMultiplayerSessionManager _sessionManager;
        private readonly IAvatarProvider<AvatarPrefab> _avatarProvider;


        public CustomAvatarData localAvatar = new CustomAvatarData();
        public bool hashesCalculated = false;
        public Action<IConnectedPlayer, CustomAvatarData>? avatarReceived;
        private readonly Dictionary<string, CustomAvatarData> _avatars = new Dictionary<string, CustomAvatarData>();
        private readonly SiraLog _logger;

        internal CustomAvatarManager(MpPacketSerializer packetSerializer, PlayerAvatarManager avatarManager, IMultiplayerSessionManager sessionManager, IAvatarProvider<AvatarPrefab> avatarProvider, SiraLog logger)
        {
            _packetSerializer = packetSerializer;
            _avatarManager = avatarManager;
            _sessionManager = sessionManager;
            _avatarProvider = avatarProvider;
            _logger = logger;
        }

        public void Initialize()
        {
            _logger.Info("Setting up CustomAvatarManager");
            _avatarProvider.hashesCalculated += (x, y) => hashesCalculated = true;
            _avatarManager.avatarScaleChanged += scale => localAvatar.scale = scale;
            _avatarManager.avatarChanged += OnAvatarChanged;

            _sessionManager.playerConnectedEvent += OnPlayerConnected;
            _packetSerializer.RegisterCallback<CustomAvatarPacket>(HandleAvatarPacket);

            OnAvatarChanged(_avatarManager.currentlySpawnedAvatar);
        }

        public CustomAvatarData? GetAvatarByUserId(string userId)
        {
            if (_avatars.ContainsKey(userId))
                return _avatars[userId];
            return null;
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            if (!avatar) return;
            if (!hashesCalculated)
            {
                _avatarProvider.hashesCalculated += (x, y) => OnAvatarChanged(avatar);
                return;
            }

            _logger.Warn($"Attempting to hash {avatar.prefab.fullPath}");
            _avatarProvider.HashAvatar(avatar.prefab).ContinueWith(r =>
            {
                localAvatar.hash = r.Result;
                localAvatar.scale = avatar.scale;
            });
        }

        private void OnPlayerConnected(IConnectedPlayer player)
        {
            CustomAvatarPacket localAvatarPacket = localAvatar.GetPacket();
            _sessionManager.Send(localAvatarPacket);
        }

        private void HandleAvatarPacket(CustomAvatarPacket packet, IConnectedPlayer player)
        {
            _logger.Info($"Received 'CustomAvatarPacket' from '{player.userId}' with '{packet.hash}'");
            _avatars[player.userId] = new CustomAvatarData(packet);
            avatarReceived?.Invoke(player, _avatars[player.userId]);
        }
    }
}
