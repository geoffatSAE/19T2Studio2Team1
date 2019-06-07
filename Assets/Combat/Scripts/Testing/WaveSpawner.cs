using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TO5.Combat
{
    public class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private CombatController m_Player;         // Players controller
 
        [SerializeField] private Target m_TargetPrefab;             // Target prefab to spawn 
        [SerializeField] private Transform[] m_SpawnPoints;         // Spawning points for obstacles
        [SerializeField] private Transform m_DisabledSpot;          // Spot to hide disabled targets
        public int m_MaxTargetsAtOnce = 5;                          // Max amount of targets at once

        private LinkedList<Target> m_TargetPool;                    // Object pool for targets
        private LinkedListNode<Target> m_FirstDisabledTarget;       // Node to first target that is disabled (in pool)

        public int m_TargetsPerRound = 3;                           // Targets to spawn each round
        public float m_TargetsRoundMultiplier = 1.75f;              // Mutliplier to use for targets per round based on current round

        private int m_Round = 0;                // Current round
        private int m_TargetsSpawned = 0;       // Number of targets spawned this round
        private int m_TargetsActive = 0;        // Number of targets currently active

        // World position for hiding targets
        private Vector3 DisabledSpot { get { return m_DisabledSpot ? m_DisabledSpot.position : new Vector3(0f, -1000f, 0f); } }

        void Start()
        {
            m_TargetPool = new LinkedList<Target>();
        }

        private void StartNextRound()
        {

        }

        private void SpawnTarget()
        {
            if (m_TargetsActive < m_MaxTargetsAtOnce)
            {
                Target target = ActivateFirstDisabledTarget();

                int spawnIndex = Random.Range(0, m_SpawnPoints.Length);
                Transform spawnPoint = m_SpawnPoints[spawnIndex];

                target.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            }
        }

        /// <summary>
        /// Activates the first disabled target in the pool. Will spawn a new target if required
        /// </summary>
        /// <returns>New target</returns>
        private Target ActivateFirstDisabledTarget()
        {
            Target target = null;

            if (m_FirstDisabledTarget != null)
            {
                target = m_FirstDisabledTarget.Value;
                m_FirstDisabledTarget = m_FirstDisabledTarget.Next;
            }
            else
            {
                // We need to spawn a new target
                target = Instantiate(m_TargetPrefab, DisabledSpot, Quaternion.identity);
                m_TargetPool.AddLast(target);
            }

            return target;
        }

        private void OnTargetDestroyed(Target target)
        {
            LinkedListNode<Target> node = m_TargetPool.Find(target);
            if (node != null)
            {
                target.transform.SetPositionAndRotation(DisabledSpot, Quaternion.identity);

                if (m_FirstDisabledTarget != null)
                {
                    m_TargetPool.Remove(node);
                    m_TargetPool.AddLast(node);

                }
                else
                {
                    m_FirstDisabledTarget = node;
                }
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            foreach (Transform transform in m_SpawnPoints)
                if (transform)
                    Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
}
