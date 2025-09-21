using UnityEngine;
using System.Collections.Generic;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI controller associated with the world space UI canvas used to display damage numbers above enemies taking damage.
    /// </summary>
    public class WorldUICanvasController : MonoBehaviour
    {
        public static WorldUICanvasController Instance;

        [SerializeField] private GameObject _damageNumberPrefab;
        [SerializeField] private Transform _damageNumberContainer;
        [SerializeField] private int _poolSize;

        private Stack<DamageNumberUIController> _damageNumberPool;
        private readonly Vector3 _poolSpawnPosition = new(0, -1000, 0);
        
        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Warning multiple instances of WorldUICanvasController detected. Destroying new GameObject");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            _damageNumberPool = new Stack<DamageNumberUIController>(_poolSize);
            for (var i = 0; i < _poolSize; i++)
            {
                var newDamageNumber = Instantiate(_damageNumberPrefab, _poolSpawnPosition, Quaternion.identity, _damageNumberContainer);
                var damageNumberUIController = newDamageNumber.GetComponent<DamageNumberUIController>();
                _damageNumberPool.Push(damageNumberUIController);
            }
        }

        public void DisplayDamageNumber(int damageValue, Vector3 startPosition, bool isCriticalHit = false)
        {
            var curDamageNumber = _damageNumberPool.Pop();
            curDamageNumber.transform.position = startPosition;
            curDamageNumber.DisplayDamageNumber(damageValue, isCriticalHit);
        }

        public void ReturnDamageNumberToPool(DamageNumberUIController damageNumberUIController)
        {
            _damageNumberPool.Push(damageNumberUIController);
            damageNumberUIController.transform.position = _poolSpawnPosition;
        }
    }
}