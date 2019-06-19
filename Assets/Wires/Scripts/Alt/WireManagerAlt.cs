using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    public class WireManagerAlt : MonoBehaviour
    {
        [Header("Player")]
        public SparkJumperAlt m_JumperPrefab;                   // The spark jumper to spawn
        private SparkJumperAlt m_SparkJumper;                   // The players spark jumper

        [Header("Sparks")]
        public SparkAlt m_SparkPrefab;                                          // The sparks to use
        private ObjectPool<SparkAlt> m_Sparks = new ObjectPool<SparkAlt>();     // Sparks being managed

        [Header("Wires")]
        [SerializeField] private int m_MinSegments = 5;         // Min amount of segments per wire
        [SerializeField] private int m_MaxSegments = 15;        // Max amount of segments per wire
        [SerializeField] private int m_InitialSegments = 10;    // Segments to not start anything
        [SerializeField] WireFactory[] m_Factories;             // Factories for generating wire types
        [SerializeField] WireAnimator m_WireAnimator;           // Animator for the wire

        private ObjectPool<Wire> m_Wires = new ObjectPool<Wire>();              // Wires being managed
        private float m_CachedSegmentDistance;                                  // Distance between the start and end of a segment

        #if UNITY_EDITOR
        [Header("Debug")]
        public bool m_Debug = true;     // If debugging is enabled

        [SerializeField] private bool m_UseDebugMesh = false;   // If debug mesh should be used
        [SerializeField] private Mesh m_DebugMesh;              // Debug mesh drawn instead of animated mesh
        #endif

        void Awake()
        {
            
        }

        void Start()
        {
          
        }

        void Update()
        {

        }

        /// <summary>
        /// Spawns a new spark jumper (only if one hasn't been spawned already)
        /// </summary>
        /// <returns>New spark jumper or null</returns>
        private SparkJumperAlt SpawnSparkJumper()
        {
            if (!m_SparkJumper)
            {
                if (!m_JumperPrefab)
                {
                    Debug.LogError("No jumper prefab has been set", this);
                    return null;
                }

                m_SparkJumper = Instantiate(m_JumperPrefab);
                return m_SparkJumper;
            }

            return null;
        }

        /// <summary>
        /// Get a random wire factory from array of factories
        /// </summary>
        /// <returns>Random factory or null if empty</returns>
        private WireFactory GetRandomWireFactory()
        {
            if (m_Factories != null && m_Factories.Length > 0)
            {
                int index = Random.Range(0, m_Factories.Length);
                return m_Factories[index];
            }

            return null;
        }

        /// <summary>
        /// Calculates the distance for a segment
        /// </summary>
        /// <returns>Distance between segments</returns>
        private float CalculateSegmentDistance()
        {
            #if UNITY_EDITOR
            if (m_Debug && m_UseDebugMesh && m_DebugMesh)
                return m_DebugMesh.bounds.size.z;
            #endif

            if (m_WireAnimator)
            {
                Mesh wireMesh = m_WireAnimator.wireMesh;
                if (wireMesh)
                    return wireMesh.bounds.size.z;
            }

            return 0f;
        }
    }
}
