using UnityEngine;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using System.IO;

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


        [UIAction("DanceStart")]
        private void AvatarDanceStart()
        {
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
