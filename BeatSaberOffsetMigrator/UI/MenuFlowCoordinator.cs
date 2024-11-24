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

        private bool _allowDismiss = true;

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
            _advanceViewController.PropertyChanged += OnAdvanceViewControllerPropertiesChanged;
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            _mainViewController.PropertyChanged -= OnMainViewControllerPropertiesChanged;
            _advanceViewController.PropertyChanged -= OnAdvanceViewControllerPropertiesChanged;
        }
        
        private void OnMainViewControllerPropertiesChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_mainViewController.EnableAdvance):
                    if (_mainViewController.EnableAdvance)
                    {
                        SetRightScreenViewController(_advanceViewController, ViewController.AnimationType.In);
                    }
                    else if (_allowDismiss)
                    {
                        SetRightScreenViewController(null, ViewController.AnimationType.Out);
                    }
                    break;
            }
        }
        
        private void OnAdvanceViewControllerPropertiesChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_advanceViewController.ModalShowing):
                    _allowDismiss = !_advanceViewController.ModalShowing;
                    break;
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            if (!_allowDismiss) return;
            base.BackButtonWasPressed(topViewController);
            _mainFlowCoordinator.DismissFlowCoordinator(this);
        }
    }
}