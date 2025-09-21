using System;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI controller associated with the stage select panel displayed in the main menu of the title scene.
    /// </summary>
    public class StageSelectPanelUIController : MonoBehaviour
    {
        [SerializeField] private StageProperties[] _availableStages;
        [SerializeField] private GameObject _selectableStageUIPrefab;
        [SerializeField] private Transform _selectableStagesContainer;
        [SerializeField] private TextMeshProUGUI _selectedStageNameText;
        [SerializeField] private TextMeshProUGUI _stageTimeLimitText;
        [SerializeField] private Button _goButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _artTestSceneButton;
        
        public int SelectedStageSceneIndex { get; private set; }
        
        private List<GameObject> _elementsToCleanup;
        
        private void Awake()
        {
            _elementsToCleanup = new List<GameObject>();
        }

        public void ShowStageSelectPanel(int stageIndexToSelect = 0)
        {
            var stageButtons = new List<Button>();
            
            for (var i = 0; i < _availableStages.Length; i++)
            {
                var availableStage = _availableStages[i];
                var newSelectableStageUI = Instantiate(_selectableStageUIPrefab, _selectableStagesContainer);
                var selectableStageButtonUIController = newSelectableStageUI.GetComponent<StageSelectButtonUIController>();
                var isUnlocked = PersistentDataManager.Instance.IsStageUnlocked(availableStage.StageID);
                var stageIndex = i;
                selectableStageButtonUIController.SetUI(availableStage, isUnlocked, () => SelectStage(availableStage, stageIndex, isUnlocked, true));
                stageButtons.Add(selectableStageButtonUIController.SelectionButton);
                if (i == stageIndexToSelect)
                {
                    selectableStageButtonUIController.SelectionButton.Select();
                }
                _elementsToCleanup.Add(newSelectableStageUI);
            }

            var artTestSceneUnlocked = PersistentDataManager.Instance.IsStageUnlocked(StageID.ArtTestScene);
            if (artTestSceneUnlocked)
            {
                _artTestSceneButton.gameObject.SetActive(true);
                stageButtons.Add(_artTestSceneButton);
            }
            else
            {
                _artTestSceneButton.gameObject.SetActive(false);
            }
            
            for (var i = 0; i < stageButtons.Count; i++)
            {
                var navigation = stageButtons[i].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                if (i != 0)
                {
                    navigation.selectOnUp = stageButtons[i - 1];
                }

                if (i == stageButtons.Count- 1)
                {
                    if (artTestSceneUnlocked)
                    {
                        navigation.selectOnLeft = _backButton;
                        navigation.selectOnRight = _goButton;
                    }
                    else
                    {
                        navigation.selectOnDown = _goButton;
                    }
                }
                else
                {
                    navigation.selectOnDown = stageButtons[i + 1];
                }

                stageButtons[i].navigation = navigation;
            }

            var goNavigation = _goButton.navigation;
            if (artTestSceneUnlocked)
            {
                goNavigation.selectOnUp = stageButtons[^2];
                goNavigation.selectOnLeft = stageButtons[^1];
            }
            else
            {
                goNavigation.selectOnUp = stageButtons[^1];
                goNavigation.selectOnLeft = _backButton;
            }
            _goButton.navigation = goNavigation;
            
            var backNavigation = _backButton.navigation;
            if (artTestSceneUnlocked)
            {
                backNavigation.selectOnUp = stageButtons[^2];
                backNavigation.selectOnRight = stageButtons[^1];
            }
            else
            {
                backNavigation.selectOnUp = stageButtons[^1];
                backNavigation.selectOnRight = _goButton;
            }
            _backButton.navigation = backNavigation;
            
            SelectStage(_availableStages[stageIndexToSelect], stageIndexToSelect, true, false);
        }

        private void SelectStage(StageProperties selectedStage, int stageIndex, bool isUnlocked, bool selectGoButton)
        {
            SelectedStageSceneIndex = selectedStage.SceneIndex;
            _selectedStageNameText.text = $"{selectedStage.StageName}";
            var stageTimeLimitSeconds = selectedStage.TimeLimit;
            var timeSpan = TimeSpan.FromSeconds(stageTimeLimitSeconds);
            var stageTimeLimitString = timeSpan.ToString(@"mm\:ss");
            _stageTimeLimitText.text = $"Stage Time Limit\n{stageTimeLimitString}";

            if (isUnlocked)
            {
                _goButton.interactable = true;
                if (selectGoButton)
                {
                    _goButton.Select();
                }
            }
            else
            {
                if (PersistentDataManager.Instance.TryBuyStage(selectedStage.UnlockCost, selectedStage.StageID))
                {
                    HideStageSelectPanel();
                    ShowStageSelectPanel(stageIndex);
                    _goButton.Select();
                }
                else
                {
                    _goButton.interactable = false;
                }
            }
        }
        
        public void HideStageSelectPanel()
        {
            foreach (var elementToCleanup in _elementsToCleanup)
            {
                Destroy(elementToCleanup);
            }

            _elementsToCleanup.Clear();
        }
    }
}