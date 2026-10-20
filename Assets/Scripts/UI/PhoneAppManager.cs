using LoadGame.Audio;
using UnityEngine;

namespace LoadGame.UI
{
    public sealed class PhoneAppManager : MonoBehaviour
    {
        [SerializeField] private PhoneAppPanel[] appPanels;
        [SerializeField] private bool returnHomeWhenPhoneOpens = true;
        [SerializeField] private bool closeAppsInstantlyWhenPhoneCloses = true;

        private PhoneAppPanel activeApp;

        public PhoneAppPanel ActiveApp => activeApp;

        private void Awake()
        {
            if (appPanels == null || appPanels.Length == 0)
            {
                appPanels = GetComponentsInChildren<PhoneAppPanel>(includeInactive: true);
            }

            ShowHomeInstant();
        }

        public void OnPhoneOpened()
        {
            if (returnHomeWhenPhoneOpens)
            {
                ShowHomeInstant();
            }
        }

        public void OnPhoneClosing()
        {
            if (closeAppsInstantlyWhenPhoneCloses)
            {
                ShowHomeInstant();
            }
        }

        public void OpenApp(string appId)
        {
            PhoneAppPanel nextApp = FindApp(appId);
            if (nextApp == null)
            {
                Debug.LogWarning($"Phone app not found: {appId}", this);
                return;
            }

            OpenApp(nextApp);
        }

        public void OpenApp(PhoneAppPanel appPanel)
        {
            if (appPanel == null || appPanel == activeApp)
            {
                return;
            }

            if (activeApp != null)
            {
                activeApp.Close();
            }

            activeApp = appPanel;
            activeApp.Open();
            SoundManager.Instance?.PlaySFX(SoundId.PhoneAppOpen);
        }

        public void CloseCurrentApp()
        {
            TryCloseCurrentApp();
        }

        public bool TryCloseCurrentApp()
        {
            if (activeApp == null)
            {
                return false;
            }

            activeApp.Close();
            activeApp = null;
            SoundManager.Instance?.PlaySFX(SoundId.PhoneAppClose);
            return true;
        }

        public void ShowHomeInstant()
        {
            if (appPanels == null)
            {
                activeApp = null;
                return;
            }

            for (int i = 0; i < appPanels.Length; i++)
            {
                if (appPanels[i] != null)
                {
                    appPanels[i].CloseInstant();
                }
            }

            activeApp = null;
        }

        private PhoneAppPanel FindApp(string appId)
        {
            if (appPanels == null || string.IsNullOrEmpty(appId))
            {
                return null;
            }

            for (int i = 0; i < appPanels.Length; i++)
            {
                if (appPanels[i] != null && appPanels[i].AppId == appId)
                {
                    return appPanels[i];
                }
            }

            return null;
        }
    }
}
