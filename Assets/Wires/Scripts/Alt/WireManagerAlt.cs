using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TO5.Wires
{
    /// <summary>
    /// Yield instruction for waiting until player reaches desired segment
    /// </summary>
    public class WaitForSegment : CustomYieldInstruction
    {
        public override bool keepWaiting { get { return m_WireManager.GetJumpersSegment() < m_Segment; } }

        private WireManagerAlt m_WireManager;
        private int m_Segment;

        public WaitForSegment(WireManagerAlt wireManager, int segment)
        {
            m_WireManager = wireManager;
            m_Segment = segment;
        }
    }

    /// <summary>
    /// Manager for the wires and sparks that spawn in. Offers event for listeners
    /// </summary>
    public class WireManagerAlt : MonoBehaviour
    {
        public static Vector3 WirePlane = Vector3.forward;

        [Header("Player")]
        [SerializeField] private SparkJumperAlt m_SparkJumper;      // The players spark jumper

        [Header("Sparks")]
        public SparkAlt m_SparkPrefab;                  // The sparks to use
        public float m_SparkSpeed = 1f;                 // Shared speed of all sparks
        public float m_SparkSwitchInterval = 2f;        // Interval for sparks switching between on and off

        private ObjectPool<SparkAlt> m_Sparks = new ObjectPool<SparkAlt>();     // Sparks being managed

        [Header("Wires")]
        [SerializeField] private int m_MinSegments = 8;         // Min amount of segments per wire
        [SerializeField] private int m_MaxSegments = 15;        // Max amount of segments per wire
        [SerializeField] private int m_InitialSegments = 10;    // Segments to not start anything
        [SerializeField] WireFactory[] m_Factories;             // Factories for generating wire types
        [SerializeField] WireAnimator m_WireAnimator;           // Animator for the wire

        public WireAlt m_WirePrefab;

        [Header("Generation")]
        [SerializeField] private int m_MinSpawnInterval = 4;        // Min segments to wait before spawning new wires
        [SerializeField] private int m_MaxSpawnInterval = 6;        // Max segments to wait before spawning new wires
        [SerializeField] private float m_MinWireOffset = 5f;        // Min distance away to spawn wires
        [SerializeField] private float m_MaxWireOffset = 20f;       // Max distance away to spawn wires
        [SerializeField] private int m_SegmentOffset = 2;           // Segments in front of current segment to spawn next wire
        [SerializeField] private int m_MaxSegmentOffsetRange = 3;   // Range from offset segment for spawning wires
        [SerializeField] private int m_MaxSparkSegmentDelay = 3;    // The max amount of segments to travel before a spark will spawn on a new wire

        private ObjectPool<WireAlt> m_Wires = new ObjectPool<WireAlt>();            // Wires being managed
        private float m_CachedSegmentDistance = 1f;                                 // Distance between the start and end of a segment

        [Header("Manager")]
        [SerializeField] private bool m_AutoStart = true;       // If game starts automatically
        [SerializeField] private Transform m_DisabledSpot;      // Spot to hide disabled objects
        [SerializeField] private ScoreManager m_ScoreManager;   // Manager for scoring

        // Spot for hiding inactive objects
        private Vector3 disabledSpot { get { return m_DisabledSpot ? m_DisabledSpot.position : Vector3.zero; } }

        #if UNITY_EDITOR
        [Header("Debug")]
        public bool m_Debug = true;                             // If debugging is enabled
        public bool m_DrawSegmentPlanes = false;                // If segment planes should be drawn
        [SerializeField] private bool m_UseDebugMesh = false;   // If debug mesh should be used
        [SerializeField] private Mesh m_DebugMesh;              // Debug mesh drawn instead of animated mesh
        [SerializeField] private Text m_DebugText;              // Text for writing debug data
#endif

        void Awake()
        {
            if (m_ScoreManager)
                m_ScoreManager.Initialize(this);
        }

        void Start()
        {
            if (m_AutoStart && m_SparkJumper)
            {
                m_CachedSegmentDistance = CalculateSegmentDistance();

                WireAlt wire = GenerateWire(transform.position, m_InitialSegments, 0, null);
                m_SparkJumper.JumpToSpark(wire.spark, true);

                StartCoroutine(WireSpawnRoutine(m_MinSpawnInterval, m_MaxSpawnInterval + 1));

                if (m_ScoreManager)
                    m_ScoreManager.EnableScoring();
            }
        }

        void Update()
        {
            // Step is influenced by multiplier
            float gameSpeed = m_ScoreManager ? m_ScoreManager.multiplier : 1f;
            float step = m_SparkSpeed * gameSpeed * Time.deltaTime;

            for (int i = 0; i < m_Wires.activeCount; ++i)
            {
                WireAlt wire = m_Wires.GetObject(i);

                float progress = wire.TickWire(step);
                if (progress >= 1f)
                {
                    DeactivateWire(wire);

                    // Object pool swaps when deactivating objects, we need
                    // to update the swapped object since it is still active
                    --i;
                }
            }
        }

        public void StartWires()
        {

        }

        public void EndWires()
        {

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
                int segmentOffset = m_SegmentOffset + Random.Range(-m_MaxSegmentOffsetRange, m_MaxSegmentOffsetRange + 1);

                start = GetSpawnCircleCenter() + new Vector3(circleOffset.x, circleOffset.y, 0f);
                start.z += segmentOffset * m_CachedSegmentDistance;

                success = HasSpaceAtLocation(start);
                if (success)
                    break;
            }

            if (!success)
            {
                Debug.LogWarning(string.Format("Failed to generate wire after {0} attempts", maxAttempts), this);
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
                GenerateSpark(wire);

            return wire;
        }

        /// <summary>
        /// Generates a spark, will move it to keep in line with segment planes
        /// </summary>
        /// <param name="wire">Wire to attach spark to</param>
        /// <returns>Spark with properties or null</returns>
        private SparkAlt GenerateSpark(WireAlt wire)
        {
            SparkAlt spark = GetSpark();
            if (!spark)
                return null;

            wire.ActivateSpark(spark, m_SparkSwitchInterval);

            Vector3 position = m_SparkJumper.GetPosition();
            float distance = Mathf.Abs(position.z - transform.position.z);
            float offset = distance - (m_CachedSegmentDistance * Mathf.FloorToInt(distance / m_CachedSegmentDistance));

            spark.transform.position += WirePlane * offset;

            if (!m_SparkJumper.spark)
                m_SparkJumper.JumpToSpark(spark, true);

            return spark;
        }

        /// <summary>
        /// Deactivates the wire, handles if player is attached to spark
        /// </summary>
        /// <param name="wire">Wire to deactivate</param>
        private void DeactivateWire(WireAlt wire)
        {
            // Resetting wire will have it drop its spark reference
            SparkAlt spark = wire.spark;

            if (spark && spark.sparkJumper != null)
            {
                JumpToClosestWire(wire);

                if (m_ScoreManager)
                    m_ScoreManager.DecreaseMultiplier(1);
            }

            wire.DeactivateWire();
            wire.transform.position = disabledSpot;
            spark.transform.position = disabledSpot;

            m_Sparks.DeactivateObject(spark);
            m_Wires.DeactivateObject(wire);
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
                int segment = GetPositionSegment(center) + 1 + m_SegmentOffset;
                center.z = m_CachedSegmentDistance * segment;

                return center;
            }

            return transform.position;
        }

        /// <summary>
        /// Finds the closest wire to the source wire that player can jump to
        /// </summary>
        /// <param name="wire">Source wire</param>
        /// <param name="requiresSpark">Only check wires with sparks</param>
        /// <returns>Closest active wire or null</returns>
        private WireAlt FindClosestWireTo(WireAlt wire, bool requiresSpark)
        {
            if (!wire)
                return null;

            WireAlt closestWire = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < m_Wires.activeCount; ++i)
            {
                // Wire might require spark
                WireAlt w = m_Wires.GetObject(i);
                if (w == wire || (requiresSpark && !w.spark))
                    continue;

                Vector2 displacement = w.transform.position - wire.transform.position;
                float sqrDistance = displacement.sqrMagnitude;

                if (sqrDistance < closestDistance)
                {
                    closestWire = w;
                    closestDistance = sqrDistance;
                }
            }

            return closestWire;
        }

        /// <summary>
        /// Finds the best wire to jump the player to
        /// </summary>
        /// <param name="wire">Wire to jump from</param>
        /// <returns>Best wire or null</returns>
        private WireAlt FindBestWireToJumpTo(WireAlt wire)
        {
            if (!wire)
                return null;

            WireAlt bestWire = null;
            if (m_Wires.activeCount > 0)
            {
                bestWire = m_Wires.GetObject(0);

                // Start at one since we already 'tested' it
                for (int i = 1; i < m_Wires.activeCount; ++i)
                {
                    // Wire requires spark
                    WireAlt w = m_Wires.GetObject(i);
                    if (w == wire || !w.spark)
                        continue;

                    // Use the wire whose spark has made less progress
                    if (w.sparkProgress < bestWire.sparkProgress)
                        bestWire = w;
                }
            }

            return bestWire;
        }

        /// <summary>
        /// Helper for getting a wire from the pool (spawns one if needed)
        /// </summary>
        /// <returns>Wire</returns>
        private WireAlt GetWire()
        {
            if (m_Wires.canActivateObject)
                return m_Wires.ActivateObject();

            WireAlt wire = null;

            // New to make a new wire object
            if (m_WirePrefab)
            {
                wire = Instantiate(m_WirePrefab);
            }
            else
            {
                GameObject gameObject = new GameObject("Wire");
                wire = gameObject.AddComponent<WireAlt>();
            }

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

            return 1f;
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
        /// Has player jumper to wire closest to origin wire
        /// </summary>
        /// <param name="origin">Wire to jump from</param>
        private void JumpToClosestWire(WireAlt origin)
        {
            if (m_SparkJumper)
            {
                WireAlt closest = FindBestWireToJumpTo(origin);//FindClosestWireTo(origin, true);
                if (closest)
                {
                    m_SparkJumper.JumpToSpark(closest.spark, true);
                }
                else
                {
                    Debug.LogWarning("Failed to move spark jumper as no wires either exist or have sparks on them");
                }
            }
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
        /// Routine for generating wires
        /// </summary>
        /// <param name="minDelay">Min segments between spawns</param>
        /// <param name="maxDelay">Max segments between spawns</param>
        private IEnumerator WireSpawnRoutine(int minDelay, int maxDelay)
        {
            while (enabled)
            {
                int delay = Random.Range(minDelay, maxDelay);
                yield return new WaitForSegment(this, GetJumpersSegment() + delay);

                GenerateRandomWire();
            }
        }

        /// <summary>
        /// Delays the spark activation for a wire
        /// </summary>
        /// <param name="delay">Segments to pass before activating</param>
        /// <param name="wire">Wire to activate</param>
        private IEnumerator DelaySparkActivation(int delay, WireAlt wire)
        {
            yield return new WaitForSegment(this, GetJumpersSegment() + delay);
            GenerateSpark(wire);
        }

        void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            if (m_Debug)
            {
                Vector3 center = GetSpawnCircleCenter();

                // Draw spawn radius
                {
                    Gizmos.color = Color.green;     

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
    
                if (Application.isPlaying)
                {
                    // Draw segment planes
                    if (m_DrawSegmentPlanes && m_SparkJumper)
                    {
                        Color color = Color.cyan;
                        color.a = 0.5f;

                        Gizmos.color = color;
                        for (int i = -m_MaxSegmentOffsetRange; i <= m_MaxSegmentOffsetRange; ++i)
                        {
                            Vector3 offset = new Vector3(0f, 0f, m_CachedSegmentDistance * i);
                            Gizmos.DrawCube(center + offset, new Vector3(50f, 50f, 0.01f));
                        }
                    }
                }
            }
            #endif
        }
    }
}
