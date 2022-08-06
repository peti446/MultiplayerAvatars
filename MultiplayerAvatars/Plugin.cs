using HarmonyLib;
using IPA;
using IPA.Loader;
using MultiplayerAvatars.Installers;
using SiraUtil.Zenject;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using IPALogger = IPA.Logging.Logger;

namespace MultiplayerAvatars
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        public static readonly string ID = "com.github.Goobwabber.MultiplayerAvatars";
        
        private readonly Harmony _harmony;
        private static PluginMetadata _pluginMetadata = null!;

        [Init]
        public Plugin(IPALogger logger, Zenjector zenjector, PluginMetadata pluginMetadata)
        {
            _harmony = new Harmony(ID);
            _pluginMetadata = pluginMetadata;
            var client = new HttpClient();
            var modVersion = _pluginMetadata.HVersion.ToString();
            var bsVersion = IPA.Utilities.UnityGame.GameVersion.ToString();
            string userAgent = $"MultiplayerAvatars/{modVersion} (BeatSaber/{bsVersion})";
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);

            zenjector.UseMetadataBinder<Plugin>();
            zenjector.UseLogger(logger);
            zenjector.UseHttpService();
            zenjector.UseSiraSync(SiraUtil.Web.SiraSync.SiraSyncServiceType.GitHub, "Goobwabber", "MultiplayerAvatar");
            zenjector.Install<MpAvatarAppInstaller>(Location.App, client);
        }

        [OnEnable]
        public void OnEnable()
        {
            _harmony.PatchAll(_pluginMetadata.Assembly);
        }

        [OnDisable]
        public void OnDisable()
        {
            _harmony.UnpatchSelf();
        }
    }
}
