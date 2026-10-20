using UnityEngine;

namespace LoadGame.UI
{
    [DefaultExecutionOrder(-1000)]
    public sealed class MobileDisplayController : MonoBehaviour
    {
        [SerializeField, Range(30, 120)] private int targetFrameRate = 60;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;

#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
#endif
        }
    }
}
