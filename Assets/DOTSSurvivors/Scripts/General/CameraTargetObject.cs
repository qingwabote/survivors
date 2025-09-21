using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Singleton MonoBehaviour to identify this GameObject as the target to use for the main Cinemachine camera.
    /// </summary>
    /// <seealso cref="CameraTarget"/>
    /// <seealso cref="InitializeCameraTargetSystem"/>
    public class CameraTargetObject : MonoBehaviour
    {
        public static CameraTargetObject Instance;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
    }
}