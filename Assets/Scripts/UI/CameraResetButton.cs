using LoadGame.Audio;
using UnityEngine;

namespace LoadGame.UI
{
    public sealed class CameraResetButton : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        public void ResetCameraPosition()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    return;
                }
            }

            Vector3 position = targetCamera.transform.position;
            position.x = 0f;
            position.y = 0f;
            targetCamera.transform.position = position;
            SoundManager.Instance?.PlaySFX(SoundId.ButtonClick);
        }
    }
}
