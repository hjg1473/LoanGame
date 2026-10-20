using TMPro;
using UnityEngine;

namespace LoadGame.UI
{
    public sealed class TMPAnimatedRainbowText : MonoBehaviour
    {
        [SerializeField] private TMP_Text targetText;
        [SerializeField, Min(0f)] private float speed = 0.2f;
        [SerializeField, Min(0.01f)] private float characterDensity = 0.12f;
        [SerializeField, Range(0f, 1f)] private float saturation = 0.9f;
        [SerializeField, Range(0f, 1f)] private float brightness = 1f;

        private void Awake()
        {
            if (targetText == null)
            {
                targetText = GetComponent<TMP_Text>();
            }
        }

        private void LateUpdate()
        {
            if (targetText == null)
            {
                return;
            }

            targetText.ForceMeshUpdate();
            TMP_TextInfo textInfo = targetText.textInfo;
            float timeOffset = Time.unscaledTime * speed;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
                if (!characterInfo.isVisible)
                {
                    continue;
                }

                float hue = Mathf.Repeat(i * characterDensity - timeOffset, 1f);
                Color color = Color.HSVToRGB(hue, saturation, brightness);
                Color32 vertexColor = color;
                int materialIndex = characterInfo.materialReferenceIndex;
                int vertexIndex = characterInfo.vertexIndex;
                Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

                colors[vertexIndex] = vertexColor;
                colors[vertexIndex + 1] = vertexColor;
                colors[vertexIndex + 2] = vertexColor;
                colors[vertexIndex + 3] = vertexColor;
            }

            targetText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
    }
}
