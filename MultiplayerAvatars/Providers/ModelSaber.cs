using CustomAvatar.Avatar;
using CustomAvatar.Player;
using MultiplayerAvatars.Avatars;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SiraUtil.Logging;
using SiraUtil.Zenject;
using Zenject;

namespace MultiplayerAvatars.Providers
{
    public class ModelSaber : IAvatarProvider<AvatarPrefab>, IInitializable
    {
#pragma warning disable CS0649
        [Inject]
        private AvatarLoader? _avatarLoader;
#pragma warning restore CS0649

#pragma warning disable CS0067
        public event EventHandler<AvatarDownloadedEventArgs>? avatarDownloaded;
#pragma warning restore CS0067
        public event EventHandler? hashesCalculated;
        public Type AvatarType => typeof(AvatarPrefab);
        public bool isCalculatingHashes { get; protected set; }
        public int cachedAvatarsCount => cachedAvatars.Count;
        public string AvatarDirectory => PlayerAvatarManager.kCustomAvatarsPath;

        private readonly Dictionary<string, AvatarPrefab> cachedAvatars = new Dictionary<string, AvatarPrefab>();
        private readonly SiraLog _logger;
        private readonly HttpClient _httpClient;

        internal ModelSaber(SiraLog logger, UBinder<MultiplayerAvatars.Plugin, HttpClient> client)
        {
            _logger = logger;
            _httpClient = client.Value;
        }

        public void Initialize()
        {
            _ = InitializeTask();
        }


        public bool CacheAvatar(string avatarPath)
        {
            return false;
        }

        public bool TryGetCachedAvatar(string hash, out AvatarPrefab? avatar)
        {
            return cachedAvatars.TryGetValue(hash, out avatar);
        }

        public async Task<AvatarPrefab?> FetchAvatarByHash(string hash, CancellationToken cancellationToken)
        {
            if (cachedAvatars.TryGetValue(hash, out AvatarPrefab cachedAvatar))
                return cachedAvatar;
            var avatarInfo = await FetchAvatarInfoByHash(hash, cancellationToken);
            if (avatarInfo == null)
            {
                _logger.Info($"Couldn't find avatar with hash '{hash}'");
                return null;
            }
            var path = await avatarInfo.DownloadAvatar(cancellationToken);
            if (path != null)
                return await LoadAvatar(path);
            else
                return null;
        }

        public async Task<AvatarInfo?> FetchAvatarInfoByHash(string hash, CancellationToken cancellationToken)
        {
            AvatarInfo? avatarInfo = null;
            try
            {
                Uri uri = new Uri($"https://modelsaber.com/api/v2/get.php?type=avatar&filter=hash:{hash}");
                var response = await _httpClient.GetAsync(uri, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.Debug("Received response from ModelSaber...");
                    string content = await response.Content.ReadAsStringAsync();
                    Dictionary<string, AvatarInfo> avatars = JsonConvert.DeserializeObject<Dictionary<string, AvatarInfo>>(content);
                    if (!avatars.Any())
                    {
                        return null;
                    }
                    avatarInfo = avatars.First().Value;
                    avatarInfo.Logger = _logger;
                    avatarInfo.HttpClient = _httpClient;
                }
                else
                {
                    _logger.Warn($"Unable to retrieve avatar info from ModelSaber: {response.StatusCode}|{response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error retrieving avatar info for '{hash}': {ex.Message}");
                _logger.Debug(ex);
            }

            return avatarInfo;
        }

        public Task<string> HashAvatar(AvatarPrefab avatar)
        {
            var path = avatar?.fullPath ?? throw new ArgumentNullException(nameof(avatar));
            string fullPath = Path.Combine(Path.GetFullPath("CustomAvatars"), path);
            if (!File.Exists(fullPath))
                throw new ArgumentException($"File at {fullPath} does not exist.");
            return Task.Run(() =>
            {
                string hash = null!;
                _logger.Debug($"Hashing avatar path {path}");
                using (var fs = File.OpenRead(fullPath))
                {
                    hash = BitConverter.ToString(MD5.Create().ComputeHash(fs)).Replace("-", "");
                    if (!cachedAvatars.ContainsKey(hash))
                        cachedAvatars.Add(hash, avatar);
                    return hash;
                }
            });
        }

        public async Task HashAllAvatars(string directory)
        {
            //var avatarFiles = Directory.GetFiles(PlayerAvatarManager.kCustomAvatarsPath, "*.avatar");
            var avatarFiles = Directory.GetFiles(AvatarDirectory, "*.avatar");
            _logger.Debug($"Hashing avatars... {avatarFiles.Length} possible avatars found");
            cachedAvatars.Clear();
            foreach (string avatarFile in avatarFiles)
            {
                await LoadAvatar(avatarFile);
            }
            isCalculatingHashes = false;
            _logger.Debug($"{cachedAvatarsCount} avatars hashed and loaded!");
            HMMainThreadDispatcher.instance.Enqueue(() =>
            {
                hashesCalculated?.Invoke(this, EventArgs.Empty);
            });
        }

        public async Task<AvatarPrefab?> LoadAvatar(string avatarFile)
        {
            try
            {
                _ = _avatarLoader ?? throw new InvalidOperationException("Avatar loader not set.");
                var cancellationTokenSource = new CancellationTokenSource();

                var task = _avatarLoader.LoadFromFileAsync(avatarFile, (IProgress<float>)null!, cancellationTokenSource.Token);

                if (await Task.WhenAny(task, Task.Delay(10000, cancellationTokenSource.Token)) != task)
                {
                    _logger.Warn($"Timeout exceeded trying to load avatar '{avatarFile}'");
                    cancellationTokenSource.Cancel();
                    return null;
                }

                // Re-awit so that any exception or cancellation is rethrown
                var avatar = await task;
                if (avatar == null)
                {
                    _logger.Warn($"Couldn't load avatar at '{avatarFile}'");
                    return null;
                }
                try
                {
                    string calculatedHash = await HashAvatar(avatar);
                    _logger.Debug($"Hashed avatar \"{avatar.descriptor.name}\"! Hash: {calculatedHash}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Unable to hash avatar \"{avatar.descriptor.name}\"! Exception: {ex}");
                    _logger.Debug(ex);
                }
                return avatar;
            }
            catch (Exception ex)
            {
                _logger.Error($"Unable to load avatar!");
                _logger.Debug(ex);
            }
            return null;
        }

        private async Task InitializeTask()
        {
            try
            {
                await HashAllAvatars(AvatarDirectory);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }
}
