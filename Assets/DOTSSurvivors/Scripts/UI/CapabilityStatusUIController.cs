using UnityEngine;
using UnityEngine.UI;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI controller associated with an individual capability to be displayed in the game pause menu.
    /// </summary>
    /// <remarks>
    /// Capabilities are weapons and passive abilities the player currently has. This controller displays the icon of the capability and uses UI toggles to indicate the number of levels the capability has (via toggle count) and what the current level is (via enabled toggle count).
    /// </remarks>
    /// <seealso cref="CurrentCapabilitiesUIController"/>
    public class CapabilityStatusUIController : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Toggle[] _levelStatus;

        public void DisplayUI(UpgradeProperties properties, int currentLevelIndex)
        {
            _icon.sprite = properties.Icon;
            for (var i = _levelStatus.Length - 1; i > properties.MaxLevelIndex; i--)
            {
                _levelStatus[i].gameObject.SetActive(false);
            }

            for (var i = 0; i <= currentLevelIndex; i++)
            {
                _levelStatus[i].isOn = true;
            }
        }
    }
}