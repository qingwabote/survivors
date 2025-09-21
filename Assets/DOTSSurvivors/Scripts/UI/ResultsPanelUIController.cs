using System;
using UnityEngine;
using TMPro;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI controller associated with the results panel that is displayed after the game over screen.
    /// </summary>
    public class ResultsPanelUIController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _astronautNameText;
        [SerializeField] private TextMeshProUGUI _stageInfoText;
        [SerializeField] private TextMeshProUGUI _timeSurvivedText;
        [SerializeField] private TextMeshProUGUI _coinsEarnedText;
        [SerializeField] private TextMeshProUGUI _aliensDefeatedText;
        
        public void ShowResultsUI()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            var gameTimeQuery = entityManager.CreateEntityQuery(typeof(GameTime));
            var secondsInGame = gameTimeQuery.GetSingleton<GameTime>().Value;
            var timeInGame = TimeSpan.FromSeconds(Mathf.FloorToInt(secondsInGame));
            _timeSurvivedText.text = $"Time Survived: {timeInGame:m\\:ss}";

            var coinCountQuery = entityManager.CreateEntityQuery(typeof(CoinsCollected));
            var coinCount = coinCountQuery.GetSingleton<CoinsCollected>().Value;
            _coinsEarnedText.text = $"Coins Earned: {coinCount:N0}";

            var aliensDefeatedQuery = entityManager.CreateEntityQuery(typeof(AliensDefeatedCount));
            var aliensDefeated = aliensDefeatedQuery.GetSingleton<AliensDefeatedCount>().Value;
            _aliensDefeatedText.text = $"Aliens Defeated: {aliensDefeated:N0}";

            PersistentDataManager.Instance.AddMoney(coinCount);
            PersistentDataManager.Instance.AddEnemiesDefeated(aliensDefeated);
            PersistentDataManager.Instance.AddTimeSurvived(secondsInGame);

            // Super hacky hardcoded value to determine that the player has completed the Venus stage and the art test scene should be unlocked.
            if (secondsInGame >= 1800)
            {
                PersistentDataManager.Instance.UnlockArtTestScene();
            }
        }
    }
}