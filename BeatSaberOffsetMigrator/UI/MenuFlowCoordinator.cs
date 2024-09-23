using System;
using BeatSaberMarkupLanguage;
using HMUI;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaberOffsetMigrator.UI
{
    internal class MenuFlowCoordinator : FlowCoordinator
    {
        [Inject]
        private SiraLog _logger = null!;
        
        [Inject]
        private MainFlowCoordinator _mainFlowCoordinator = null!;

        [Inject]
        private MenuViewController _viewController = null!;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            try
            {
                if (firstActivation)
                {
                    SetTitle("Offset Helper");
                    showBackButton = true;
                    ProvideInitialViewControllers(_viewController);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            base.BackButtonWasPressed(topViewController);
            _mainFlowCoordinator.DismissFlowCoordinator(this);
        }
    }
}