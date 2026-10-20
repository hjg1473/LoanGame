using System;
using UnityEngine;

namespace LoadGame.Audio
{
    public sealed class SoundManager : MonoBehaviour
    {
        private const string MasterVolumeKey = "Sound.MasterVolume";
        private const string SfxVolumeKey = "Sound.SFXVolume";
        private const string BgmVolumeKey = "Sound.BGMVolume";
        private const string MutedKey = "Sound.Muted";

        [Serializable]
        private sealed class SoundClipEntry
        {
            [SerializeField] private SoundId id;
            [SerializeField] private AudioClip clip;

            public SoundId Id => id;
            public AudioClip Clip => clip;
        }

        [Header("Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource ambientSource;

        [Header("Clips")]
        [SerializeField] private SoundClipEntry[] sfxClips;

        [Header("Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float bgmVolume = 1f;
        [SerializeField] private bool muted;

        private float ambientLevel;

        public static SoundManager Instance { get; private set; }

        public float MasterVolume => masterVolume;
        public float SFXVolume => sfxVolume;
        public float BGMVolume => bgmVolume;
        public bool Muted => muted;

        public event Action VolumesChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent != null)
            {
                transform.SetParent(null, true);
            }
            DontDestroyOnLoad(gameObject);
            EnsureAudioSources();
            LoadSettings();
            ApplyVolumes();
        }

        public void PlaySFX(SoundId id)
        {
            if (muted || sfxSource == null)
            {
                return;
            }

            AudioClip clip = FindClip(id);
            if (clip == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip, GetEffectiveSFXVolume());
        }

        public void PlayBGM(AudioClip clip)
        {
            if (bgmSource == null)
            {
                return;
            }

            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.volume = GetEffectiveBGMVolume();

            if (clip != null && !muted)
            {
                bgmSource.Play();
            }
        }

        public void StopBGM()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
        }

        public void ResumeBGM()
        {
            if (bgmSource != null && bgmSource.clip != null && !muted && !bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }

        public bool StartAmbient(SoundId id)
        {
            if (ambientSource == null)
            {
                return false;
            }

            AudioClip clip = FindClip(id);
            if (clip == null)
            {
                return false;
            }

            if (ambientSource.clip != clip)
            {
                ambientSource.Stop();
                ambientSource.clip = clip;
            }

            ambientSource.loop = true;
            ambientSource.volume = GetEffectiveSFXVolume() * ambientLevel;
            if (!muted && !ambientSource.isPlaying)
            {
                ambientSource.Play();
            }

            return true;
        }

        public void SetAmbientLevel(float normalizedLevel)
        {
            ambientLevel = Mathf.Clamp01(normalizedLevel);
            if (ambientSource != null)
            {
                ambientSource.volume = GetEffectiveSFXVolume() * ambientLevel;
            }
        }

        public void StopAmbient()
        {
            ambientLevel = 0f;
            if (ambientSource != null)
            {
                ambientSource.Stop();
                ambientSource.clip = null;
                ambientSource.volume = 0f;
            }
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            SaveFloat(MasterVolumeKey, masterVolume);
            ApplyVolumes();
            VolumesChanged?.Invoke();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            SaveFloat(SfxVolumeKey, sfxVolume);
            ApplyVolumes();
            VolumesChanged?.Invoke();
        }

        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            SaveFloat(BgmVolumeKey, bgmVolume);
            ApplyVolumes();
            VolumesChanged?.Invoke();
        }

        public void SetMuted(bool value)
        {
            muted = value;
            PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyVolumes();
            VolumesChanged?.Invoke();
        }

        public void ToggleMute()
        {
            SetMuted(!muted);
        }

        private void EnsureAudioSources()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }

            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }

            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
            }

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
        }

        private void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, masterVolume);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);
            bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, bgmVolume);
            muted = PlayerPrefs.GetInt(MutedKey, muted ? 1 : 0) == 1;
        }

        private void ApplyVolumes()
        {
            if (sfxSource != null)
            {
                sfxSource.volume = GetEffectiveSFXVolume();
            }

            if (bgmSource != null)
            {
                bgmSource.volume = GetEffectiveBGMVolume();
                if (muted && bgmSource.isPlaying)
                {
                    bgmSource.Pause();
                }
                else if (!muted && bgmSource.clip != null && !bgmSource.isPlaying)
                {
                    bgmSource.UnPause();
                }
            }


            if (ambientSource != null)
            {
                ambientSource.volume = GetEffectiveSFXVolume() * ambientLevel;
                if (muted && ambientSource.isPlaying)
                {
                    ambientSource.Pause();
                }
                else if (!muted && ambientSource.clip != null && !ambientSource.isPlaying)
                {
                    ambientSource.UnPause();
                }
            }
        }

        private AudioClip FindClip(SoundId id)
        {
            if (sfxClips == null)
            {
                return null;
            }

            for (int i = 0; i < sfxClips.Length; i++)
            {
                if (sfxClips[i] != null && sfxClips[i].Id == id)
                {
                    return sfxClips[i].Clip;
                }
            }

            return null;
        }

        private float GetEffectiveSFXVolume()
        {
            return muted ? 0f : masterVolume * sfxVolume;
        }

        private float GetEffectiveBGMVolume()
        {
            return muted ? 0f : masterVolume * bgmVolume;
        }

        private static void SaveFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }
}
