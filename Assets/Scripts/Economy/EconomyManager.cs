using System;
using UnityEngine;

namespace LoadGame.Economy
{
    public sealed class EconomyManager : MonoBehaviour
    {
        [SerializeField] private long startingCash = 1000L;
        [SerializeField] private long startingDebt = 0L;
        [SerializeField] private double debtInterestRatePerSecond = 0.05d;
        [SerializeField] private float bankruptcyCountdownSeconds = 20f;

        private long cash;
        private long debt;
        private double cashPerSecond;
        private double productionBuffer;
        private double debtInterestBuffer;
        private DebtRiskState debtRiskState = DebtRiskState.Safe;
        private float bankruptcyTimeRemaining;
        private bool bankruptcyCountdownActive;

        public static EconomyManager Instance { get; private set; }
        public event Action<long> CashChanged;
        public event Action<long> DebtChanged;
        public event Action<double> ProductionChanged;
        public event Action<double> InterestChanged;
        public event Action<DebtRiskState> DebtRiskStateChanged;
        public event Action<float, bool> BankruptcyCountdownChanged;
        public event Action Recovered;
        public event Action Bankrupt;
        public event Action<long, long> EconomyChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple EconomyManager instances found. Disabling duplicate.", this);
                enabled = false;
                return;
            }

            Instance = this;
            cash = Math.Max(0L, startingCash);
            debt = Math.Max(0L, startingDebt);
        }

        private void Start()
        {
            NotifyChanged();
            ProductionChanged?.Invoke(cashPerSecond);
            InterestChanged?.Invoke(GetDebtInterestPerSecond());
            DebtRiskStateChanged?.Invoke(debtRiskState);
            BankruptcyCountdownChanged?.Invoke(bankruptcyTimeRemaining, bankruptcyCountdownActive);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            TickProduction(deltaTime);
            TickDebtInterest(deltaTime);
            TickBankruptcyCountdown(deltaTime);
        }

        private void TickProduction(float deltaTime)
        {
            if (cashPerSecond <= 0d)
            {
                return;
            }

            productionBuffer += cashPerSecond * deltaTime;
            if (productionBuffer < 1d)
            {
                return;
            }

            long wholeCash = (long)productionBuffer;
            productionBuffer -= wholeCash;
            AddCash(wholeCash);
        }

        private void TickDebtInterest(float deltaTime)
        {
            if (debt <= 0L || debtInterestRatePerSecond <= 0d)
            {
                return;
            }

            debtInterestBuffer += GetDebtInterestPerSecond() * deltaTime;
            if (debtInterestBuffer < 1d)
            {
                return;
            }

            long wholeDebt = (long)debtInterestBuffer;
            debtInterestBuffer -= wholeDebt;
            AddDebtInternal(wholeDebt);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public long GetCash()
        {
            return cash;
        }

        public long GetDebt()
        {
            return debt;
        }

        public double GetCashPerSecond()
        {
            return cashPerSecond;
        }

        public double GetDebtInterestPerSecond()
        {
            return debt * debtInterestRatePerSecond;
        }

        public DebtRiskState GetDebtRiskState()
        {
            return debtRiskState;
        }

        public float GetBankruptcyTimeRemaining()
        {
            return bankruptcyTimeRemaining;
        }

        public bool IsBankruptcyCountdownActive()
        {
            return bankruptcyCountdownActive;
        }

        public void AddCash(long amount)
        {
            if (amount <= 0L)
            {
                Debug.LogWarning($"AddCash ignored non-positive amount: {amount}", this);
                return;
            }

            cash = SafeAdd(cash, amount);
            NotifyChanged();
        }

        public void AddDebt(long amount)
        {
            if (amount <= 0L)
            {
                Debug.LogWarning($"AddDebt ignored non-positive amount: {amount}", this);
                return;
            }

            debt = SafeAdd(debt, amount);
            NotifyChanged();
        }

        public void RegisterProduction(double amountPerSecond)
        {
            if (amountPerSecond <= 0d)
            {
                Debug.LogWarning($"RegisterProduction ignored non-positive amount: {amountPerSecond}", this);
                return;
            }

            cashPerSecond += amountPerSecond;
            ProductionChanged?.Invoke(cashPerSecond);
            UpdateDebtRisk();
        }

        public void UnregisterProduction(double amountPerSecond)
        {
            if (amountPerSecond <= 0d)
            {
                Debug.LogWarning($"UnregisterProduction ignored non-positive amount: {amountPerSecond}", this);
                return;
            }

            cashPerSecond = Math.Max(0d, cashPerSecond - amountPerSecond);
            ProductionChanged?.Invoke(cashPerSecond);
            UpdateDebtRisk();
        }

        public void Pay(long cost)
        {
            if (cost <= 0L)
            {
                Debug.LogWarning($"Pay ignored non-positive cost: {cost}", this);
                return;
            }

            long paidFromCash = Math.Min(cash, cost);
            cash -= paidFromCash;

            long unpaidCost = cost - paidFromCash;
            if (unpaidCost > 0L)
            {
                AddDebtInternal(unpaidCost, notifyImmediately: false);
            }

            NotifyChanged();
        }

        public long RepayDebt(long amount)
        {
            if (amount <= 0L)
            {
                Debug.LogWarning($"RepayDebt ignored non-positive amount: {amount}", this);
                return 0L;
            }

            long repaidAmount = Math.Min(amount, Math.Min(cash, debt));
            if (repaidAmount <= 0L)
            {
                return 0L;
            }

            cash -= repaidAmount;
            debt -= repaidAmount;
            if (debt <= 0L)
            {
                debt = 0L;
                debtInterestBuffer = 0d;
            }

            NotifyChanged();
            return repaidAmount;
        }

        public void Repay100()
        {
            RepayDebt(100L);
        }

        public void RepayAll()
        {
            RepayDebt(Math.Min(cash, debt));
        }

        public bool TrySpendCash(long amount)
        {
            if (amount <= 0L)
            {
                Debug.LogWarning($"TrySpendCash ignored non-positive amount: {amount}", this);
                return false;
            }

            if (cash < amount)
            {
                return false;
            }

            cash -= amount;
            NotifyChanged();
            return true;
        }

        private void NotifyChanged()
        {
            CashChanged?.Invoke(cash);
            DebtChanged?.Invoke(debt);
            InterestChanged?.Invoke(GetDebtInterestPerSecond());
            EconomyChanged?.Invoke(cash, debt);
            UpdateDebtRisk();
        }

        private void AddDebtInternal(long amount, bool notifyImmediately = true)
        {
            debt = SafeAdd(debt, amount);
            if (notifyImmediately)
            {
                NotifyChanged();
            }
        }

        private void TickBankruptcyCountdown(float deltaTime)
        {
            if (!bankruptcyCountdownActive || debtRiskState == DebtRiskState.Bankrupt)
            {
                return;
            }

            bankruptcyTimeRemaining = Math.Max(0f, bankruptcyTimeRemaining - deltaTime);
            BankruptcyCountdownChanged?.Invoke(bankruptcyTimeRemaining, true);

            if (bankruptcyTimeRemaining > 0f)
            {
                return;
            }

            debtRiskState = DebtRiskState.Bankrupt;
            bankruptcyCountdownActive = false;
            DebtRiskStateChanged?.Invoke(debtRiskState);
            BankruptcyCountdownChanged?.Invoke(0f, false);
            Bankrupt?.Invoke();
            Debug.LogWarning("BANKRUPT: temporary game-over state reached.", this);
        }

        private void UpdateDebtRisk()
        {
            if (debtRiskState == DebtRiskState.Bankrupt)
            {
                return;
            }

            DebtRiskState nextState = CalculateDebtRiskState();
            if (nextState != debtRiskState)
            {
                debtRiskState = nextState;
                DebtRiskStateChanged?.Invoke(debtRiskState);
            }

            if (debtRiskState == DebtRiskState.Insolvent)
            {
                StartBankruptcyCountdown();
                return;
            }

            CancelBankruptcyCountdownIfNeeded();
        }

        private DebtRiskState CalculateDebtRiskState()
        {
            if (debt <= 0L)
            {
                return DebtRiskState.Safe;
            }

            if (cashPerSecond <= 0d)
            {
                return DebtRiskState.Insolvent;
            }

            double debtPaybackSeconds = debt / cashPerSecond;
            if (debtPaybackSeconds <= 30d)
            {
                return DebtRiskState.Safe;
            }

            if (debtPaybackSeconds < 60d)
            {
                return DebtRiskState.Risky;
            }

            if (debtPaybackSeconds < 120d)
            {
                return DebtRiskState.Danger;
            }

            return DebtRiskState.Insolvent;
        }

        private void StartBankruptcyCountdown()
        {
            if (bankruptcyCountdownActive)
            {
                return;
            }

            bankruptcyTimeRemaining = bankruptcyCountdownSeconds;
            bankruptcyCountdownActive = true;
            BankruptcyCountdownChanged?.Invoke(bankruptcyTimeRemaining, true);
        }

        private void CancelBankruptcyCountdownIfNeeded()
        {
            if (!bankruptcyCountdownActive)
            {
                return;
            }

            bankruptcyCountdownActive = false;
            bankruptcyTimeRemaining = 0f;
            BankruptcyCountdownChanged?.Invoke(0f, false);
            Recovered?.Invoke();
        }

        private static long SafeAdd(long current, long amount)
        {
            return long.MaxValue - current < amount ? long.MaxValue : current + amount;
        }
    }
}
