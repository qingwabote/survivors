using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI controller associated with a singular capability level up UI element that is shown in the level up UI.
    /// </summary>
    /// <remarks>
    /// Displays information about a single weapon or passive ability that is available to be leveled up.
    /// </remarks>
    public class CapabilityUpgradeUIController : MonoBehaviour
    {
        [SerializeField] private Image _itemIconImage;
        [SerializeField] private TextMeshProUGUI _itemNameText;
        [SerializeField] private TextMeshProUGUI _itemLevelText;
        [SerializeField] private TextMeshProUGUI _itemDescriptionText;
        [SerializeField] private Button _button;

        public Button Button => _button;
        
        private void OnDisable()
        {
            _button.onClick.RemoveAllListeners();
        }

        public void DisableInteraction()
        {
            _button.interactable = false;
        }
        
        /// <summary>
        /// Sets UI elements to provide information on what will change with the new level. Adds event listeners that will be invoked when the element is clicked.
        /// </summary>
        /// <param name="upgradeLevel">Helper struct containing the <see cref="UpgradeProperties"/> associated with the capability to display and the level index it is currently at.</param>
        public void SetLevelUpUI(CapabilityUpgradeLevel upgradeLevel)
        {
            var upgradeProperties = upgradeLevel.UpgradeProperties;
            var nextLevelIndex = upgradeLevel.NextLevelIndex;
            _itemIconImage.sprite = upgradeProperties.Icon;
            _itemNameText.text = upgradeProperties.Name;
            var itemLevelText = nextLevelIndex == 0 ? "<color=yellow>New!</color>" : $"Level: {nextLevelIndex + 1}";
            _itemLevelText.text = itemLevelText;
            _itemDescriptionText.text = upgradeProperties.GetDescription(nextLevelIndex);
            _button.onClick.AddListener(() => CapabilityUpgradeController.Instance.UpgradeCapability(upgradeProperties));
            _button.onClick.AddListener(() => LevelUpUIController.Instance.HideLevelUpUI());
            _button.onClick.AddListener(() => SelectionIconUIController.Instance.SetPositionOffscreen());
        }

        /// <summary>
        /// Alternate method to set UI elements for "bonus items" which will be displayed when the player has all 10 capabilities at max level.
        /// </summary>
        /// <param name="itemProperties">Properties of the bonus item to be displayed in the UI.</param>
        public void SetLevelUpUI(BonusItemProperties itemProperties)
        {
            _itemIconImage.sprite = itemProperties.ItemIcon;
            _itemNameText.text = itemProperties.ItemName;
            _itemLevelText.text = "";
            _itemDescriptionText.text = itemProperties.ItemDescription;
            _button.onClick.AddListener(() => CapabilityUpgradeController.Instance.CollectBonusItem(itemProperties.ItemType, itemProperties.ItemValue));
            _button.onClick.AddListener(() => LevelUpUIController.Instance.HideLevelUpUI());
            _button.onClick.AddListener(() => SelectionIconUIController.Instance.SetPositionOffscreen());
        }
    }
}