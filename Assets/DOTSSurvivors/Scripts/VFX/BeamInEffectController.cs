using UnityEngine;
using System.Collections;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// MonoBehaviour to control the beam in effect that is played when the player initially spawns into a level.
    /// </summary>
    public class BeamInEffectController : MonoBehaviour
    {
        /// <summary>
        /// Public singleton access to this MonoBehaviour.
        /// </summary>
        public static BeamInEffectController Instance;
        
        /// <summary>
        /// Transform of the base of the beam in effect. Used to rotate this portion of the visual effect.
        /// </summary>
        [SerializeField] private Transform _beamInHit;
        /// <summary>
        /// Transform of the vertical beam of the beam in effect. Used to modify the scale of the visual effect.
        /// </summary>
        [SerializeField] private Transform _beamInBeam;
        /// <summary>
        /// Rotation speed the base of the beam in effect will rotate.
        /// </summary>
        [SerializeField] private float _rotationSpeed;
        /// <summary>
        /// Duration of the first phase of the effect in seconds. 
        /// </summary>
        [SerializeField] private float _effectTime;
        /// <summary>
        /// Duration of the fade out phase of the effect in seconds.
        /// </summary>
        [SerializeField] private float _fadeOutTime;

        /// <summary>
        /// Sprite renderer component of the base of the beam in effect. Used to fade the effect out.
        /// </summary>
        private SpriteRenderer _hitSpriteRenderer;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _hitSpriteRenderer = _beamInHit.GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Method to invoke the coroutine to play the beam in effect. Method called from the <see cref="GameStartSystem"/> once level has loaded.
        /// </summary>
        public void BeginBeamInEffect()
        {
            StartCoroutine(BeamInEffect());
        }
        
        /// <summary>
        /// Coroutine to play the beam in effect.
        /// </summary>
        private IEnumerator BeamInEffect()
        {
            var effectTimer = _effectTime;
            while (effectTimer > 0f)
            {
                _beamInHit.Rotate(new Vector3(0f, 0f, _rotationSpeed * Time.deltaTime));

                var t = Mathf.Max(0f, effectTimer / _effectTime);

                _beamInBeam.localScale = new Vector3(0.5f * t, 0.5f, 0.5f);

                effectTimer -= Time.deltaTime;
                yield return null;
            }

            _beamInBeam.localScale = Vector3.zero;
            effectTimer = _fadeOutTime;

            while (effectTimer > 0f)
            {
                _beamInHit.Rotate(new Vector3(0f, 0f, _rotationSpeed * Time.deltaTime));
                var t = Mathf.Max(0f, effectTimer / _effectTime);
                var curColor = Color.Lerp(Color.clear, Color.white, t);
                _hitSpriteRenderer.color = curColor;
                effectTimer -= Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}