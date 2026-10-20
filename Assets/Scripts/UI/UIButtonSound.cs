using LoadGame.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace LoadGame.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonSound : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private SoundId soundId = SoundId.ButtonClick;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.RemoveListener(PlaySound);
                button.onClick.AddListener(PlaySound);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlaySound);
            }
        }

        private void PlaySound()
        {
            SoundManager.Instance?.PlaySFX(soundId);
        }
    }
}
