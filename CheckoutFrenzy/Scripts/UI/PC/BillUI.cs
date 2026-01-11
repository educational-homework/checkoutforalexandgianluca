using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class BillUI : MonoBehaviour
    {
        [SerializeField, Tooltip("Displays the type of the bill.")]
        private TextMeshProUGUI typeText;

        [SerializeField, Tooltip("Displays the issue and due day of the bill.")]
        private TextMeshProUGUI dayText;

        [SerializeField, Tooltip("Displays the current status of the bill.")]
        private TextMeshProUGUI statusText;

        [SerializeField, Tooltip("Displays any penalties or warnings about the bill.")]
        private TextMeshProUGUI penaltyText;

        [SerializeField, Tooltip("Displays the amount due for the bill.")]
        private TextMeshProUGUI amountText;

        [SerializeField, Tooltip("Button that triggers the bill payment.")]
        private Button payButton;

        [Header("Status Colors")]
        [SerializeField, Tooltip("Color used when the bill is paid.")]
        private Color paidColor;

        [SerializeField, Tooltip("Color used when the bill is overdue and charged.")]
        private Color overdueColor;

        [SerializeField, Tooltip("Color used when the bill is in its grace period.")]
        private Color graceColor;

        [SerializeField, Tooltip("Color used when the bill is due soon (today or tomorrow).")]
        private Color dueSoonColor;

        [SerializeField, Tooltip("Color used when the bill is unpaid but not urgent.")]
        private Color dueNormalColor;

        public Bill Bill { get; private set; }

        public void Initialize(Bill bill)
        {
            this.Bill = bill;

            typeText.text = bill.Type.ToString();
            dayText.text = $"Day {bill.IssueDay}\nDay {bill.DueDay}";
            amountText.text = $"${bill.Amount:F0}";
            payButton.onClick.AddListener(() =>
            {
                if (FinanceManager.Instance.PayBill(Bill))
                {
                    AudioManager.Instance.PlaySFX(AudioID.Kaching);
                    UpdateUI();
                }
            });

            UpdateUI();
        }

        public void UpdateUI()
        {
            statusText.text = Bill.GetStatusText();
            statusText.color = GetStatusColor();

            if (Bill.IsOverdue())
            {
                decimal totalDue = Bill.GetTotalAmountDue();
                penaltyText.text = $"+ ${Bill.LatePenalty:F0} = <b>${totalDue:F0}</b>";
            }
            else if (Bill.IsInGracePeriod())
            {
                penaltyText.text = $"Pay now or + ${Bill.LatePenalty:F0}";
            }
            else
            {
                penaltyText.text = string.Empty;
            }

            if (!string.IsNullOrEmpty(penaltyText.text))
                penaltyText.color = GetStatusColor();

            payButton.interactable = Bill.Status == BillStatus.Unpaid;
        }

        private Color GetStatusColor()
        {
            var color = new Color();
            int currentDay = DataManager.Instance.Data.TotalDays;

            if (Bill.IsPaid)
            {
                color = paidColor;
            }
            else if (Bill.IsOverdue())
            {
                color = overdueColor;
            }
            else if (Bill.IsInGracePeriod())
            {
                color = graceColor;
            }
            else if (currentDay <= Bill.DueDay)
            {
                if (Bill.DueDay - currentDay <= 1)
                {
                    color = dueSoonColor;
                }
                else
                {
                    color = dueNormalColor;
                }
            }
            else
            {
                color = Color.magenta;
            }

            return color;
        }
    }
}
