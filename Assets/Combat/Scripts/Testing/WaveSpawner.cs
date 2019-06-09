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
        [SerializeField] private TargetAnchor[] m_Anchors;          // Spawning points for obstacles
        [SerializeField] private Transform m_DisabledSpot;          // Spot to hide disabled targets
        public int m_MaxTargetsAtOnce = 5;                          // Max amount of targets at once

        private LinkedList<Target> m_TargetPool;                    // Object pool for targets
        private LinkedListNode<Target> m_FirstDisabledTarget;       // Node to first target that is disabled (in pool)

        // The amount of targets left in current round
        public int targetsRemaining { get { return m_RoundActive ? m_TargetsToSpawn - m_TargetsSpawned + m_TargetsActive : 0; } }

        public int m_TargetsPerRound = 3;                           // Targets to spawn each round
        public float m_TargetsRoundMultiplier = 1.75f;              // Mutliplier to use for targets per round based on current round
        public float m_HealthBonus = 20f;                           // Health to restore each round to the player

        private int m_Round = 0;                // Current round
        private bool m_RoundActive = false;     // If round is active
        private int m_TargetsToSpawn = 0;       // Number of targets to spawn this round
        private int m_TargetsSpawned = 0;       // Number of targets spawned this round
        private int m_TargetsActive = 0;        // Number of targets currently active

        // World position for hiding targets
        private Vector3 disabledSpot { get { return m_DisabledSpot ? m_DisabledSpot.position : new Vector3(0f, -1000f, 0f); } }

        [SerializeField] private Text m_RoundText;       // Text for displaying round
        [SerializeField] private Text m_TargetText;      // Text for displaying target count

        void Start()
        {
            m_TargetPool = new LinkedList<Target>();
            StartNextRound();
        }

        /// <summary>
        /// Starts the next round
        /// </summary>
        private void StartNextRound()
        {
            if (m_RoundActive)
                return;

            if (m_Player)
                m_Player.RestoreHealth(m_HealthBonus);

            ++m_Round;
            m_TargetsToSpawn = Mathf.FloorToInt(m_TargetsPerRound * m_Round * m_TargetsRoundMultiplier);
            m_TargetsSpawned = 0;
            m_TargetsActive = 0;

            m_RoundActive = true;
            InvokeRepeating("SpawnTarget", 1f, 1f);

            if (m_RoundText)
                m_RoundText.text = string.Format("Round: {0}", m_Round);

            if (m_TargetText)
                m_TargetText.text = string.Format("Targets: {0}", m_TargetsToSpawn + m_TargetsActive);
        }

        /// <summary>
        /// Finishes the current round
        /// </summary>
        private void FinishCurrentRound()
        {
            CancelInvoke("SpawnTarget");
            m_RoundActive = false;

            if (m_RoundText)
                m_RoundText.text = string.Format("Round {0} Complete", m_Round);

            Invoke("StartNextRound", 5f);
        }

        /// <summary>
        /// Spawns a target
        /// </summary>
        private void SpawnTarget()
        {
            if (m_TargetsActive < m_MaxTargetsAtOnce &&
                m_TargetsSpawned < m_TargetsToSpawn)
            {
                // We need an anchor to attach to
                TargetAnchor anchor = FindRandomEmptyAnchor();
                if (!anchor)
                    return;

                Target target = ActivateFirstDisabledTarget();
                if (!target)
                    return;

                target.OnTargetDestroyed += OnTargetDestroyed;
                target.ActivateTarget(m_Player.transform, anchor);

                ++m_TargetsActive;
                ++m_TargetsSpawned;
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
                target = Instantiate(m_TargetPrefab, disabledSpot, Quaternion.identity);
                m_TargetPool.AddLast(target);
            }

            return target;
        }
        
        /// <summary>
        /// Finds a random anchor with no target attached to it
        /// </summary>
        /// <returns>Anchor if one found, null if not</returns>
        private TargetAnchor FindRandomEmptyAnchor()
        {
            // TODO:

            if (m_Anchors.Length > 0)
            {
                int i = Random.Range(0, m_Anchors.Length);
                if (!m_Anchors[i].hasTarget)
                    return m_Anchors[i];
            }

            return null;
        }

        /// <summary>
        /// Notify that a target has been destroyed
        /// </summary>
        /// <param name="target">The target that was destroyed</param>
        private void OnTargetDestroyed(Target target)
        {
            LinkedListNode<Target> node = m_TargetPool.Find(target);
            if (node != null)
            {
                target.DeacativateTarget();
                target.OnTargetDestroyed -= OnTargetDestroyed;
                target.transform.SetPositionAndRotation(disabledSpot, Quaternion.identity);

                if (m_FirstDisabledTarget != null)
                {
                    m_TargetPool.Remove(node);
                    m_TargetPool.AddLast(node);
                }
                else
                {
                    m_FirstDisabledTarget = node;
                } 

                // Was last target just destroyed?
                --m_TargetsActive;
                if (m_TargetsActive <= 0 &&
                    m_TargetsSpawned >= m_TargetsToSpawn)
                {
                    FinishCurrentRound();
                }

                if (m_TargetText)
                    m_TargetText.text = string.Format("Targets: {0}", targetsRemaining);
            }
        }

        void OnDrawGizmos()
        {
            if (m_Anchors != null)
            {
                Gizmos.color = Color.red;
                foreach (TargetAnchor anchor in m_Anchors)
                    if (anchor)
                        Gizmos.DrawWireSphere(anchor.transform.position, 0.1f);
            }
        }
    }
}
