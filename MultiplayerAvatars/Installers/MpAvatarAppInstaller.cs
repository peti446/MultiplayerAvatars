using System.Net.Http;
using MultiplayerAvatars.Avatars;
using MultiplayerAvatars.Providers;
using SiraUtil.Zenject;
using Zenject;

namespace MultiplayerAvatars.Installers
{
    class MpAvatarAppInstaller : Installer
    {
        private readonly HttpClient _client;

        internal MpAvatarAppInstaller(HttpClient client)
        {
            _client = client;
        }

        public override void InstallBindings()
        {
            Container.BindInstance(new UBinder<Plugin, HttpClient>(_client)).AsSingle();
            Container.BindInterfacesAndSelfTo<ModelSaber>().AsSingle();
            Container.BindInterfacesAndSelfTo<CustomAvatarManager>().AsSingle();
        }
    }
}
