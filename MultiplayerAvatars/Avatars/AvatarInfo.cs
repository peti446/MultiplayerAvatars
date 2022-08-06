using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace MultiplayerAvatars.Avatars
{
    public class AvatarInfo
    {
        public string[]? tags;
        public string? type;
        public string? name;
        public string? author;
        public string? image;
        public string? hash;
        public string? bsaber;
        public string? download;
        public string? install_link;
        public string? date;

        [JsonIgnore] public SiraLog Logger = null!;
        [JsonIgnore] public HttpClient HttpClient = null!;

        public async Task<string?> DownloadAvatar(CancellationToken cancellationToken)
        {
            if (Uri.TryCreate(download, UriKind.Absolute, out Uri uri))
            {
                string? customAvatarPath = null;
                try
                {
                    HttpResponseMessage? response =
                        await HttpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    string avatarDirectory = Path.Combine(IPA.Utilities.UnityGame.InstallPath, "CustomAvatars");
                    Directory.CreateDirectory(avatarDirectory);
                    // TODO: May be better to download to temp directory first?
                    if (string.IsNullOrWhiteSpace(name))
                        name = "Unknown";
                    customAvatarPath = Path.Combine(avatarDirectory, $"{name}.avatar");
                    int index = 2;
                    while (File.Exists(customAvatarPath))
                    {
                        customAvatarPath = Path.Combine(avatarDirectory, $"{name}_{index++}.avatar");
                    }

                    using (var fs = File.Create(customAvatarPath))
                    {
                        await response.Content.CopyToAsync(fs).ConfigureAwait(false);
                    }

                    return customAvatarPath;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error downloading avatar from '{uri}': {ex.Message}");
                    Logger.Debug(ex);
                    if (customAvatarPath != null && File.Exists(customAvatarPath))
                    {
                        try
                        {
                            File.Delete(customAvatarPath);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(
                                $"Error trying to delete incomplete download at '{customAvatarPath}': {e.Message}");
                        }
                    }
                }
            }

            return null;
        }

        public IEnumerator DownloadAvatar(Action<string> callback)
        {
            UnityWebRequest www = UnityWebRequest.Get(download);

            if (HttpClient.DefaultRequestHeaders.TryGetValues("User-Agent", out IEnumerable<string> value))
            {
                www.SetRequestHeader("User-Agent", value.FirstOrDefault());
            }
            else
            {
                www.SetRequestHeader("User-Agent", "MultiplayerInfo BeatSaber");
            }

            www.timeout = 0;

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Unable to download avatar! {www.error}");
                yield break;
            }

            Logger.Debug("Received response from ModelSaber...");
            string docPath = "";
            string customAvatarPath = "";

            byte[] data = www.downloadHandler.data;

            try
            {
                docPath = Application.dataPath;
                docPath = docPath.Substring(0, docPath.Length - 5);
                docPath = docPath.Substring(0, docPath.LastIndexOf("/"));
                customAvatarPath = docPath + "/CustomAvatars/" + name + ".avatar";
                customAvatarPath = Path.Combine(IPA.Utilities.UnityGame.InstallPath, "CustomAvatars", $"{name}.avatar");

                Logger.Debug($"Saving avatar to \"{customAvatarPath}\"...");

                File.WriteAllBytes(customAvatarPath, data);
                Logger.Debug("Downloaded avatar!");

                _ = name ?? throw new InvalidOperationException("Unable to download avatar. No name found.");

                callback(name);
            }
            catch (Exception e)
            {
                Logger.Critical(e);
                yield break;
            }
        }
    }
}