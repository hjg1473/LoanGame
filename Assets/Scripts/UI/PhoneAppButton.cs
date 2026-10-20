using UnityEngine;
using UnityEngine.UI;

namespace LoadGame.UI
{
    public sealed class PhoneAppButton : MonoBehaviour
    {
        [SerializeField] private PhoneAppManager appManager;
        [SerializeField] private Button button;
        [SerializeField] private string appId = "candle_chart";

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (appManager == null)
            {
                appManager = GetComponentInParent<PhoneAppManager>();
            }

            if (button != null)
            {
                button.onClick.RemoveListener(OpenApp);
                button.onClick.AddListener(OpenApp);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OpenApp);
            }
        }

        public void OpenApp()
        {
            if (appManager != null)
            {
                appManager.OpenApp(appId);
            }
        }
    }
}
