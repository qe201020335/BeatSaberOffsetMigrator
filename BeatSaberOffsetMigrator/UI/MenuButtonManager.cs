﻿using System;
using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaberMarkupLanguage;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaberOffsetMigrator.UI
{
    public class MenuButtonManager : IInitializable, IDisposable
    {
        private readonly MenuButton _menuButton;
        
        [Inject]
        private readonly MainFlowCoordinator _mainFlowCoordinator = null!;
        
        [Inject]
        private readonly MenuFlowCoordinator _menuFlowCoordinator = null!;
        
        // [Inject]
        // private readonly MenuButtons _menuButtons = null!;
        
        [Inject]
        private readonly SiraLog _logger = null!;

        
        public MenuButtonManager()
        {
            _menuButton = new MenuButton("Offset Helper", "Migrate controller offsets", OnMenuButtonClick);
        }

        public void Initialize()
        {
            MenuButtons.instance.RegisterButton(_menuButton);
        }

        public void Dispose()
        {
            MenuButtons.instance.UnregisterButton(_menuButton);
        }
        
        private void OnMenuButtonClick()
        {
            _mainFlowCoordinator.PresentFlowCoordinator(_menuFlowCoordinator);
        }
    }
}