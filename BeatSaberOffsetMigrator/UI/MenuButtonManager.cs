using System;
using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaberMarkupLanguage;
using BGLib.Polyglot;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaberOffsetMigrator.UI
{
    public class MenuButtonManager : IInitializable
    {
        private readonly MenuButton _menuButton;
        
        [Inject]
        private readonly MainFlowCoordinator _mainFlowCoordinator = null!;
        
        [Inject]
        private readonly MenuFlowCoordinator _menuFlowCoordinator = null!;
        
        [Inject]
        private readonly MenuButtons _menuButtons = null!;
        
        [Inject]
        private readonly SiraLog _logger = null!;

        
        public MenuButtonManager()
        {
            _menuButton = new MenuButton(Localization.Get("BSOM_MENU_TITLE"), Localization.Get("BSOM_MENU_BUTTON_HINT"), OnMenuButtonClick);
        }

        public void Initialize()
        {
            _menuButtons.RegisterButton(_menuButton);
        }
        
        private void OnMenuButtonClick()
        {
            _mainFlowCoordinator.PresentFlowCoordinator(_menuFlowCoordinator);
        }
    }
}