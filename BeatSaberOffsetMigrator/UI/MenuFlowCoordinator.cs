using System;
using System.ComponentModel;
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
                    ProvideInitialViewControllers(_mainViewController);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            _mainViewController.PropertyChanged += OnMainViewControllerPropertiesChanged;
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            _mainViewController.PropertyChanged -= OnMainViewControllerPropertiesChanged;
        }
        
        private void OnMainViewControllerPropertiesChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_mainViewController.EnableAdvance))
            {
                if (_mainViewController.EnableAdvance)
                {
                    SetRightScreenViewController(_advanceViewController, ViewController.AnimationType.In);
                }
                else
                {
                    SetRightScreenViewController(null, ViewController.AnimationType.Out);
                }
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