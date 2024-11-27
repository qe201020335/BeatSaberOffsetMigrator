using System.Collections;
using System.Text;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberOffsetMigrator.Configuration;
using BeatSaberOffsetMigrator.EO;
using BeatSaberOffsetMigrator.InputHelper;
using BeatSaberOffsetMigrator.Patches;
using BeatSaberOffsetMigrator.Utils;
using BGLib.Polyglot;
using SiraUtil.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.OpenXR;
using Zenject;

namespace BeatSaberOffsetMigrator.UI
{
    [ViewDefinition("BeatSaberOffsetMigrator.UI.BSML.AdvanceView.bsml")]
    [HotReload(RelativePathToLayout = @"BSML\AdvanceView.bsml")]
    internal class AdvanceViewController : BSMLAutomaticViewController
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
        
        [Inject]
        private readonly VRControllerPatch _vrControllerPatch = null!;
        
        private bool _parsed = false;

        private bool _modalShowing = false;
        public bool ModalShowing
        {
            get => _modalShowing;
            set
            {
                _modalShowing = value;
                NotifyPropertyChanged();
            }
        }
        
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
        
        private readonly StringBuilder _infoTextBuilder = new StringBuilder(256);
        
        [UIParams]
        private BSMLParserParams parserParams = null!;
        
        [UIValue("RecordUnityOffset")]
        private bool RecordUnityOffset { get; set; } = false;

        [UIValue("supported")]
        private bool OffsetSupported => _vrInputHelper.Supported;
        
        private string _exportModalText = "";
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
        
        private string _exportButtonText = Localization.Get("BSOM_AM_EXPORT_MODAL_EXPORT");
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
        
        private string _saveModalText = "";
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
        
        private string _saveButtonText = Localization.Get("BSOM_AM_SAVE_MODAL_SAVE");
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

            _config.ConfigDidChange += OnConfigChanged;
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            _config.ConfigDidChange -= OnConfigChanged;
        }

        private void OnConfigChanged()
        {
            RefreshState();
        }

        private void RefreshState()
        {
            if (!_parsed) return;
            
            _saveModalButton.interactable = !_config.ApplyOffset && OffsetSupported;
            _exportModalButton.interactable = _config.ApplyOffset && OffsetSupported;

            if (!OffsetSupported)
            {
                var runtimeName = string.IsNullOrWhiteSpace(OpenXRRuntime.name) ? Localization.Get("BSOM_GENERIC_UNKNOWN") : OpenXRRuntime.name;
                _infoText.text = string.Format(Localization.Get("BSOM_AM_CURRENT_RUNTIME_UNSUPPORTED"), runtimeName);
                _infoText.text += $"\n<color=#FF0000>{_vrInputHelper.ReasonIfNotWorking}</color>";
            }
        }

        private void LateUpdate()
        {
            if (!_parsed || !OffsetSupported) return;

            _infoTextBuilder.Clear();

            var runtimeText = string.Format(Localization.Get("BSOM_AM_CURRENT_RUNTIME_SUPPORTED"), OpenXRRuntime.name, _vrInputHelper.GetType().Name);
            _infoTextBuilder.Append(runtimeText).Append("\n");

            if (!_vrInputHelper.Working)
            {
                _infoTextBuilder.Append("<color=#FF0000>").Append(_vrInputHelper.ReasonIfNotWorking).Append("</color>\n");
            }
            else if (_config.ApplyOffset && !_vrControllerPatch.UseGeneratedOffset)
            {
                var info = string.Format(Localization.Get("BSOM_AM_OFFSET_APPLIED"), _config.LeftOffset.Format(), _config.RightOffset.Format());
                _infoTextBuilder.Append(info).Append("\n");
            }
            else
            {
                if (_config.ApplyOffset && _vrControllerPatch.UseGeneratedOffset)
                {
                    _infoTextBuilder.Append(Localization.Get("BSOM_AM_AM_NOT_ENABLED")).Append("\n");
                }

                var poseText = string.Format(
                    Localization.Get("BSOM_AM_CONTROLLER_POSES"),
                    _offsetHelper.LeftRuntimePose.Format(),
                    _offsetHelper.RightRuntimePose.Format(),
                    _offsetHelper.LeftGamePose.Format(),
                    _offsetHelper.RightGamePose.Format(),
                    _offsetHelper.LeftOffset.Format(),
                    _offsetHelper.RightOffset.Format());

                _infoTextBuilder.Append(poseText).Append("\n");
            }

            _infoText.text = _infoTextBuilder.ToString();
        }

        [UIAction("open_save")]
        private void OpenSaveModal()
        {
            if (CurExportState != ExportState.Idle) return;
            SaveButtonText = Localization.Get("BSOM_AM_SAVE_MODAL_SAVE");
            if (!_offsetHelper.IsRuntimePoseValid)
            {
                SaveModalText = Localization.Get("BSOM_AM_SAVE_MODAL_INVALID");
                CurExportState = ExportState.CannotExport;
            }
            else
            {
                SaveModalText = Localization.Get("BSOM_AM_SAVE_MODAL_TEXT");
            }
            
            
            parserParams.EmitEvent("show_save");
            ModalShowing = true;
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
                SaveButtonText = string.Format(Localization.Get("BSOM_AM_SAVE_MODAL_COUNTDOWN"), i);
                yield return new WaitForSeconds(1);
            }
            
            yield return null;
            if (!_offsetHelper.IsRuntimePoseValid)
            {
                SaveModalText = Localization.Get("BSOM_AM_SAVE_MODAL_POST_INVALID");
                SaveButtonText = Localization.Get("BSOM_AM_SAVE_MODAL_SAVE");
            }
            else
            {
                if (RecordUnityOffset)
                {
                    SaveModalText = Localization.Get("BSOM_AM_SAVE_MODAL_SAMPLING");
                    SaveButtonText = Localization.Get("BSOM_AM_SAVE_MODAL_SAVING");
                    yield return _offsetHelper.SaveUnityOffset();
                }
                else
                {
                    _config.LeftOffset = _offsetHelper.LeftOffset;
                    _config.RightOffset = _offsetHelper.RightOffset;
                }

                SaveModalText = Localization.Get("BSOM_AM_SAVE_MODAL_SUCCESS");
                SaveButtonText = Localization.Get("BSOM_AM_SAVE_MODAL_SAVED");
            }

            CurExportState = ExportState.ExportedOrFailed;
        }

        [UIAction("open_export")]
        private void OpenExportModal()
        {
            if (CurExportState != ExportState.Idle) return;
            
            ExportButtonText = Localization.Get("BSOM_AM_EXPORT_MODAL_EXPORT");
            if (!_easyOffsetExporter.IsEasyOffsetInstalled)
            {
                ExportModalText = Localization.Get("BSOM_AM_EXPORT_MODAL_EO_NOT_INSTALLED");
                CurExportState = ExportState.CannotExport;
            }
            else if (!_easyOffsetExporter.IsEasyOffsetDisabled)
            {
                ExportModalText = Localization.Get("BSOM_AM_EXPORT_MODAL_EO_NOT_DISABLED");
                CurExportState = ExportState.CannotExport;
            }
            else if (!_offsetHelper.IsRuntimePoseValid)
            {
                ExportModalText = Localization.Get("BSOM_AM_EXPORT_MODAL_INVALID");
                CurExportState = ExportState.CannotExport;
            }
            else
            {
                ExportModalText = Localization.Get("BSOM_AM_EXPORT_MODAL_TEXT");
            }
            
            parserParams.EmitEvent("show_export");
            ModalShowing = true;
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
            ModalShowing = false;
            CurExportState = ExportState.Idle;
        }
        
        private IEnumerator Export()
        {
            CurExportState = ExportState.Exporting;
            
            for (var i = 10; i > 0; i--)
            {
                ExportButtonText = string.Format(Localization.Get("BSOM_AM_EXPORT_MODAL_COUNTDOWN"), i);
                yield return new WaitForSeconds(1);
            }
            
            ExportButtonText = Localization.Get("BSOM_AM_EXPORT_MODAL_EXPORT");
            
            yield return null;
            if (!_offsetHelper.IsRuntimePoseValid)
            {
                ExportModalText = Localization.Get("BSOM_AM_EXPORT_MODAL_POST_INVALID");
            }
            else if (_easyOffsetExporter.ExportToEastOffset())
            {
                ExportModalText = Localization.Get("BSOM_AM_EXPORT_MODAL_SUCCESS");
                ExportButtonText = Localization.Get("BSOM_AM_EXPORT_MODAL_EXPORTED");
            }
            else
            {
                ExportModalText = Localization.Get("BSOM_AM_EXPORT_MODAL_FAILED");
            }

            CurExportState = ExportState.ExportedOrFailed;
        }
    }
}