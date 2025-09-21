using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI controller associated with the individual UI elements to display a stage available to be chosen.
    /// </summary>
    public class StageSelectButtonUIController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _stageNameText;
        [SerializeField] private TextMeshProUGUI _stageDescriptionText;
        [SerializeField] private Image _stagePreviewImage;
        [SerializeField] private Button _selectStageButton;
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private TextMeshProUGUI _unlockText;
        
        public Button SelectionButton => _selectStageButton;
        
        /// <summary>
        /// Sets UI elements associated with the stage available to be selected.
        /// </summary>
        /// <param name="stageProperties">ScriptableObject containing information and assets pertaining to this stage.</param>
        /// <param name="isUnlocked">Flag to denote if this stage is unlocked. If the stage is locked, a lock icon will be shown with the price to unlock.</param>
        /// <param name="onSelectStage">Method that will be called when the main button associated with this UI element is clicked.</param>
        public void SetUI(StageProperties stageProperties, bool isUnlocked, UnityAction onSelectStage)
        {
            _stageNameText.text = stageProperties.StageName;
            _stagePreviewImage.sprite = stageProperties.PreviewSprite;
            _selectStageButton.onClick.AddListener(onSelectStage);
            if (isUnlocked)
            {
                _stageDescriptionText.text = stageProperties.StageDescription;
                _lockIcon.SetActive(false);
            }
            else
            {
                _stageDescriptionText.text = "";
                _lockIcon.SetActive(true);
                _unlockText.text = $"Unlock for\n${stageProperties.UnlockCost:N0}";
            }
        }
    }
}