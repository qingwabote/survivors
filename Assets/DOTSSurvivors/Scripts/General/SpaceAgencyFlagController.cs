using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Singleton MonoBehaviour to control setting of the space agency flag in each level with the flag of the nation associated with the current character.
    /// </summary>
    /// <remarks>
    /// This controller will exist on the 'Flag' GameObject that is a child of the 'Flagpole' GameObject in the root of the scene.
    /// </remarks>
    public class SpaceAgencyFlagController : MonoBehaviour
    {
        /// <summary>
        /// Public singleton instance.
        /// </summary>
        public static SpaceAgencyFlagController Instance;

        /// <summary>
        /// SpriteRenderer of the flag
        /// </summary>
        private SpriteRenderer _spriteRenderer;
        
        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Warning, multiple instances of SpaceAgencyFlagController detected. Destroying new object to retain singleton behavior");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.enabled = false;
        }

        /// <summary>
        /// Sets the flag to the desired country flag.
        /// </summary>
        /// <param name="flagSprite">Country flag associated with the current character.</param>
        /// <remarks>
        /// Called from <see cref="GameStartSystem.OnUpdate"/> that executes once at the start of the game.
        /// </remarks>
        public void SetFlag(Sprite flagSprite)
        {
            _spriteRenderer.enabled = true;
            _spriteRenderer.sprite = flagSprite;
        }
    }
}