using System;
using System.ComponentModel;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberOffsetMigrator.Configuration;
using SiraUtil.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BeatSaberOffsetMigrator.UI
{
    [ViewDefinition("BeatSaberOffsetMigrator.UI.BSML.MenuView.bsml")]
    internal class MenuViewController : BSMLAutomaticViewController
    {
        [Inject]
        private readonly SiraLog _logger = null!;
        
        [Inject]
        private readonly PluginConfig _config = null!;
        
        [Inject]
        private readonly OffsetHelper _offsetHelper = null!;
        
        private bool _parsed = false;

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

        [UIComponent("info_text")]
        private TMP_Text _infoText = null!;

        [UIComponent("save_button")]
        private Button _saveButton = null!;

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
            
            _saveButton.interactable = !_config.ApplyOffset;

            if (_config.ApplyOffset)
            {
                _infoText.text = "Offset is applied, disable offset to see live numbers \n" +
                                 $"L: {new Pose(_config.LeftOffsetPosition, _config.LeftOffsetRotation).Format()}\n" +
                                 $"R: {new Pose(_config.RightOffsetPosition, _config.RightOffsetRotation).Format()}";
            }
        }
        
        private void LateUpdate()
        {
            if (!_parsed || _config.ApplyOffset) return;

            _infoText.text = $"L Real: {_offsetHelper.LeftSteamVRPose.Format()}\nR Real: {_offsetHelper.RightSteamVRPose.Format()}\n" +
                             $"L Game: {_offsetHelper.LeftGamePose.Format()}\nR Game: {_offsetHelper.RightGamePose.Format()}\n" +
                             $"L Diff: {_offsetHelper.LeftOffset.Format()}\nR Diff: {_offsetHelper.RightOffset.Format()}";
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