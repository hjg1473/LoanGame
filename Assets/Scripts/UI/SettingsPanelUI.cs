using LoadGame.Audio;
using LoadGame.Ads;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoadGame.UI
{
    public sealed class SettingsPanelUI : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private TMP_Text bgmVolumePercentText;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Text sfxVolumePercentText;

        [Header("Version")]
        [SerializeField] private TMP_Text versionText;
        [SerializeField] private string versionPrefix = "VERSION ";

        [Header("Privacy")]
        [SerializeField] private Button privacyOptionsButton;

        private SoundManager soundManager;

        private void Start()
        {
            soundManager = SoundManager.Instance;
            ConfigureSlider(bgmVolumeSlider);
            ConfigureSlider(sfxVolumeSlider);

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
                bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            if (soundManager != null)
            {
                soundManager.VolumesChanged += RefreshAudio;
            }

            if (versionText != null)
            {
                versionText.text = $"{versionPrefix}{Application.version}";
            }

            if (privacyOptionsButton != null)
            {
                privacyOptionsButton.onClick.RemoveListener(OpenPrivacyOptions);
                privacyOptionsButton.onClick.AddListener(OpenPrivacyOptions);
            }

            RefreshAudio();
        }

        private void OnDestroy()
        {
            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            }

            if (soundManager != null)
            {
                soundManager.VolumesChanged -= RefreshAudio;
            }

            if (privacyOptionsButton != null)
            {
                privacyOptionsButton.onClick.RemoveListener(OpenPrivacyOptions);
            }
        }

        private static void OpenPrivacyOptions()
        {
            AdManager.Instance?.ShowPrivacyOptions();
        }

        private void OnBgmVolumeChanged(float value)
        {
            soundManager?.SetBGMVolume(value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            soundManager?.SetSFXVolume(value);
        }

        private void RefreshAudio()
        {
            if (soundManager == null)
            {
                SetPercentText(bgmVolumePercentText, 0f);
                SetPercentText(sfxVolumePercentText, 0f);
                return;
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.SetValueWithoutNotify(soundManager.BGMVolume);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(soundManager.SFXVolume);
            }

            SetPercentText(bgmVolumePercentText, soundManager.BGMVolume);
            SetPercentText(sfxVolumePercentText, soundManager.SFXVolume);
        }

        private static void ConfigureSlider(Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
        }

        private static void SetPercentText(TMP_Text label, float normalizedValue)
        {
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(Mathf.Clamp01(normalizedValue) * 100f)}%";
            }
        }
    }
}
