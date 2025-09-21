using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    ///  UI controller associated with the panel to select which character to play as in the main title screen menu.
    /// </summary>
    public class CharacterSelectPanelUIController : MonoBehaviour
    {
        [SerializeField] private CharacterProperties[] _selectableCharacters;
        [SerializeField] private CurrentStatsUIController _currentStatsUIController;
        [SerializeField] private Transform _characterSelectButtonContainer;
        [SerializeField] private GameObject _characterSelectButtonPrefab;
        [SerializeField] private TextMeshProUGUI _selectedCharacterNameText;
        [SerializeField] private TextMeshProUGUI _selectedCharacterDescriptionText;
        [SerializeField] private Image _selectedCharacterImage;
        [SerializeField] private Image _selectedCharacterWeaponIconImage;
        [SerializeField] private Image _selectedCharacterFlagImage;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _unlockCharacterButton;
        [SerializeField] private TextMeshProUGUI _unlockCharacterButtonText;

        private List<GameObject> _elementsToCleanup;
        private EntityQuery _selectedCharacterReferenceQuery;
        private EntityManager _entityManager;

        private CharacterProperties _selectedCharacterProperties;
        private Button[] _characterButtons;
        
        private void Awake()
        {
            _elementsToCleanup = new List<GameObject>();
        }

        private void Start()
        {
            StartCoroutine(InitializeECS());
        }

        /// <summary>
        /// Coroutine to set the entity manager and an entity query required later in this class.
        /// </summary>
        /// <remarks>
        /// A coroutine is used to account for the unlikely case where this script loads before ECS is initialized. If the ECS world is null, it will try again the next frame and error out after 600 fails.
        /// </remarks>
        private IEnumerator InitializeECS()
        {
            var failCount = 0;
            var maxFailCount = 600;

            while (World.DefaultGameObjectInjectionWorld == null)
            {
                failCount += 1;
                if (failCount >= maxFailCount)
                {
                    Debug.LogError("Error: ECS Not Initialized");
                    yield break;
                }

                yield return null;
            }

            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _selectedCharacterReferenceQuery = _entityManager.CreateEntityQuery(typeof(SelectedCharacterReference));
        }
        
        public void ShowCharacterSelectPanel()
        {
            _characterButtons = new Button[_selectableCharacters.Length];
            Button selectedCharacterButton = null;
            var selectedCharacterUnlocked = false;
            
            for (var i = 0; i < _selectableCharacters.Length; i++)
            {
                var selectableCharacter = _selectableCharacters[i];
                var newCharacterSelectButton = Instantiate(_characterSelectButtonPrefab, _characterSelectButtonContainer);
                var isUnlocked = PersistentDataManager.Instance.IsCharacterUnlocked(selectableCharacter.CharacterID);
                var characterSelectButtonUIController = newCharacterSelectButton.GetComponent<CharacterSelectButtonUIController>();
                characterSelectButtonUIController.ShowCharacterUI(selectableCharacter, isUnlocked, () => SelectCharacter(selectableCharacter, isUnlocked, true));
                var characterButton = characterSelectButtonUIController.SelectionButton;
                
                if (selectableCharacter == _selectedCharacterProperties)
                {
                    selectedCharacterButton = characterButton;
                    selectedCharacterUnlocked = isUnlocked;
                }
                else if (_selectedCharacterProperties == null && i == 0)
                {
                    _selectedCharacterProperties = selectableCharacter;
                    selectedCharacterButton = characterButton;
                    selectedCharacterUnlocked = isUnlocked;
                }

                _characterButtons[i] = characterButton;
                _elementsToCleanup.Add(newCharacterSelectButton);
            }

            selectedCharacterButton.Select();
            SelectCharacter(_selectedCharacterProperties, selectedCharacterUnlocked);

            var navigation = _characterButtons[0].navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnRight = _characterButtons[1];
            navigation.selectOnDown = _characterButtons[3];
            _characterButtons[0].navigation = navigation;
            
            navigation = _characterButtons[1].navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnRight = _characterButtons[2];
            navigation.selectOnDown = _characterButtons[4];
            navigation.selectOnLeft = _characterButtons[0];
            _characterButtons[1].navigation = navigation;
            
            navigation = _characterButtons[2].navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnLeft = _characterButtons[1];
            navigation.selectOnDown = _characterButtons[5];
            _characterButtons[2].navigation = navigation;
            
            navigation = _characterButtons[3].navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnRight = _characterButtons[4];
            navigation.selectOnUp = _characterButtons[0];
            navigation.selectOnDown = _backButton;
            _characterButtons[3].navigation = navigation;
            
            navigation = _characterButtons[4].navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnRight = _characterButtons[5];
            navigation.selectOnUp = _characterButtons[1];
            navigation.selectOnLeft = _characterButtons[3];
            navigation.selectOnDown = _nextButton;
            _characterButtons[4].navigation = navigation;
            
            navigation = _characterButtons[5].navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnLeft = _characterButtons[4];
            navigation.selectOnUp = _characterButtons[2];
            navigation.selectOnDown = _nextButton;
            _characterButtons[5].navigation = navigation;

            navigation = _backButton.navigation;
            navigation.selectOnUp = _characterButtons[3];
            _backButton.navigation = navigation;
            
            navigation = _nextButton.navigation;
            navigation.selectOnUp = _characterButtons[5];
            _nextButton.navigation = navigation;

            navigation = _unlockCharacterButton.navigation;
            navigation.selectOnUp = _characterButtons[4];
            _unlockCharacterButton.navigation = navigation;
        }

        private void SelectCharacter(CharacterProperties selectedCharacter, bool isCharacterUnlocked, bool selectNextButton = false)
        {
            _selectedCharacterProperties = selectedCharacter;
            _currentStatsUIController.HideStatsUI();
            _currentStatsUIController.ShowStatsUIForCharacter(selectedCharacter);
            _selectedCharacterNameText.text = selectedCharacter.CharacterName;
            _selectedCharacterImage.sprite = selectedCharacter.CharacterSprite;
            _selectedCharacterWeaponIconImage.sprite = selectedCharacter.StartingWeapon.Icon;
            _selectedCharacterFlagImage.sprite = selectedCharacter.SpaceAgencySprite;

            if (isCharacterUnlocked)
            {
                _selectedCharacterDescriptionText.text = selectedCharacter.BuffDescription;
                _unlockCharacterButton.gameObject.SetActive(false);
                
                var navigation = _backButton.navigation;
                navigation.selectOnUp = _characterButtons[3];
                _backButton.navigation = navigation;
            
                navigation = _nextButton.navigation;
                navigation.selectOnUp = _characterButtons[5];
                _nextButton.navigation = navigation;
                
                navigation = _characterButtons[3].navigation;
                navigation.selectOnDown = _backButton;
                _characterButtons[3].navigation = navigation;
             
                navigation = _characterButtons[4].navigation;
                navigation.selectOnDown = _nextButton;
                _characterButtons[4].navigation = navigation;
             
                navigation = _characterButtons[5].navigation;
                navigation.selectOnDown = _nextButton;
                _characterButtons[5].navigation = navigation;
                
                _nextButton.interactable = true;
                
                if (selectNextButton)
                {
                    _nextButton.Select();
                }
            }
            else
            {
                _selectedCharacterDescriptionText.text = "";
                _unlockCharacterButton.gameObject.SetActive(true);
                _unlockCharacterButton.Select();
                _unlockCharacterButtonText.text = $"Unlock for ${selectedCharacter.UnlockCost}";
                _unlockCharacterButton.onClick.RemoveAllListeners();
                _unlockCharacterButton.onClick.AddListener(() => TryUnlockCharacter(selectedCharacter));
                
                var navigation = _backButton.navigation;
                navigation.selectOnUp = _unlockCharacterButton;
                _backButton.navigation = navigation;
            
                navigation = _nextButton.navigation;
                navigation.selectOnUp = _unlockCharacterButton;
                _nextButton.navigation = navigation;
                
                navigation = _characterButtons[3].navigation;
                navigation.selectOnDown = _unlockCharacterButton;
                _characterButtons[3].navigation = navigation;
             
                navigation = _characterButtons[4].navigation;
                navigation.selectOnDown = _unlockCharacterButton;
                _characterButtons[4].navigation = navigation;
             
                navigation = _characterButtons[5].navigation;
                navigation.selectOnDown = _unlockCharacterButton;
                _characterButtons[5].navigation = navigation;
                
                _nextButton.interactable = false;
            }
            
            if (!_selectedCharacterReferenceQuery.TryGetSingletonEntity<SelectedCharacterReference>(out var selectedCharacterReferenceEntity))
            {
                selectedCharacterReferenceEntity = _entityManager.CreateEntity(typeof(SelectedCharacterReference));
            }

            _entityManager.SetComponentData(selectedCharacterReferenceEntity, new SelectedCharacterReference
            {
                Value = selectedCharacter
            });
        }

        private void TryUnlockCharacter(CharacterProperties characterProperties)
        {
            if (PersistentDataManager.Instance.TryBuyCharacter(characterProperties.UnlockCost, characterProperties.CharacterID))
            {
                _selectedCharacterDescriptionText.text = characterProperties.BuffDescription;
                _unlockCharacterButton.gameObject.SetActive(false);
                HideCharacterSelectPanel();
                ShowCharacterSelectPanel();
                _nextButton.Select();
                
            }
        }

        public void HideCharacterSelectPanel()
        {
            foreach (var elementToCleanup in _elementsToCleanup)
            {
                Destroy(elementToCleanup);
            }

            _elementsToCleanup.Clear();
        }
    }
}