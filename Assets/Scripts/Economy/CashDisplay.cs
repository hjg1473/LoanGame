using TMPro;
using UnityEngine;

namespace LoadGame.Economy
{
    public sealed class CashDisplay : MonoBehaviour
    {
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private TMP_Text cashText;
        [SerializeField] private TMP_Text debtText;
        [SerializeField] private TMP_Text productionText;
        [SerializeField] private TMP_Text interestText;
        [SerializeField] private TMP_Text debtRiskStateText;
        [SerializeField] private TMP_Text bankruptcyCountdownText;
        [SerializeField] private TMP_Text recoveredText;
        [SerializeField] private string cashPrefix = "Cash ";
        [SerializeField] private string debtPrefix = "Debt ";
        [SerializeField] private string productionPrefix = "Production/sec +";
        [SerializeField] private string interestPrefix = "Interest/sec +";
        [SerializeField] private string riskPrefix = "Debt Risk ";

        private void Awake()
        {
            if (cashText == null)
            {
                cashText = GetComponent<TMP_Text>();
            }
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
            economyManager.DebtRiskStateChanged += UpdateDebtRiskStateText;
            economyManager.BankruptcyCountdownChanged += UpdateBankruptcyCountdownText;
            economyManager.Recovered += ShowRecoveredText;
            economyManager.Bankrupt += ShowBankruptText;
            UpdateCashText(economyManager.GetCash());
            UpdateDebtText(economyManager.GetDebt());
            UpdateProductionText(economyManager.GetCashPerSecond());
            UpdateInterestText(economyManager.GetDebtInterestPerSecond());
            UpdateDebtRiskStateText(economyManager.GetDebtRiskState());
            UpdateBankruptcyCountdownText(economyManager.GetBankruptcyTimeRemaining(), economyManager.IsBankruptcyCountdownActive());
        }

        private void OnDisable()
        {
            if (economyManager != null)
            {
                economyManager.CashChanged -= UpdateCashText;
                economyManager.DebtChanged -= UpdateDebtText;
                economyManager.ProductionChanged -= UpdateProductionText;
                economyManager.InterestChanged -= UpdateInterestText;
                economyManager.DebtRiskStateChanged -= UpdateDebtRiskStateText;
                economyManager.BankruptcyCountdownChanged -= UpdateBankruptcyCountdownText;
                economyManager.Recovered -= ShowRecoveredText;
                economyManager.Bankrupt -= ShowBankruptText;
            }
        }

        private void UpdateCashText(long cash)
        {
            if (cashText == null)
            {
                Debug.LogError("CashDisplay needs a TMP_Text reference.", this);
                return;
            }

            cashText.text = $"{cashPrefix}{cash:N0}";
        }

        private void UpdateDebtText(long debt)
        {
            if (debtText == null)
            {
                Debug.LogError("CashDisplay needs a debt TMP_Text reference.", this);
                return;
            }

            debtText.text = $"{debtPrefix}{debt:N0}";
        }

        private void UpdateProductionText(double cashPerSecond)
        {
            if (productionText == null)
            {
                Debug.LogError("CashDisplay needs a production TMP_Text reference.", this);
                return;
            }

            productionText.text = $"{productionPrefix}{cashPerSecond:N0}";
        }

        private void UpdateInterestText(double interestPerSecond)
        {
            if (interestText == null)
            {
                Debug.LogError("CashDisplay needs an interest TMP_Text reference.", this);
                return;
            }

            interestText.text = $"{interestPrefix}{interestPerSecond:N0}";
        }

        private void UpdateDebtRiskStateText(DebtRiskState state)
        {
            if (debtRiskStateText == null)
            {
                Debug.LogError("CashDisplay needs a debt risk state TMP_Text reference.", this);
                return;
            }

            debtRiskStateText.text = $"{riskPrefix}{state.ToString().ToUpperInvariant()}";
        }

        private void UpdateBankruptcyCountdownText(float secondsRemaining, bool isActive)
        {
            if (bankruptcyCountdownText == null)
            {
                Debug.LogError("CashDisplay needs a bankruptcy countdown TMP_Text reference.", this);
                return;
            }

            bankruptcyCountdownText.gameObject.SetActive(isActive);
            bankruptcyCountdownText.text = isActive ? $"BANKRUPTCY IN {Mathf.CeilToInt(secondsRemaining)}" : string.Empty;
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

        private void ShowBankruptText()
        {
            if (bankruptcyCountdownText == null)
            {
                return;
            }

            bankruptcyCountdownText.gameObject.SetActive(true);
            bankruptcyCountdownText.text = "BANKRUPT";
        }

        private void HideRecoveredText()
        {
            if (recoveredText != null)
            {
                recoveredText.gameObject.SetActive(false);
            }
        }
    }
}
