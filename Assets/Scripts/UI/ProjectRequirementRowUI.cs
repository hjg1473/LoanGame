using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoadGame.UI
{
    public sealed class ProjectRequirementRowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Image checkImage;
        [SerializeField] private Color metColor = new Color(0.35f, 1f, 0.55f, 1f);
        [SerializeField] private Color unmetColor = Color.white;

        public void Initialize(TMP_Text requirementName, TMP_Text progress, Image check)
        {
            nameText = requirementName;
            progressText = progress;
            checkImage = check;
        }

        public void Render(string displayName, string progress, bool met)
        {
            Color color = met ? metColor : unmetColor;
            SetText(nameText, displayName);
            SetText(progressText, progress);
            SetTextColor(nameText, color);
            SetTextColor(progressText, color);

            if (checkImage != null)
            {
                checkImage.gameObject.SetActive(met);
                checkImage.color = color;
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static void SetTextColor(TMP_Text target, Color color)
        {
            if (target != null)
            {
                target.color = color;
            }
        }
    }
}
