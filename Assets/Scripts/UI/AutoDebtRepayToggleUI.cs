using LoadGame.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace LoadGame.UI
{
    public sealed class AutoDebtRepayToggleUI : MonoBehaviour
    {
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private Toggle toggle;
        [SerializeField] private Image rotatingImage;
        [SerializeField] private float rotationDegreesPerSecond = 180f;

        private RectTransform rotatingImageRect;
        private bool isRotating;
        private float currentRotationZ;

        private void Awake()
        {
            if (toggle == null)
            {
                toggle = GetComponent<Toggle>();
            }

            if (rotatingImage != null)
            {
                rotatingImageRect = rotatingImage.rectTransform;
            }
        }

        private void Start()
        {
            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            if (economyManager != null)
            {
                economyManager.AutoRepayDebtWithIncomeChanged += OnAutoRepayChanged;
            }

            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(SetAutoRepay);
                toggle.onValueChanged.AddListener(SetAutoRepay);

                if (economyManager != null)
                {
                    toggle.SetIsOnWithoutNotify(economyManager.IsAutoRepayDebtWithIncomeEnabled());
                    SetRotationActive(economyManager.IsAutoRepayDebtWithIncomeEnabled());
                }
            }
        }

        private void Update()
        {
            if (!isRotating || rotatingImageRect == null)
            {
                return;
            }

            currentRotationZ -= rotationDegreesPerSecond * Time.deltaTime;
            if (currentRotationZ <= -360f)
            {
                currentRotationZ += 360f;
            }

            rotatingImageRect.localRotation = Quaternion.Euler(0f, 0f, currentRotationZ);
        }

        private void OnDisable()
        {
            if (economyManager != null)
            {
                economyManager.AutoRepayDebtWithIncomeChanged -= OnAutoRepayChanged;
            }

            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(SetAutoRepay);
            }
        }

        private void SetAutoRepay(bool enabled)
        {
            if (economyManager != null)
            {
                economyManager.SetAutoRepayDebtWithIncome(enabled);
            }
        }

        private void OnAutoRepayChanged(bool enabled)
        {
            if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(enabled);
            }

            SetRotationActive(enabled);
        }

        private void SetRotationActive(bool active)
        {
            isRotating = active;
            if (active || rotatingImageRect == null)
            {
                return;
            }

            currentRotationZ = 0f;
            rotatingImageRect.localRotation = Quaternion.identity;
        }
    }
}
