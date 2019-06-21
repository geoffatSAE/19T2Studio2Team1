using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Yield instruction for waiting until player reaches desired segment
    /// </summary>
    public class WaitForSegment : CustomYieldInstruction
    {
        public override bool keepWaiting { get { return m_WireManager.GetJumpersSegment() >= m_Segment; } }

        private WireManagerAlt m_WireManager;
        private int m_Segment;

        public WaitForSegment(WireManagerAlt wireManager, int segment)
        {
            m_WireManager = wireManager;
            m_Segment = segment;
        }
    }

    public class WireManagerAlt : MonoBehaviour
    {
        public static Vector3 WirePlane = Vector3.forward;

        [Header("Player")]
        public SparkJumperAlt m_JumperPrefab;                   // The spark jumper to spawn
        private SparkJumperAlt m_SparkJumper;                   // The players spark jumper

        [Header("Sparks")]
        public SparkAlt m_SparkPrefab;                  // The sparks to use
        public float m_SparkSwitchInterval = 2f;        // Interval for sparks switching between on and off

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

        private ObjectPool<WireAlt> m_Wires = new ObjectPool<WireAlt>();            // Wires being managed
        private float m_CachedSegmentDistance;                                      // Distance between the start and end of a segment

        [Header("Manager")]
        [SerializeField] private bool m_AutoStart = true;       // If game starts automatically
        [SerializeField] private Transform m_DisabledSpot;      // Spot to hide disabled objects

        #if UNITY_EDITOR
        [Header("Debug")]
        public bool m_Debug = true;                             // If debugging is enabled
        [SerializeField] private bool m_UseDebugMesh = false;   // If debug mesh should be used
        [SerializeField] private Mesh m_DebugMesh;              // Debug mesh drawn instead of animated mesh
        #endif

        void Start()
        {
            if (m_AutoStart)
            {
                m_CachedSegmentDistance = 1f;

                SparkJumperAlt sparkJumper = SpawnSparkJumper();

                WireAlt wire = GenerateWire(transform.position, m_InitialSegments, 0, null);
                sparkJumper.JumpToSpark(wire.spark, true);

                InvokeRepeating("GenerateRandomWire", m_WireSpawnInterval, m_WireSpawnInterval);
            }
        }

        void Update()
        {
            float step = Time.deltaTime;

            for (int i = 0; i < m_Wires.activeCount; ++i)
            {
                WireAlt wire = m_Wires.GetObject(i);

                float progress = wire.TickWire(step);
                if (progress >= 1f)
                {
                    if (wire.spark && wire.spark.sparkJumper != null)
                    {
                        // TODO:
                    }

                    SparkAlt spark = wire.spark;

                    wire.DeactivateWire();

                    m_Sparks.DeactivateObject(spark);
                    m_Wires.DeactivateObject(wire);

                    // Object pool swaps when deactivating objects, we need
                    // to update the swapped object since it is still active
                    --i;
                }
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
        /// <returns>Randomly generated wire or null</returns>
        private WireAlt GenerateRandomWire()
        {
            const int maxAttempts = 5;

            Vector3 start = transform.position;
            bool success = true;

            // We don't want to loop too many times
            int attempts = 0;
            while (++attempts <= maxAttempts)
            {
                Vector2 circleOffset = Random.insideUnitCircle.normalized * Random.Range(m_MinWireOffset, m_MaxWireOffset);
                int segmentOffset = Random.Range(-m_MaxSegmentOffset, m_MaxSegmentOffset + 1);

                start = GetSpawnCircleCenter() + new Vector3(circleOffset.x, circleOffset.y, 0f);
                start.z += segmentOffset * m_CachedSegmentDistance;

                success = HasSpaceAtLocation(start);
                if (!success)
                    break;
            }

            if (!success)
            {
                Debug.LogWarning(string.Format("Failed to generate wire after {0} attempts", attempts), this);
                return null;
            }

            int segments = Random.Range(m_MinSegments, m_MaxSegments + 1);
            int sparkDelay = Random.Range(0, m_MaxSparkSegmentDelay + 1);

            // Random factory (themes)
            WireFactory factory = null;
            if (m_Factories.Length > 0)
                factory = m_Factories[Random.Range(0, m_Factories.Length)];

            return GenerateWire(start, segments, sparkDelay, factory);
        }

        /// <summary>
        /// Generates and activates a wire with given attributes
        /// </summary>
        /// <param name="start">Start position of the wire</param>
        /// <param name="segments">Segments of the wire</param>
        /// <param name="sparkDelay">Delay before spawning spark</param>
        /// <param name="factory">Factory for wires aesthetics</param>
        /// <returns>Wire with properties or null</returns>
        private WireAlt GenerateWire(Vector3 start, int segments, int sparkDelay, WireFactory factory)
        {
            WireAlt wire = GetWire();
            if (!wire)
                return null;

            wire.ActivateWire(start, segments, m_CachedSegmentDistance);

            if (sparkDelay > 0)
                StartCoroutine(DelaySparkActivation(sparkDelay, wire));
            else
                wire.ActivateSpark(GetSpark(), m_SparkSwitchInterval);

            return wire;
        }

        /// <summary>
        /// Get the segment the player is up to
        /// </summary>
        /// <returns>Segment of player</returns>
        public int GetJumpersSegment()
        {
            if (m_SparkJumper && m_SparkJumper.spark)
                return GetPositionSegment(m_SparkJumper.spark.transform.position);

            return 0;
        }

        /// <summary>
        /// Get the origin of the wire spawn circle
        /// </summary>
        /// <returns>Position in world space</returns>
        private Vector3 GetSpawnCircleCenter()
        {
            if (m_SparkJumper && m_SparkJumper.spark)
            {
                Vector3 center = m_SparkJumper.spark.transform.position;

                // We would use Ceil in thise case, but this function uses floor
                int segment = GetPositionSegment(center) + 1;
                center.z = m_CachedSegmentDistance * segment;

                return center;
            }

            return transform.position;
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
            m_Wires.ActivateObject();

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
            m_Sparks.ActivateObject();

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

        /// <summary>
        /// Get the segment the position is based in with origin being the start
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <returns>Positions segment (clamped to zero)</returns>
        public int GetPositionSegment(Vector3 position)
        {
            float distance = Mathf.Abs(position.z - transform.position.z);
            int segment = Mathf.FloorToInt(distance / m_CachedSegmentDistance);

            return Mathf.Max(segment, 0);
        }

        /// <summary>
        /// If a wire has space to spawn at location, assumes position will be in front of active wires
        /// </summary>
        /// <param name="position">Position of wire</param>
        /// <returns>If wire has space</returns>
        bool HasSpaceAtLocation(Vector3 position)
        {
            float sqrMinDistance = m_MinWireOffset * m_MinWireOffset;

            // Chance we might need this
            int start = GetPositionSegment(position);

            // We only consider active wires
            for (int i = 0; i < m_Wires.activeCount; ++i)
            {
                WireAlt wire = m_Wires.GetObject(i);

                Vector2 offset = position - wire.transform.position;
                float distance = offset.sqrMagnitude;

                // Might be in within min offset required, but not in in terms of the z axis
                if (distance < sqrMinDistance)
                {
                    int wireEnd = GetPositionSegment(wire.end);
                    if (start <= wireEnd)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Delays the spark activation for a wire
        /// </summary>
        /// <param name="delay">Segments to pass before activating</param>
        /// <param name="wire">Wire to activate</param>
        private IEnumerator DelaySparkActivation(int delay, WireAlt wire)
        {
            yield return new WaitForSegment(this, GetJumpersSegment() + delay);
            wire.ActivateSpark(GetSpark(), m_SparkSwitchInterval);
        }

        void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            if (m_Debug)
            {
                // Draw spawn radius
                {
                    Gizmos.color = Color.green;

                    Vector3 center = GetSpawnCircleCenter();

                    const int segments = 16;
                    const float step = Mathf.PI * 2f / segments;
                    for (int i = 0; i < segments; ++i)
                    {
                        float crad = step * i;
                        float nrad = step * ((i + 1) % segments);

                        Vector3 cdir = new Vector3(Mathf.Cos(crad), Mathf.Sin(crad), 0f);
                        Vector3 ndir = new Vector3(Mathf.Cos(nrad), Mathf.Sin(nrad), 0f);

                        // Inner border
                        {
                            Vector3 start = center + cdir * m_MinWireOffset;
                            Vector3 end = center + ndir * m_MinWireOffset;
                            Gizmos.DrawLine(start, end);
                        }

                        // Outer border
                        {
                            Vector3 start = center + cdir * m_MaxWireOffset;
                            Vector3 end = center + ndir * m_MaxWireOffset;
                            Gizmos.DrawLine(start, end);
                        }
                    }
                }

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
