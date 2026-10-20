using UnityEngine;
using UnityEngine.UI;

namespace LoadGame.UI
{
    public sealed class MobileTouchTargetController : MonoBehaviour
    {
        [SerializeField, Min(44f)] private float minimumSize = 88f;

        private void Start()
        {
            Apply();
        }

        public void Apply()
        {
            Selectable[] selectables = GetComponentsInChildren<Selectable>(includeInactive: true);
            for (int i = 0; i < selectables.Length; i++)
            {
                Selectable selectable = selectables[i];
                Graphic graphic = selectable != null ? selectable.targetGraphic : null;
                RectTransform rect = selectable != null ? selectable.transform as RectTransform : null;
                if (graphic == null || rect == null)
                {
                    continue;
                }

                float extraX = Mathf.Max(0f, (minimumSize - rect.rect.width) * 0.5f);
                float extraY = Mathf.Max(0f, (minimumSize - rect.rect.height) * 0.5f);
                graphic.raycastPadding = new Vector4(-extraX, -extraY, -extraX, -extraY);
            }
        }
    }
}
