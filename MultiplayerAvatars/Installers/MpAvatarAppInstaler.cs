using MultiplayerAvatars.Avatars;
using MultiplayerAvatars.Providers;
using SiraUtil.Zenject;
using Zenject;

namespace MultiplayerAvatars.Installers
{
    class MpAvatarAppInstaler : Installer
    {
        public override void InstallBindings()
        {
            Plugin.Log?.Info("Injecting Dependencies");
            Container.BindInterfacesAndSelfTo<ModelSaber>().AsSingle();
            Container.BindInterfacesAndSelfTo<CustomAvatarManager>().AsSingle();
        }
    }
}
