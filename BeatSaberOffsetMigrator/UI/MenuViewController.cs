using System;
using System.Collections;
using System.ComponentModel;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.InputHelper;
using SiraUtil.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BeatSaberOffsetMigrator.UI
{
    [ViewDefinition("BeatSaberOffsetMigrator.UI.BSML.MenuView.bsml")]
    [HotReload(RelativePathToLayout = @"BSML\MenuView.bsml")]
    internal class MenuViewController : BSMLAutomaticViewController
    {
        internal enum ExportState
        {
            Idle,
            CannotExport,
            Exporting,
            ExportedOrFailed
        }
        
        [Inject]
        private readonly SiraLog _logger = null!;
        
        [Inject]
        private readonly PluginConfig _config = null!;
        
        [Inject]
        private readonly OffsetHelper _offsetHelper = null!;
        
        [Inject]
        private readonly IVRInputHelper _vrInputHelper = null!;
        
        private bool _parsed = false;
        private bool _modalShowing = false;     
        private ExportState _curExportState = ExportState.Idle;
        
        private ExportState CurExportState
        {
            get => _curExportState;
            set
            {
                _curExportState = value;
                switch (value)
                {
                    case ExportState.Idle:
                        _exportButton.interactable = true;
                        _closeModalButton.interactable = true;
                        break;
                    case ExportState.CannotExport:
                        _exportButton.interactable = false;
                        _closeModalButton.interactable = true;
                        break;
                    case ExportState.Exporting:
                        _exportButton.interactable = false;
                        _closeModalButton.interactable = true;
                        break;
                    case ExportState.ExportedOrFailed:
                        _exportButton.interactable = false;
                        _closeModalButton.interactable = true;
                        break;
                }
            }
        }
        
        public bool AllowClose => !_modalShowing;
        
        [UIParams]
        private BSMLParserParams parserParams = null!;

        [UIValue("ApplyOffset")]
        private bool ApplyOffset
        {
            get => _config.ApplyOffset;
            set
            {
                _config.ApplyOffset = value;
                RefreshState();
            }
        }

        [UIValue("supported")]
        private bool OffsetSupported => _vrInputHelper.Supported;
        
        private string _exportModalText = "Export to EasyOffset";
        [UIValue("export_model_text")]
        public string ExportModalText
        {
            get => _exportModalText;
            private set
            {
                _exportModalText = value;
                NotifyPropertyChanged();
            }
        }
        
        private string _exportButtonText = "Export";
        [UIValue("export_button_text")]
        public string ExportButtonText
        {
            get => _exportButtonText;
            private set
            {
                _exportButtonText = value;
                NotifyPropertyChanged();
            }
        }

        [UIComponent("info_text")]
        private TMP_Text _infoText = null!;

        [UIComponent("save_button")]
        private Button _saveButton = null!;

        [UIComponent("export_modal_button")]
        private Button _exportModalButton = null!;

        [UIComponent("export_button")]
        private Button _exportButton = null!;
        
        [UIComponent("close_modal_button")]
        private Button _closeModalButton = null!;

        [UIAction("#post-parse")]
        private void OnParsed()
        {
            _parsed = true;
            RefreshState();
        }

        [UIAction("save_offset")]
        private void OnSaveOffsetPressed()
        {
            var leftOffset = _offsetHelper.LeftOffset;
            var rightOffset = _offsetHelper.RightOffset;
            _config.LeftOffsetPosition = leftOffset.position;
            _config.LeftOffsetRotation = leftOffset.rotation;
            _config.RightOffsetPosition = rightOffset.position;
            _config.RightOffsetRotation = rightOffset.rotation;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            if (!firstActivation)
            {
                RefreshState();
            }
        }

        private void RefreshState()
        {
            if (!_parsed) return;
            
            _saveButton.interactable = !_config.ApplyOffset && OffsetSupported;
            _exportModalButton.interactable = _config.ApplyOffset && OffsetSupported;

            if (!OffsetSupported)
            {
                _infoText.text = "Current runtime: <color=#FF0000>Unsupported</color>";
                return;
            }

            if (_config.ApplyOffset)
            {
                _infoText.text = $"Current runtime: {_vrInputHelper.RuntimeName}\n" + 
                                 "Offset is applied, disable offset to see live numbers \n" +
                                 $"L: {new Pose(_config.LeftOffsetPosition, _config.LeftOffsetRotation).Format()}\n" +
                                 $"R: {new Pose(_config.RightOffsetPosition, _config.RightOffsetRotation).Format()}";
            }
        }
        
        private void LateUpdate()
        {
            if (!_parsed || _config.ApplyOffset || !OffsetSupported) return;

            _infoText.text = $"Current runtime: {_vrInputHelper.RuntimeName}\n" + 
                             $"L Real: {_offsetHelper.LeftRuntimePose.Format()}\nR Real: {_offsetHelper.RightRuntimePose.Format()}\n" +
                             $"L Game: {_offsetHelper.LeftGamePose.Format()}\nR Game: {_offsetHelper.RightGamePose.Format()}\n" +
                             $"L Diff: {_offsetHelper.LeftOffset.Format()}\nR Diff: {_offsetHelper.RightOffset.Format()}";
        }

        [UIAction("open_export")]
        private void OpenExportModal()
        {
            if (CurExportState != ExportState.Idle) return;
            
            ExportButtonText = "Export";
            if (!EasyOffsetExporter.IsEasyOffsetInstalled)
            {
                ExportModalText = "EasyOffset is not installed.";
                CurExportState = ExportState.CannotExport;
            }
            else if (!EasyOffsetExporter.IsEasyOffsetDisabled)
            {
                ExportModalText = "EasyOffset needs to be disabled first.";
                CurExportState = ExportState.CannotExport;
            }
            else
            {
                ExportModalText = "Click export to export the saved offset to EasyOffset";
            }
            
            parserParams.EmitEvent("show_export");
            _modalShowing = true;
        }

        [UIAction("export_offset")]
        private void OnExportPressed()
        {
            if (CurExportState != ExportState.Idle) return;

            StartCoroutine(Export());
        }

        [UIAction("close_model")]
        private void OnCloseModalPressed()
        {
            if (CurExportState is not (ExportState.Idle or ExportState.CannotExport or ExportState.ExportedOrFailed)) return;
            parserParams.EmitEvent("hide");
            _modalShowing = false;
            CurExportState = ExportState.Idle;
        }
        
        private IEnumerator Export()
        {
            CurExportState = ExportState.Exporting;
            _exportButton.interactable = false;
            _closeModalButton.interactable = false;

            yield return null;
            if (EasyOffsetExporter.ExportToEastOffset())
            {
                ExportModalText = "Exported successfully";
            }
            else
            {
                ExportModalText = "Failed to export";
            }

            _closeModalButton.interactable = true;
            CurExportState = ExportState.ExportedOrFailed;
        }
    }

    public static class FormatExtensions
    {
        public static string Format(this Pose pose)
        {
            var euler = Utils.ClampAngle(pose.rotation.eulerAngles);
            return $"({pose.position.x:F3}, {pose.position.y:F3}, {pose.position.z:F3}) " + 
                   $"({euler.x:F1}, {euler.y:F1}, {euler.z:F1})";
        }
    }
}