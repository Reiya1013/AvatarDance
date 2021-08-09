using UnityEngine;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using System.IO;
using AvatarDance.Parameter;

namespace AvatarDance.Views
{
    class AvatarDanceUI : BSMLAutomaticViewController
    {
        public ModMainFlowCoordinator mainFlowCoordinator { get; set; }
        public void SetMainFlowCoordinator(ModMainFlowCoordinator mainFlowCoordinator)
        {
            this.mainFlowCoordinator = mainFlowCoordinator;
        }
        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        }
        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            Plugin.Dance.VRMCopyDestroy();
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        [UIComponent("DanceList")]
        private CustomListTableData dancelNameList = new CustomListTableData();
        private int selectRow;
        [UIAction("DanceSelect")]
        private void Select(TableView _, int row)
        {
            selectRow = row;
        }

        [UIValue("x-position")]
        private float XPosition = PluginParameter.Instance.Offset_XPosition;
        [UIAction("OnXPositionChange")]
        private void OnXPositionChange(float value)
        {
            XPosition = value;
            PluginParameter.Instance.Offset_XPosition = value;
        }
        [UIValue("y-position")]
        private float YPosition = PluginParameter.Instance.Offset_YPosition;
        [UIAction("OnYPositionChange")]
        private void OnYPositionChange(float value)
        {
            YPosition = value;
            PluginParameter.Instance.Offset_YPosition = value;
        }
        [UIValue("z-position")]
        private float ZPosition = PluginParameter.Instance.Offset_ZPosition;
        [UIAction("OnZPositionChange")]
        private void OnZPositionChange(float value)
        {
            ZPosition = value;
            PluginParameter.Instance.Offset_ZPosition = value;
        }
        [UIValue("x-rotation")]
        private float XRotation = PluginParameter.Instance.Offset_XRotation;
        [UIAction("OnXRotationChange")]
        private void OnXRotationChange(float value)
        {
            XRotation = value;
            PluginParameter.Instance.Offset_XRotation = value;
        }

        [UIValue("y-rotation")]
        private float YRotation = PluginParameter.Instance.Offset_YRotation;
        [UIAction("OnYRotationChange")]
        private void OnYRotationChange(float value)
        {
            YRotation = value;
            PluginParameter.Instance.Offset_YRotation = value;
        }
        [UIValue("z-rotation")]
        private float ZRotation = PluginParameter.Instance.Offset_ZRotation;
        [UIAction("OnZRotationChange")]
        private void OnZRotationChange(float value)
        {
            ZRotation = value;
            PluginParameter.Instance.Offset_ZRotation = value;
        }



        [UIAction("DanceStart")]
        private void AvatarDanceStart()
        {
            Debug.Log($"Dance Start Parametere X{XPosition} {PluginParameter.Instance.Offset_XPosition}");
            Plugin.Dance.VRMCopyDestroy();
            SharedCoroutineStarter.instance.StartCoroutine(Plugin.Dance.GetVRMAndSetDance(selectRow));
        }

        [UIAction("AvatarDestroy")]
        private void AvatarDestroy()
        {
            Plugin.Dance.VRMCopyDestroy();
        }



        [UIAction("#post-parse")]
        public void SetupList()
        {

            dancelNameList.data.Clear();
            foreach (var materialName in Plugin.Dance.GetDanceName())
            {
                var customCellInfo = new CustomListTableData.CustomCellInfo(materialName);
                dancelNameList.data.Add(customCellInfo);
            }

            dancelNameList.tableView.ReloadData();
        }
    }
}
