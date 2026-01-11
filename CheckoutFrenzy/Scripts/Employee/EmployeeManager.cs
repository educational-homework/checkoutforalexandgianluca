using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    public class EmployeeManager : MonoBehaviour
    {
        public static EmployeeManager Instance { get; private set; }

        [SerializeField] private List<Transform> janitorPoints;
        [SerializeField] private List<Transform> sorterPoints;
        [SerializeField] private List<Transform> stockerPoints;

        [SerializeField] private List<Employee> employeePrefabs;

        public event System.Action OnUnpaidEmployeeFired;

        public Dictionary<EmployeeType, List<Employee>> EmployeeLookup { get; private set; }

        private void Awake()
        {
            Instance = this;

            EmployeeLookup = employeePrefabs
                .GroupBy(e => e.Type)
                .ToDictionary(group => group.Key, group => group.ToList());
        }

        private IEnumerator Start()
        {
            StoreManager.Instance.OnDayStarted += HandleDayStarted;

            // Ensure checkout counters are registered before spawning employees,
            // necessary for assigning cashiers to counters.
            yield return new WaitForSeconds(0.1f);

            foreach (var hiredEmployee in DataManager.Instance.Data.HiredEmployees)
            {
                SpawnEmployee(hiredEmployee, GetEmployeePrefab(hiredEmployee));
            }
        }

        private void OnDisable()
        {
            StoreManager.Instance.OnDayStarted -= HandleDayStarted;
        }

        private void HandleDayStarted(int currentDay)
        {
            for (int i = DataManager.Instance.Data.HiredEmployees.Count - 1; i >= 0; i--)
            {
                var hiredEmployee = DataManager.Instance.Data.HiredEmployees[i];

                var employeePrefab = GetEmployeePrefab(hiredEmployee);

                BillTemplate salaryBill = employeePrefab.SalaryBill;

                if (currentDay % salaryBill.FrequencyInDays != 0) continue;

                if (DataManager.Instance.PlayerMoney < salaryBill.Amount)
                {
                    FireEmployee(hiredEmployee);
                    OnUnpaidEmployeeFired?.Invoke();
                }
                else
                {
                    FinanceManager.Instance.CreateBillFromTemplate(salaryBill, currentDay);
                }
            }
        }

        public void HireEmployee(EmployeeData employeeData)
        {
            if (employeeData.Type is EmployeeType.Sorter or EmployeeType.Stocker)
            {
                if (!IsWarehouseUnlocked()) return;
            }

            var employeePrefab = GetEmployeePrefab(employeeData);
            int cost = employeePrefab != null ? employeePrefab.Cost : 0;

            if (!IsFundSufficient(cost)) return;

            if (!SpawnEmployee(employeeData, employeePrefab)) return;

            DataManager.Instance.Data.HiredEmployees.Add(employeeData);
            UIManager.Instance.Message.Log($"{employeeData.Type} hired successfully!");

            DataManager.Instance.PlayerMoney -= cost;
            AudioManager.Instance.PlaySFX(AudioID.Kaching);
        }

        private bool IsWarehouseUnlocked()
        {
            if (!DataManager.Instance.Data.IsWarehouseUnlocked)
            {
                UIManager.Instance.Message.Log("Unlock the warehouse first!", Color.red);
                return false;
            }

            return true;
        }

        private bool IsFundSufficient(int cost)
        {
            if (DataManager.Instance.PlayerMoney < cost)
            {
                UIManager.Instance.Message.Log("You don't have enough money!", Color.red);
                return false;
            }

            return true;
        }

        private bool SpawnEmployee(EmployeeData data, Employee prefab)
        {
            switch (data.Type)
            {
                case EmployeeType.Cashier:
                    var counter = StoreManager.Instance.GetCounterAtIndex(data.PointIndex);
                    if (counter == null)
                    {
                        UIManager.Instance.Message.Log("No counter available for this Cashier!", Color.red);
                        return false;
                    }
                    counter.AssignCashier(Instantiate(prefab as Cashier));
                    return true;

                case EmployeeType.Janitor:
                    return TrySpawnAtPoint(janitorPoints, data, prefab);

                case EmployeeType.Sorter:
                    return TrySpawnAtPoint(sorterPoints, data, prefab);

                case EmployeeType.Stocker:
                    return TrySpawnAtPoint(stockerPoints, data, prefab);

                default:
                    Debug.LogError($"Unknown employee type: {data.Type}");
                    return false;
            }
        }

        private bool TrySpawnAtPoint(List<Transform> points, EmployeeData data, Employee prefab)
        {
            if (data.PointIndex < 0 || data.PointIndex >= points.Count)
            {
                UIManager.Instance.Message.Log($"No point available for this {data.Type}!", Color.red);
                return false;
            }

            var point = points[data.PointIndex];

            if (point == null)
            {
                Debug.LogError($"Spawn point for {data.Type} is null!");
                return false;
            }

            if (point.childCount > 0)
            {
                Debug.LogError($"{data.Type} spot at index {data.PointIndex} is already occupied.");
                return false;
            }

            Instantiate(prefab, point.position, point.rotation, point);
            return true;
        }

        public void FireEmployee(EmployeeData employeeData)
        {
            DataManager.Instance.Data.HiredEmployees.Remove(employeeData);
            UIManager.Instance.Message.Log($"{employeeData.Type} fired!", Color.red);

            switch (employeeData.Type)
            {
                case EmployeeType.Cashier:
                    var occupiedCounter = StoreManager.Instance.GetCounterAtIndex(employeeData.PointIndex);
                    occupiedCounter.AssignCashier(null);
                    break;

                case EmployeeType.Janitor:
                    janitorPoints[employeeData.PointIndex].GetComponentInChildren<Employee>().Dismiss();
                    break;

                case EmployeeType.Sorter:
                    sorterPoints[employeeData.PointIndex].GetComponentInChildren<Employee>().Dismiss();
                    break;

                case EmployeeType.Stocker:
                    stockerPoints[employeeData.PointIndex].GetComponentInChildren<Employee>().Dismiss();
                    break;

                default:
                    break;
            }
        }

        public Employee GetEmployeePrefab(EmployeeData data)
        {
            if (!EmployeeLookup.TryGetValue(data.Type, out var prefabs) || prefabs == null || prefabs.Count == 0)
            {
                Debug.LogError($"No prefabs found for employee type: {data.Type}");
                return null;
            }

            int index = Mathf.Clamp(data.PointIndex, 0, prefabs.Count - 1);
            return prefabs[index];
        }
    }
}
