using UnityEngine;
using System.Collections;
using TMPro;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI controller associated with the damage icons that display above enemy characters when taking damage.
    /// </summary>
    /// <remarks>
    /// Damage text elements are pooled in <see cref="WorldUICanvasController"/>.
    /// </remarks>
    public class DamageNumberUIController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _damageText;

        private const float EFFECT_TIME = 0.75f;

        private readonly Vector3 _movementAmount = new(0f, 0f, 0.25f);
        
        public void DisplayDamageNumber(int damageValue, bool isCriticalHit)
        {
            var criticalHitColor = isCriticalHit ? "<color=red>" : "";
            var colorClosingTag = isCriticalHit ? "</color>" : "";
            _damageText.text = $"{criticalHitColor}{damageValue}{colorClosingTag}";

            StartCoroutine(DamageNumberLifecycle());
        }

        private IEnumerator DamageNumberLifecycle()
        {
            var timer = 0f;
            var startPosition = transform.position;
            var endPosition = startPosition + _movementAmount;
            
            while (timer < EFFECT_TIME)
            {
                var t = timer / EFFECT_TIME;

                transform.position = Vector3.Lerp(startPosition, endPosition, t);
                var textColor = Color.Lerp(Color.white, Color.clear, t);
                _damageText.color = textColor;

                yield return null;
                timer += Time.deltaTime;
            }
            _damageText.text = "";
            WorldUICanvasController.Instance.ReturnDamageNumberToPool(this);
        }
    }
}