using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI controller associated with a single UI element for a character that can be selected in the main menu UI.
    /// </summary>
    /// <seealso cref="CharacterSelectPanelUIController"/>
    public class CharacterSelectButtonUIController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _selectedCharacterNameText;
        [SerializeField] private Image _selectedCharacterImage;
        [SerializeField] private Image _selectedCharacterWeaponIconImage;
        [SerializeField] private Image _selectedCharacterSpaceAgencyImage;
        [SerializeField] private Button _selectionButton;
        [SerializeField] private GameObject _lockedIcon;
        [SerializeField] private TextMeshProUGUI _unlockCostText;

        public Button SelectionButton => _selectionButton;
        
        private void OnDisable()
        {
            _selectionButton.onClick.RemoveAllListeners();
        }
        
        /// <summary>
        /// Sets UI elements associated with the stage available to be selected.
        /// </summary>
        /// <param name="characterProperties">ScriptableObject containing information and assets pertaining to this character.</param>
        /// <param name="isUnlocked">Flag to denote if the character is unlocked. If the character is locked, a lock icon will be shown with the price to unlock.</param>
        /// <param name="showSelectedCharacterInformationAction">Method that will be called when the main button associated with this UI element is clicked. Will set UI elements in <see cref="CharacterSelectPanelUIController.SelectCharacter"/></param>
        /// <seealso cref="CharacterSelectPanelUIController"/>
        public void ShowCharacterUI(CharacterProperties characterProperties, bool isUnlocked, UnityAction showSelectedCharacterInformationAction)
        {
            _selectedCharacterNameText.text = characterProperties.CharacterName;
            _selectedCharacterImage.sprite = characterProperties.CharacterSprite;
            _selectedCharacterWeaponIconImage.sprite = characterProperties.StartingWeapon.Icon;
            _selectedCharacterSpaceAgencyImage.sprite = characterProperties.SpaceAgencySprite;
            _selectionButton.onClick.AddListener(showSelectedCharacterInformationAction);
            if (isUnlocked)
            {
                _lockedIcon.SetActive(false);
            }
            else
            {
                _lockedIcon.SetActive(true);
                _unlockCostText.text = $"${characterProperties.UnlockCost}";
            }
        }
    }
}