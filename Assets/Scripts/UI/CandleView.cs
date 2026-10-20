using UnityEngine;
using UnityEngine.UI;

namespace LoadGame.UI
{
    public sealed class CandleView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform body;
        [SerializeField] private RectTransform wick;
        [SerializeField] private Image bodyImage;
        [SerializeField] private Image wickImage;

        public void Initialize(RectTransform rootTransform, RectTransform bodyTransform, RectTransform wickTransform, Image bodyGraphic, Image wickGraphic)
        {
            root = rootTransform;
            body = bodyTransform;
            wick = wickTransform;
            bodyImage = bodyGraphic;
            wickImage = wickGraphic;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Render(Vector2 anchoredPosition, float width, float bodyCenterY, float bodyHeight, float wickCenterY, float wickHeight, Color color)
        {
            if (root == null || body == null || wick == null)
            {
                return;
            }

            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = new Vector2(width, 0f);

            wick.anchoredPosition = new Vector2(0f, wickCenterY);
            wick.sizeDelta = new Vector2(Mathf.Max(1f, width * 0.18f), Mathf.Max(1f, wickHeight));

            body.anchoredPosition = new Vector2(0f, bodyCenterY);
            body.sizeDelta = new Vector2(width, Mathf.Max(1f, bodyHeight));

            if (bodyImage != null)
            {
                bodyImage.color = color;
            }

            if (wickImage != null)
            {
                wickImage.color = color;
            }
        }
    }
}
