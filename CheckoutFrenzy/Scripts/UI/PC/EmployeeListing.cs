using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class EmployeeListing : MonoBehaviour
    {
        [SerializeField] private Image avatar;
        [SerializeField] private TMP_Text typeLabel;
        [SerializeField] private TMP_Text costLabel;
        [SerializeField] private TMP_Text salaryLabel;
        [SerializeField] private TMP_Text description;
        [SerializeField] private Button hireButton;
        [SerializeField] private TMP_Text hireLabel;
        [SerializeField] private Color hireColor = Color.green;
        [SerializeField] private Color fireColor = Color.red;

        private EmployeeData employeeData;

        public void Initialize(EmployeeData employeeData)
        {
            this.employeeData = employeeData;

            var employee = EmployeeManager.Instance.GetEmployeePrefab(employeeData);

            avatar.sprite = employee.Avatar;
            typeLabel.text = employee.Type.ToString();
            costLabel.text = $"Cost: ${employee.Cost:F0}";

            string interval = employee.SalaryBill.FrequencyInDays > 1
                ? $"/ {employee.SalaryBill.FrequencyInDays} days"
                : "/ day";
            salaryLabel.text = $"Salary: ${employee.SalaryBill.Amount:F0} {interval}";

            description.text = employee.Description;

            EmployeeManager.Instance.OnUnpaidEmployeeFired += UpdateHireButton;
            UpdateHireButton();
        }

        private void UpdateHireButton()
        {
            bool hired = DataManager.Instance.Data.HiredEmployees.Contains(employeeData);

            hireButton.image.color = hired ? fireColor : hireColor;
            hireLabel.text = hired ? "Fire" : "Hire";

            hireButton.onClick.RemoveAllListeners();
            hireButton.onClick.AddListener(() =>
            {
                if (hired) EmployeeManager.Instance.FireEmployee(employeeData);
                else EmployeeManager.Instance.HireEmployee(employeeData);

                UpdateHireButton();
            });
        }
    }
}
