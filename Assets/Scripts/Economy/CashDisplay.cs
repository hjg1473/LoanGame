using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LoadGame.UI;

namespace LoadGame.Economy
{
    public sealed class CashDisplay : MonoBehaviour
    {
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private CityValueManager cityValueManager;
        [SerializeField] private NetWorthManager netWorthManager;
        [SerializeField] private PopulationManager populationManager;
        [SerializeField] private TMP_Text cashText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text netWorthText;
        [SerializeField] private TMP_Text debtText;
        [SerializeField] private TMP_Text productionText;
        [SerializeField] private TMP_Text interestText;
        [SerializeField] private TMP_Text maintenanceText;
        [SerializeField] private TMP_Text netIncomeText;
        [SerializeField] private TMP_Text rateText;
        [SerializeField] private TMP_Text debtRiskStateText;
        [SerializeField] private TMP_Text bankruptcyCountdownText;
        [SerializeField] private TMP_Text recoveredText;
        [Header("Population")]
        [SerializeField] private TMP_Text populationText;
        [SerializeField] private TMP_Text employmentText;
        [SerializeField] private TMP_Text happinessText;
        [SerializeField] private string populationPrefix = "POP ";
        [SerializeField] private string employmentPrefix = "JOBS ";
        [SerializeField] private string happinessPrefix = "HAPPY ";
        [SerializeField] private Button foldToggleButton;
        [SerializeField] private TMP_Text foldToggleButtonText;
        [SerializeField] private bool startExpanded;
        [SerializeField] private string collapsedButtonText = "▼";
        [SerializeField] private string expandedButtonText = "▲";
        [SerializeField] private RectTransform foldRoot;
        [SerializeField] private float collapsedHeight = 300f;
        [SerializeField] private float expandedHeight = 600f;
        [SerializeField] private float foldTweenDuration = 0.22f;
        [SerializeField] private Ease foldEase = Ease.OutCubic;
        [SerializeField] private string cashPrefix = "Cash ";
        [SerializeField] private string debtPrefix = "Debt ";
        [SerializeField] private string productionPrefix = "+$";
        [SerializeField] private string interestPrefix = "Interest -$";
        [SerializeField] private string maintenancePrefix = "Maintenance -$";
        [SerializeField] private string netIncomePrefix = "Net ";
        [SerializeField] private Color positiveNetIncomeColor = Color.white;
        [SerializeField] private Color negativeNetIncomeColor = Color.red;
        [SerializeField] private string ratePrefix = "Rate ";
        [SerializeField] private string riskPrefix = "Debt Risk ";
        [SerializeField] private Color safeRiskColor = new Color(0.25f, 1f, 0.45f, 1f);
        [SerializeField] private Color dangerRiskColor = new Color(1f, 0.18f, 0.12f, 1f);
        [SerializeField] private Color bankruptRiskColor = Color.red;
        [Header("Bankruptcy Countdown")]
        [SerializeField] private Color oddSecondCountdownColor = Color.red;
        [SerializeField] private Color evenSecondCountdownColor = Color.white;

        private bool isExpanded;
        private Tween foldTween;

        private void Awake()
        {
            if (interestPrefix == "Interest +")
            {
                interestPrefix = "Interest -$";
            }

            if (cashText == null)
            {
                cashText = GetComponent<TMP_Text>();
            }

            if (foldRoot == null)
            {
                foldRoot = transform as RectTransform;
            }

            isExpanded = startExpanded;
            ApplyFoldState(animate: false);
        }

        private void OnEnable()
        {
            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            if (economyManager == null)
            {
                Debug.LogError("CashDisplay needs an EconomyManager reference.", this);
                return;
            }

            economyManager.CashChanged += UpdateCashText;
            economyManager.DebtChanged += UpdateDebtText;
            economyManager.ProductionChanged += UpdateProductionText;
            economyManager.InterestChanged += UpdateInterestText;
            economyManager.MaintenanceChanged += UpdateMaintenanceText;
            economyManager.NetIncomeChanged += UpdateNetIncomeText;
            economyManager.RateChanged += UpdateRateText;
            economyManager.DebtRiskStateChanged += UpdateDebtRiskStateText;
            economyManager.BankruptcyCountdownChanged += UpdateBankruptcyCountdownText;
            economyManager.Recovered += ShowRecoveredText;
            economyManager.Bankrupt += ShowBankruptText;

            if (cityValueManager == null)
            {
                cityValueManager = FindFirstObjectByType<CityValueManager>();
            }

            if (cityValueManager != null)
            {
                cityValueManager.CityValueChanged += UpdateValueText;
                UpdateValueText(cityValueManager.GetCityValue());
            }

            if (netWorthManager == null)
            {
                netWorthManager = FindFirstObjectByType<NetWorthManager>();
            }

            if (netWorthManager != null)
            {
                netWorthManager.NetWorthChanged += UpdateNetWorthText;
                UpdateNetWorthText(netWorthManager.GetNetWorth());
            }

            BindPopulationManager(populationManager != null
                ? populationManager
                : FindFirstObjectByType<PopulationManager>());

            UpdateCashText(economyManager.GetCash());
            UpdateDebtText(economyManager.GetDebt());
            UpdateProductionText(economyManager.GetCashPerSecond());
            UpdateInterestText(economyManager.GetDebtInterestPerSecond());
            UpdateMaintenanceText(economyManager.GetMaintenancePerSecond());
            UpdateNetIncomeText(economyManager.GetNetIncomePerSecond());
            UpdateRateText(economyManager.GetCurrentInterestRate());
            UpdateDebtRiskStateText(economyManager.GetDebtRiskState());
            UpdateBankruptcyCountdownText(economyManager.GetBankruptcyTimeRemaining(), economyManager.IsBankruptcyCountdownActive());

            if (foldToggleButton != null)
            {
                foldToggleButton.onClick.RemoveListener(ToggleFold);
                foldToggleButton.onClick.AddListener(ToggleFold);
            }

            ApplyFoldState(animate: false);
        }

        private void OnDisable()
        {
            foldTween?.Kill();
            foldTween = null;

            if (foldToggleButton != null)
            {
                foldToggleButton.onClick.RemoveListener(ToggleFold);
            }

            if (economyManager != null)
            {
                economyManager.CashChanged -= UpdateCashText;
                economyManager.DebtChanged -= UpdateDebtText;
                economyManager.ProductionChanged -= UpdateProductionText;
                economyManager.InterestChanged -= UpdateInterestText;
                economyManager.MaintenanceChanged -= UpdateMaintenanceText;
                economyManager.NetIncomeChanged -= UpdateNetIncomeText;
                economyManager.RateChanged -= UpdateRateText;
                economyManager.DebtRiskStateChanged -= UpdateDebtRiskStateText;
                economyManager.BankruptcyCountdownChanged -= UpdateBankruptcyCountdownText;
                economyManager.Recovered -= ShowRecoveredText;
                economyManager.Bankrupt -= ShowBankruptText;
            }

            if (cityValueManager != null)
            {
                cityValueManager.CityValueChanged -= UpdateValueText;
            }

            if (netWorthManager != null)
            {
                netWorthManager.NetWorthChanged -= UpdateNetWorthText;
            }

            UnbindPopulationManager();
        }

        public void ToggleFold()
        {
            SetExpanded(!isExpanded);
        }

        public void SetExpanded(bool expanded)
        {
            if (isExpanded == expanded)
            {
                return;
            }

            isExpanded = expanded;
            ApplyFoldState(animate: true);
        }

        public void SetValueManagers(CityValueManager cityValue, NetWorthManager netWorth)
        {
            if (cityValueManager != null)
            {
                cityValueManager.CityValueChanged -= UpdateValueText;
            }

            if (netWorthManager != null)
            {
                netWorthManager.NetWorthChanged -= UpdateNetWorthText;
            }

            cityValueManager = cityValue;
            netWorthManager = netWorth;

            if (cityValueManager != null)
            {
                cityValueManager.CityValueChanged += UpdateValueText;
                UpdateValueText(cityValueManager.GetCityValue());
            }

            if (netWorthManager != null)
            {
                netWorthManager.NetWorthChanged += UpdateNetWorthText;
                UpdateNetWorthText(netWorthManager.GetNetWorth());
            }
        }

        public void SetPopulationManager(PopulationManager manager)
        {
            BindPopulationManager(manager);
        }

        private void BindPopulationManager(PopulationManager manager)
        {
            UnbindPopulationManager();
            populationManager = manager;
            if (populationManager == null || !isActiveAndEnabled)
            {
                return;
            }

            populationManager.PopulationChanged += UpdatePopulationText;
            populationManager.EmploymentChanged += UpdateEmploymentText;
            populationManager.HappinessChanged += UpdateHappinessText;
            UpdatePopulationText(populationManager.Population, populationManager.PopulationCapacity);
            UpdateEmploymentText(
                populationManager.EmployedPopulation,
                populationManager.TotalRequiredWorkers,
                populationManager.EmploymentEfficiency);
            UpdateHappinessText(populationManager.Happiness);
        }

        private void UnbindPopulationManager()
        {
            if (populationManager == null)
            {
                return;
            }

            populationManager.PopulationChanged -= UpdatePopulationText;
            populationManager.EmploymentChanged -= UpdateEmploymentText;
            populationManager.HappinessChanged -= UpdateHappinessText;
        }

        private void UpdatePopulationText(double current, long capacity)
        {
            if (populationText != null)
            {
                populationText.text = $"{populationPrefix}{Mathf.FloorToInt((float)current):N0}/{capacity:N0}";
            }
        }

        private void UpdateEmploymentText(double employed, long required, double efficiency)
        {
            if (employmentText != null)
            {
                employmentText.text = $"{employmentPrefix}{Mathf.FloorToInt((float)employed):N0}/{required:N0}";
            }
        }

        private void UpdateHappinessText(float value)
        {
            if (happinessText != null)
            {
                happinessText.text = $"{happinessPrefix}{value:N0}%";
            }
        }

        private void UpdateCashText(long cash)
        {
            if (cashText == null)
            {
                Debug.LogError("CashDisplay needs a TMP_Text reference.", this);
                return;
            }

            cashText.text = $"{cashPrefix}${CompactNumberFormatter.Format(cash)}";
        }

        private void UpdateValueText(double value)
        {
            if (valueText == null)
            {
                return;
            }

            valueText.text = $"${CompactNumberFormatter.Format(value)}";
        }

        private void UpdateNetWorthText(double value)
        {
            if (netWorthText == null)
            {
                return;
            }

            netWorthText.text = $"${CompactNumberFormatter.Format(value)}";
        }

        private void UpdateDebtText(long debt)
        {
            if (debtText == null)
            {
                Debug.LogError("CashDisplay needs a debt TMP_Text reference.", this);
                return;
            }

            debtText.text = $"{debtPrefix}${CompactNumberFormatter.Format(debt)}";
        }

        private void UpdateProductionText(double cashPerSecond)
        {
            if (productionText == null)
            {
                Debug.LogError("CashDisplay needs a production TMP_Text reference.", this);
                return;
            }

            productionText.text = cashPerSecond > 0d ? $"{productionPrefix}{cashPerSecond:N0}/s" : string.Empty;
        }

        private void UpdateInterestText(double interestPerSecond)
        {
            if (interestText == null)
            {
                Debug.LogError("CashDisplay needs an interest TMP_Text reference.", this);
                return;
            }

            interestText.text = interestPerSecond > 0d ? $"{interestPrefix}{interestPerSecond:N0}/s" : string.Empty;
        }

        private void UpdateMaintenanceText(double maintenancePerSecond)
        {
            if (maintenanceText != null)
            {
                maintenanceText.text = maintenancePerSecond > 0d
                    ? $"{maintenancePrefix}{maintenancePerSecond:N0}/s"
                    : string.Empty;
            }
        }

        private void UpdateNetIncomeText(double netIncomePerSecond)
        {
            if (netIncomeText == null)
            {
                return;
            }

            string sign = netIncomePerSecond < 0d ? "-$" : "+$";
            netIncomeText.text = $"{netIncomePrefix}{sign}{Math.Abs(netIncomePerSecond):N0}/s";
            netIncomeText.color = netIncomePerSecond < 0d
                ? negativeNetIncomeColor
                : positiveNetIncomeColor;
        }

        private void UpdateRateText(double currentRate)
        {
            if (rateText == null)
            {
                Debug.LogError("CashDisplay needs a rate TMP_Text reference.", this);
                return;
            }

            rateText.text = $"{ratePrefix}{currentRate:P2}";
        }

        private void UpdateDebtRiskStateText(DebtRiskState state)
        {
            if (debtRiskStateText == null)
            {
                Debug.LogError("CashDisplay needs a debt risk state TMP_Text reference.", this);
                return;
            }

            debtRiskStateText.text = $"{riskPrefix}{state.ToString().ToUpperInvariant()}";
            debtRiskStateText.color = GetRiskColor(state);
        }

        private Color GetRiskColor(DebtRiskState state)
        {
            if (state == DebtRiskState.Bankrupt)
            {
                return bankruptRiskColor;
            }

            float t;
            switch (state)
            {
                case DebtRiskState.Risky:
                    t = 0.33f;
                    break;
                case DebtRiskState.Danger:
                    t = 0.66f;
                    break;
                case DebtRiskState.Insolvent:
                    t = 1f;
                    break;
                default:
                    t = 0f;
                    break;
            }

            return Color.Lerp(safeRiskColor, dangerRiskColor, t);
        }

        private void UpdateBankruptcyCountdownText(float secondsRemaining, bool isActive)
        {
            if (bankruptcyCountdownText == null)
            {
                Debug.LogError("CashDisplay needs a bankruptcy countdown TMP_Text reference.", this);
                return;
            }

            bankruptcyCountdownText.gameObject.SetActive(isActive);
            if (!isActive)
            {
                bankruptcyCountdownText.text = string.Empty;
                return;
            }

            int displayedSeconds = Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));
            bankruptcyCountdownText.text = $"BANKRUPTCY IN {displayedSeconds}";
            bankruptcyCountdownText.color = displayedSeconds % 2 == 0
                ? evenSecondCountdownColor
                : oddSecondCountdownColor;
        }

        private void ShowRecoveredText()
        {
            if (recoveredText == null)
            {
                return;
            }

            recoveredText.gameObject.SetActive(true);
            recoveredText.text = "RECOVERED!";
            CancelInvoke(nameof(HideRecoveredText));
            Invoke(nameof(HideRecoveredText), 1.2f);
        }

        private void ShowBankruptText(EconomyGameOverStats stats)
        {
            if (bankruptcyCountdownText == null)
            {
                return;
            }

            bankruptcyCountdownText.gameObject.SetActive(true);
            bankruptcyCountdownText.text = "BANKRUPT";
            bankruptcyCountdownText.color = bankruptRiskColor;
        }

        private void HideRecoveredText()
        {
            if (recoveredText != null)
            {
                recoveredText.gameObject.SetActive(false);
            }
        }

        private void ApplyFoldState(bool animate)
        {
            SetText(foldToggleButtonText, isExpanded ? expandedButtonText : collapsedButtonText);

            if (foldRoot == null)
            {
                return;
            }

            float targetHeight = isExpanded ? expandedHeight : collapsedHeight;
            Vector2 size = foldRoot.sizeDelta;

            foldTween?.Kill();
            if (!animate)
            {
                foldRoot.sizeDelta = new Vector2(size.x, targetHeight);
                return;
            }

            foldTween = foldRoot
                .DOSizeDelta(new Vector2(size.x, targetHeight), Mathf.Max(0f, foldTweenDuration))
                .SetEase(foldEase);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
