using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    public class CleanableSpawner : MonoBehaviour
    {
        [SerializeField] private Vector2 spawnArea = Vector2.one;
        [SerializeField] private float spawnInterval = 30f;
        [SerializeField, Range(0.1f, 1.0f)] private float chanceToSpawn = 0.6f;
        [SerializeField] private float checkRadius = 1f;
        [SerializeField] private List<Cleanable> cleanablePrefabs;

        private IEnumerator Start()
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);

                if (StoreManager.Instance.CanSpawnCleanable && Random.value < chanceToSpawn)
                {
                    var cleanablePrefab = cleanablePrefabs[Random.Range(0, cleanablePrefabs.Count)];

                    Vector3 spawnPos = transform.position + new Vector3(
                        Random.Range(-spawnArea.x / 2f, spawnArea.x / 2f),
                        0f,
                        Random.Range(-spawnArea.y / 2f, spawnArea.y / 2f)
                    );

                    bool isBlocked = Physics.CheckSphere(
                        spawnPos,
                        checkRadius,
                        ~GameConfig.Instance.GroundLayer,
                        QueryTriggerInteraction.Collide
                    );

                    if (!isBlocked)
                    {
                        Instantiate(cleanablePrefab, spawnPos, Quaternion.identity);
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;

            Vector3 center = transform.position;
            Vector3 size = new Vector3(spawnArea.x, 0.1f, spawnArea.y);

            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}
