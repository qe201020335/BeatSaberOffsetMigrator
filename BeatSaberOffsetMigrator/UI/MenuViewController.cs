using System.Collections;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.EO;
using BeatSaberOffsetMigrator.InputHelper;
using BeatSaberOffsetMigrator.Utils;
using SiraUtil.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.OpenXR;
using Zenject;

namespace BeatSaberOffsetMigrator.UI
{
    [ViewDefinition("BeatSaberOffsetMigrator.UI.BSML.MenuView.bsml")]
    [HotReload(RelativePathToLayout = @"BSML\MenuView.bsml")]
    internal class MenuViewController : BSMLAutomaticViewController
    {
        internal enum ExportState
        {
            /// <summary>
            /// Export/Save have not begun
            /// </summary>
            Idle,
            /// <summary>
            /// Cannot start exporting or saving offset
            /// </summary>
            CannotExport,
            /// <summary>
            /// Exporting/Saving offset
            /// </summary>
            Exporting,
            /// <summary>
            /// Failed to export/saved offset
            /// </summary>
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
        
        [Inject]
        private readonly EasyOffsetExporter _easyOffsetExporter = null!;
        
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
                        _closeExportModalButton.interactable = true;
                        _saveButton.interactable = true;
                        _closeSaveModalButton.interactable = true;
                        break;
                    case ExportState.CannotExport:
                        _exportButton.interactable = false;
                        _closeExportModalButton.interactable = true;
                        _saveButton.interactable = false;
                        _closeSaveModalButton.interactable = true;
                        break;
                    case ExportState.Exporting:
                        _exportButton.interactable = false;
                        _closeExportModalButton.interactable = false;
                        _saveButton.interactable = false;
                        _closeSaveModalButton.interactable = false;
                        break;
                    case ExportState.ExportedOrFailed:
                        _exportButton.interactable = false;
                        _closeExportModalButton.interactable = true;
                        _saveButton.interactable = false;
                        _closeSaveModalButton.interactable = true;
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
        
        [UIValue("RecordUnityOffset")]
        private bool RecordUnityOffset { get; set; } = false;

        [UIValue("supported")]
        private bool OffsetSupported => _vrInputHelper.Supported;
        
        private string _exportModalText = "Export to EasyOffset";
        [UIValue("export_modal_text")]
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
        
        private string _saveModalText = "Save current offset";
        [UIValue("save_modal_text")]
        public string SaveModalText
        {
            get => _saveModalText;
            private set
            {
                _saveModalText = value;
                NotifyPropertyChanged();
            }
        }
        
        private string _saveButtonText = "Save";
        [UIValue("save_button_text")]
        public string SaveButtonText
        {
            get => _saveButtonText;
            private set
            {
                _saveButtonText = value;
                NotifyPropertyChanged();
            }
        }
        

        [UIComponent("info_text")]
        private TMP_Text _infoText = null!;

        [UIComponent("save_modal_button")]
        private Button _saveModalButton = null!;

        [UIComponent("export_modal_button")]
        private Button _exportModalButton = null!;

        [UIComponent("export_button")]
        private Button _exportButton = null!;
        
        [UIComponent("close_export_modal_button")]
        private Button _closeExportModalButton = null!;
        
        [UIComponent("save_button")]
        private Button _saveButton = null!;
        
        [UIComponent("close_save_modal_button")]
        private Button _closeSaveModalButton = null!;

        [UIAction("#post-parse")]
        private void OnParsed()
        {
            _parsed = true;
            RefreshState();
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
            
            _saveModalButton.interactable = !_config.ApplyOffset && OffsetSupported;
            _exportModalButton.interactable = _config.ApplyOffset && OffsetSupported;

            if (!OffsetSupported)
            {
                _infoText.text = $"Current runtime: {(string.IsNullOrWhiteSpace(OpenXRRuntime.name) ? "Unknown" : OpenXRRuntime.name)}";
                _infoText.text += $"\n<color=#FF0000>{_vrInputHelper.ReasonIfNotWorking}</color>";
            }
        }
        
        private void LateUpdate()
        {
            if (!_parsed || !OffsetSupported) return;
            
            if (!_vrInputHelper.Working)
            {
                _infoText.text = $"Current runtime: {OpenXRRuntime.name} using helper {_vrInputHelper.GetType().Name}\n" + 
                                 $"<color=#FF0000>{_vrInputHelper.ReasonIfNotWorking}</color>";
                return;
            }
            
            if (_config.ApplyOffset)
            {
                _infoText.text = $"Current runtime: {OpenXRRuntime.name} using helper {_vrInputHelper.GetType().Name}\n" + 
                                 "Offset is applied, disable offset to see live numbers \n" +
                                 $"L: {new Pose(_config.LeftOffsetPosition, _config.LeftOffsetRotation).Format()}\n" +
                                 $"R: {new Pose(_config.RightOffsetPosition, _config.RightOffsetRotation).Format()}";
            }
            else
            {
                _infoText.text = $"Current runtime: {OpenXRRuntime.name} using helper {_vrInputHelper.GetType().Name}\n" + 
                                 $"L Real: {_offsetHelper.LeftRuntimePose.Format()}\nR Real: {_offsetHelper.RightRuntimePose.Format()}\n" +
                                 $"L Game: {_offsetHelper.LeftGamePose.Format()}\nR Game: {_offsetHelper.RightGamePose.Format()}\n" +
                                 $"L Diff: {_offsetHelper.LeftOffset.Format()}\nR Diff: {_offsetHelper.RightOffset.Format()}";
            }
        }

        [UIAction("open_save")]
        private void OpenSaveModal()
        {
            if (CurExportState != ExportState.Idle) return;
            SaveButtonText = "Save";
            if (!_offsetHelper.IsWorking)
            {
                SaveModalText = "Controllers are not Tracking.";
                CurExportState = ExportState.CannotExport;
            }
            else
            {
                SaveModalText = "After pressing the save button, try holding your controller still " +
                                "or put it somewhere stable with good tracking. " +
                                "Offset will be saved 10 seconds after pressing the button.";
            }
            
            
            parserParams.EmitEvent("show_save");
            _modalShowing = true;
        }
        
        [UIAction("save_offset")]
        private void OnSavePressed()
        {
            if (CurExportState != ExportState.Idle) return;

            StartCoroutine(Save());
        }
        
        private IEnumerator Save()
        {
            CurExportState = ExportState.Exporting;
            
            for (var i = 10; i > 0; i--)
            {
                SaveButtonText = $"Save in {i}";
                yield return new WaitForSeconds(1);
            }
            
            yield return null;
            if (!_offsetHelper.IsWorking)
            {
                SaveModalText = "Controllers are not tracking\nCannot save";
                SaveButtonText = "Save";
            }
            else
            {
                if (RecordUnityOffset)
                {
                    _offsetHelper.SaveUnityOffset();
                }
                else
                {
                    var leftOffset = _offsetHelper.LeftOffset;
                    var rightOffset = _offsetHelper.RightOffset;
                    _config.LeftOffsetPosition = leftOffset.position;
                    _config.LeftOffsetRotation = leftOffset.rotation;
                    _config.RightOffsetPosition = rightOffset.position;
                    _config.RightOffsetRotation = rightOffset.rotation;
                }

                SaveModalText = "Offset saved successfully";
                SaveButtonText = "Saved";
            }

            CurExportState = ExportState.ExportedOrFailed;
        }

        [UIAction("open_export")]
        private void OpenExportModal()
        {
            if (CurExportState != ExportState.Idle) return;
            
            ExportButtonText = "Export";
            if (!_easyOffsetExporter.IsEasyOffsetInstalled)
            {
                ExportModalText = "EasyOffset is not installed.";
                CurExportState = ExportState.CannotExport;
            }
            else if (!_easyOffsetExporter.IsEasyOffsetDisabled)
            {
                ExportModalText = "EasyOffset needs to be disabled first.";
                CurExportState = ExportState.CannotExport;
            }
            else if (!_offsetHelper.IsWorking)
            {
                ExportModalText = "Controllers are not Tracking.";
                CurExportState = ExportState.CannotExport;
            }
            else
            {
                ExportModalText = "After pressing the export button, try holding your controller still " +
                                  "or put it somewhere stable with good tracking. " + 
                                  "Offset will be exported to EasyOffset 10 seconds after pressing the button.";
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

        [UIAction("close_modal")]
        private void OnCloseModalPressed()
        {
            if (CurExportState is ExportState.Exporting) return;
            parserParams.EmitEvent("hide");
            _modalShowing = false;
            CurExportState = ExportState.Idle;
        }
        
        private IEnumerator Export()
        {
            CurExportState = ExportState.Exporting;
            
            for (var i = 10; i > 0; i--)
            {
                ExportButtonText = $"Export in {i}";
                yield return new WaitForSeconds(1);
            }
            
            ExportButtonText = "Export";
            
            yield return null;
            if (!_offsetHelper.IsWorking)
            {
                ExportModalText = "Controllers are not tracking\nCannot export";
            }
            else if (_easyOffsetExporter.ExportToEastOffset())
            {
                ExportModalText = "Exported successfully";
                ExportButtonText = "Exported";
            }
            else
            {
                ExportModalText = "Failed to export";
            }

            CurExportState = ExportState.ExportedOrFailed;
        }
    }

    public static class FormatExtensions
    {
        public static string Format(this Pose pose)
        {
            var euler = PoseUtils.ClampAngle(pose.rotation.eulerAngles);
            return $"({pose.position.x:F3}, {pose.position.y:F3}, {pose.position.z:F3}) " + 
                   $"({euler.x:F1}, {euler.y:F1}, {euler.z:F1})";
        }
    }
}