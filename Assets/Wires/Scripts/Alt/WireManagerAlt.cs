using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    public class WireManagerAlt : MonoBehaviour
    {
        public static Vector3 WirePlane = Vector3.forward;

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

        [Header("Generation")]
        [SerializeField] private float m_WireSpawnInterval = 4f;    // Spawn interval for wires
        [SerializeField] private float m_MinWireOffset = 5f;        // Min distance away to spawn wires
        [SerializeField] private float m_MaxWireOffset = 20f;       // max distance away to spawn wires
        [SerializeField] private int m_MaxSegmentOffset = 5;        // Max amount of segments before or after a wire can spawn
        [SerializeField] private int m_MaxSparkSegmentDelay = 3;    // The max amount of segments to travel before a spark will spawn on a new wire

        private ObjectPool<WireAlt> m_Wires = new ObjectPool<WireAlt>();        // Wires being managed
        private float m_CachedSegmentDistance;                                  // Distance between the start and end of a segment

        [Header("Manager")]
        [SerializeField] private bool m_AutoStart = true;       // If game starts automatically
        [SerializeField] private Transform m_DisabledSpot;      // Spot to hide disabled objects

        #if UNITY_EDITOR
        [Header("Debug")]
        public bool m_Debug = true;                             // If debugging is enabled
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
            float step = Time.deltaTime;

            for (int i = 0; i < m_Wires.activeCount; ++i)
            {
                WireAlt wire = m_Wires.GetObject(i);
                wire.TickWire(step);
            }
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
        /// Generates a random wire
        /// </summary>
        /// <returns>Randomly generated wire</returns>
        private WireAlt GenerateRandomWire()
        {
            Vector2 offset = Random.insideUnitCircle * Random.Range(m_MinWireOffset, m_MaxWireOffset);
            int segmentOffset = Random.Range(0, m_MaxSegmentOffset + 1);
            int segments = Random.Range(m_MinSegments, m_MaxSegments + 1);
            int sparkDelay = Random.Range(0, m_MaxSparkSegmentDelay + 1);

            // Random factory (themes)
            WireFactory factory = null;
            if (m_Factories.Length > 0)
                factory = m_Factories[Random.Range(0, m_Factories.Length)];

            return GenerateWire(offset, segmentOffset, segments, sparkDelay, factory);
        }

        private WireAlt GenerateWire(Vector2 offset, int segmentOffset, int segments, int sparkDelay, WireFactory factory)
        {
            WireAlt wire = GetWire();
            if (!wire)
                return null;

            return wire;
        }

        /// <summary>
        /// Helper for getting a wire from the pool (spawns one if needed)
        /// </summary>
        /// <returns>Wire</returns>
        private WireAlt GetWire()
        {
            if (m_Wires.canActivateObject)
                return m_Wires.ActivateObject();

            // New to make a new wire object
            GameObject gameObject = new GameObject("Wire");
            WireAlt wire = gameObject.AddComponent<WireAlt>();

            m_Wires.Add(wire);

            return wire;
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
        /// Helper for getting a spark from the pool (spawns one if needed)
        /// </summary>
        /// <returns>Spark or null</returns>
        private SparkAlt GetSpark()
        {
            if (m_Sparks.canActivateObject)
                return m_Sparks.ActivateObject();   

            if (!m_SparkPrefab)
            {
                Debug.LogError("Unable to spawn spark as prefab is invalid", this);
                return null;
            }

            Vector3 position = m_DisabledSpot ? m_DisabledSpot.position : Vector3.zero;
            SparkAlt spark = Instantiate(m_SparkPrefab, position, Quaternion.identity);

            m_Sparks.Add(spark);

            return spark;
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

        void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            if (m_Debug)
            {
                // Draw Wires
                Gizmos.color = Color.red;
                for (int i = 0; i < m_Wires.activeCount; ++i)
                {
                    WireAlt wire = m_Wires.GetObject(i);
                    wire.DrawDebugGizmos();
                }
            }
            #endif
        }
    }
}
