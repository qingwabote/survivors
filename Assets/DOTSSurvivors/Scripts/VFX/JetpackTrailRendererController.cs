using UnityEngine;
using System.Collections;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// MonoBehaviour component attached to companion trail renderers of jetpack attack entities. Provides some nice visual effects to fade the trail renderer in and out.
    /// </summary>
    public class JetpackTrailRendererController : MonoBehaviour
    {
        private TrailRenderer _trailRenderer;
        private float _startWidth;
        private Gradient _targetGradient;
        private float _timeAlive;
        private Coroutine _startCoroutine;
        
        private void Awake()
        {
            _trailRenderer = GetComponent<TrailRenderer>();
            _startWidth = _trailRenderer.startWidth;
            _targetGradient = _trailRenderer.colorGradient;

            var transparentAlphaKeys = _targetGradient.alphaKeys;
            for (var i = 0; i < transparentAlphaKeys.Length; i++)
            {
                var alphaKey = transparentAlphaKeys[i];
                alphaKey.alpha = 0f;
                transparentAlphaKeys[i] = alphaKey;
            }

            var transparentGradient = new Gradient();
            transparentGradient.SetKeys(_targetGradient.colorKeys, transparentAlphaKeys);
            _trailRenderer.colorGradient = transparentGradient;
        }

        private void Start()
        {
            _startCoroutine = StartCoroutine(StartTrailRendererRoutine());
        }

        private void Update()
        {
            _timeAlive += Time.deltaTime;
        }

        private IEnumerator StartTrailRendererRoutine()
        {
            var lineTime = _trailRenderer.time;
            while (lineTime > 0f)
            {
                var t = (_trailRenderer.time - lineTime) / _trailRenderer.time;
                FadeTrailRenderer(t);

                yield return null;
                lineTime -= Time.deltaTime;
            }
            _trailRenderer.colorGradient = _targetGradient;
        }

        public void EndTrailRenderer()
        {
            if (_startCoroutine != null)
            {
                StopCoroutine(_startCoroutine);
            }
            StartCoroutine(EndTrailRendererRoutine());
        }

        private IEnumerator EndTrailRendererRoutine()
        {
            var lineTime = Mathf.Min(_trailRenderer.time, _timeAlive);
            while (lineTime > 0f)
            {
                var t = lineTime / _trailRenderer.time;
                FadeTrailRenderer(t);
                yield return null;
                lineTime -= Time.deltaTime;
            }
        }

        private void FadeTrailRenderer(float t)
        {
            _trailRenderer.widthMultiplier = t * _startWidth;

            var curAlphaKeys = _targetGradient.alphaKeys;
            for (var i = 0; i < curAlphaKeys.Length; i++)
            {
                curAlphaKeys[i].alpha = Mathf.Lerp(0f, _targetGradient.alphaKeys[i].alpha, t);
            }

            var tempGradient = new Gradient();
            tempGradient.SetKeys(_targetGradient.colorKeys, curAlphaKeys);
            _trailRenderer.colorGradient = tempGradient;
        }
    }
}