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
        private AdvanceViewController _advanceViewController = null!;
        
        [Inject]
        private MainViewController _mainViewController = null!;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            try
            {
                if (firstActivation)
                {
                    SetTitle("Offset Helper");
                    showBackButton = true;
                    ProvideInitialViewControllers(_mainViewController, rightScreenViewController: _advanceViewController);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            if (!_advanceViewController.AllowClose) return;
            base.BackButtonWasPressed(topViewController);
            _mainFlowCoordinator.DismissFlowCoordinator(this);
        }
    }
}