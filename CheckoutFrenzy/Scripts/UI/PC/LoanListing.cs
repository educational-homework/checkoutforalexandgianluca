using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class LoanListing : MonoBehaviour
    {
        [SerializeField, Tooltip("Text element displaying the loan's name.")]
        private TextMeshProUGUI nameText;

        [SerializeField, Tooltip("Text element displaying the principal amount of the loan.")]
        private TextMeshProUGUI principalText;

        [SerializeField, Tooltip("Text element displaying interest, payment schedule, total repayment, and penalties.")]
        private TextMeshProUGUI infoText;

        [SerializeField, Tooltip("Button used to take this loan.")]
        private Button takeButton;

        [SerializeField, Tooltip("UI element shown when the loan is locked (not yet available).")]
        private GameObject locker;

        private LoanTemplate loanTemplate;

        public void Initialize(LoanTemplate template)
        {
            loanTemplate = template;

            nameText.text = template.DisplayName;
            principalText.text = $"${template.Principal:F0}";

            decimal totalToRepay = template.Principal + (template.Principal * template.InterestRate);
            decimal paymentAmount = totalToRepay / template.TotalPayments;
            decimal penaltyAmount = template.LateFeePerInstallment;

            string intervalLabel = template.PaymentInterval == 1 ? "day" : $"{template.PaymentInterval} days";

            infoText.text =
                $"{template.InterestRate * 100m:F0}%\n" +
                $"{template.TotalPayments}\n" +
                $"${paymentAmount:F0} / {intervalLabel}\n" +
                $"${totalToRepay:F0}\n" +
                $"+${penaltyAmount:F0} / late";

            takeButton.onClick.AddListener(() =>
                FinanceManager.Instance.AddLoanFromTemplate(template));

            SetLockerLabel();

            DataManager.Instance.OnLevelUp += TryUnlock;
            TryUnlock(DataManager.Instance.Data.CurrentLevel);
        }

        private void SetLockerLabel()
        {
            if (locker != null)
            {
                var label = locker.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = $"Unlock at Level {loanTemplate.LevelRequirement}";
                }
                else
                {
                    Debug.LogWarning("No TextMeshProUGUI found in locker children.");
                }
            }
            else
            {
                Debug.LogWarning("Locker object is null.");
            }
        }

        private void TryUnlock(int currentLevel)
        {
            bool levelMet = currentLevel >= loanTemplate.LevelRequirement;
            locker.SetActive(!levelMet);
            if (levelMet) DataManager.Instance.OnLevelUp -= TryUnlock;
        }
    }
}
