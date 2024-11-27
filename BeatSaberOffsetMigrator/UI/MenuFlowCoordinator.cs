using System;
using System.ComponentModel;
using BeatSaberMarkupLanguage;
using BGLib.Polyglot;
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
        
        [Inject]
        private DocumentationViewController _documentationViewController = null!;

        private bool _allowDismiss = true;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            try
            {
                if (firstActivation)
                {
                    SetTitle(Localization.Get("BSOM_MENU_TITLE"));
                    showBackButton = true;
                    ProvideInitialViewControllers(_mainViewController, leftScreenViewController: _documentationViewController);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return;
            }

            _mainViewController.PropertyChanged += OnMainViewControllerPropertiesChanged;
            _advanceViewController.PropertyChanged += OnAdvanceViewControllerPropertiesChanged;
            RefreshAdvanceViewControllerState();
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
                    RefreshAdvanceViewControllerState();
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

        private void RefreshAdvanceViewControllerState()
        {
            if (_mainViewController.EnableAdvance)
            {
                SetRightScreenViewController(_advanceViewController, ViewController.AnimationType.In);
            }
            else if (_allowDismiss)
            {
                SetRightScreenViewController(null, ViewController.AnimationType.Out);
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