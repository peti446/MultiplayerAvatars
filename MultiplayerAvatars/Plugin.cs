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
        
        internal static IPALogger Log { get; private set; } = null!;
        internal static HttpClient HttpClient { get; private set; } = null!;
        
        private readonly Harmony _harmony;
        private static PluginMetadata _pluginMetadata = null!;

        public static string UserAgent
		{
            get
			{
                var modVersion = _pluginMetadata.HVersion.ToString();
                var bsVersion = IPA.Utilities.UnityGame.GameVersion.ToString();
                return $"MultiplayerAvatars/{modVersion} (BeatSaber/{bsVersion})";
			}
		}

        [Init]
        public Plugin(IPALogger logger, Zenjector zenjector, PluginMetadata pluginMetadata)
        {
            _harmony = new Harmony(ID);
            _pluginMetadata = pluginMetadata;
            Log = logger;

            zenjector.UseMetadataBinder<Plugin>();
            zenjector.UseLogger(logger);
            zenjector.UseHttpService();
            zenjector.UseSiraSync(SiraUtil.Web.SiraSync.SiraSyncServiceType.GitHub, "Goobwabber", "MultiplayerCore");
            zenjector.Install<MpAvatarAppInstaler>(Location.App);

            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Add("User-Agent", Plugin.UserAgent);
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
