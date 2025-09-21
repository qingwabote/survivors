using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI controller for the loading screen that is displayed when switching between title scene and gameplay scenes.
    /// </summary>
    /// <remarks>
    /// Marked as don't destroy on load so it can be shown/hidden across scene bounds.
    /// </remarks>
    public class LoadingScreenUIController : MonoBehaviour
    {
        public static LoadingScreenUIController Instance;

        [SerializeField] private GameObject _loadingScreen;
        
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            HideLoadingScreen();
        }
        
        public void ShowLoadingScreen()
        {
            _loadingScreen.SetActive(true);
        }

        public void HideLoadingScreen()
        {
            _loadingScreen.SetActive(false);
        }
    }
}