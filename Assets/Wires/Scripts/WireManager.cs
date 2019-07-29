using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace TO5.Wires
{
    /// <summary>
    /// Yield instruction for waiting until player reaches desired segment
    /// </summary>
    public class WaitForSegment : CustomYieldInstruction
    {
        public override bool keepWaiting { get { return m_WireManager.GetJumpersSegment() < m_Segment; } }

        private WireManager m_WireManager;      // Wire manager to listen to
        private int m_Segment;                  // Segment to reach

        public WaitForSegment(WireManager wireManager, int segment)
        {
            Assert.IsNotNull(wireManager, "WaitForSegment expects valid wire manager");

            m_WireManager = wireManager;
            m_Segment = segment;
        }
    }

    /// <summary>
    /// Yield instruction for waiting until player travels desired segments (on a wire)
    /// </summary>
    public class WaitForSegmentsTravelled : CustomYieldInstruction
    {
        public override bool keepWaiting { get { return m_WireManager.sparkJumper.wireDistanceTravelled < m_End; } }

        private WireManager m_WireManager;      // Wire manager to listen to
        private float m_End;                    // Distance player must reach

        public WaitForSegmentsTravelled(WireManager wireManager, int segments)
        {
            Assert.IsNotNull(wireManager, "WaitForSegmentsTravelled expects valid wire manager");

            m_WireManager = wireManager;
            m_End = m_WireManager.sparkJumper.wireDistanceTravelled + (m_WireManager.segmentLength * segments);
        }
    }

    /// <summary>
    /// Manager for the wires and sparks that spawn in. Offers event for listeners
    /// </summary>
    public class WireManager : MonoBehaviour
    {
        public static readonly Vector3 WirePlane = Vector3.forward;
        
        /// <summary>
        /// Delegate for when the player has jumped off a wire
        /// </summary>
        /// <param name="failed">If player 'failed' to jump (was auto jumped or jumped while drifting)</param>
        public delegate void JumpedOffWire(Wire wire, bool failed);

        public JumpedOffWire PlayerJumpedOffWire;       // Event for when player has jumped off a wire
        private bool m_IsRunning = false;               // If wires are being ticked
        private bool m_GeneratingWires = false;         // If wires are being generated automatically
        private bool m_WireFailed = false;              // If player has jump penalty applied (didn't jump in time)

        [Header("Player")]
        [SerializeField] private SparkJumper m_SparkJumper;      // The players spark jumper

        [Header("Sparks")]
        public Spark m_SparkPrefab;                                         // The sparks to use
        [SerializeField] private float m_BoostSpeedMultiplier = 1.25f;      // Multiplier to speed when boost is active
        public AudioSource m_SparkSpawnAudioSource;                         // Audio source to play spark spawn sound

        [Header("Wires")]
        [SerializeField] private int m_InitialSegments = 10;            // Segments for initial starting wire
        [SerializeField] WireAnimator m_WireAnimator;                   // Animator for the wire
        [SerializeField] WireFactory[] m_Factories;                     // Factories for generating wire types
        public bool m_DriftingEnabled = true;                           // If drifting is enabled
        [SerializeField] private float m_MaxDriftTime = 5f;             // Max time player can be drifting for before auto jump

        private ObjectPool<Spark> m_Sparks = new ObjectPool<Spark>();       // Sparks being managed
        private Coroutine m_DriftRoutine;                                   // Routine for when player is drifting 

        public Wire m_WirePrefab;

        [Header("Generation")]
        [SerializeField] private float m_WireSpace = 4f;                        // Space needed for wires to spawn
        [SerializeField] private WireStageProperties[] m_WireProperties;        // Properties for wire behavior for each multiplier stage

        #if UNITY_EDITOR
        public int m_WirePropertiesStagePreview = 0;            // Preview properties for wire stage at index
        #endif

        private ObjectPool<Wire> m_Wires = new ObjectPool<Wire>();              // Wires being managed
        private float m_CachedSegmentDistance = 1f;                             // Distance between the start and end of a segment
        private WireStageProperties m_ActiveWireProperties;                     // Properties for current stage
        private Coroutine m_WireSpawnRoutine;                                   // Routine for spawning wires
        private Wire m_DriftingWire;                                            // The wire the player was last on before drifting

        [Header("Manager")]
        [SerializeField] private Transform m_DisabledSpot;                      // Spot to hide disabled objects
        [SerializeField] private ScoreManager m_ScoreManager;                   // Manager for scoring
        [SerializeField] private bool m_TickWhenJumping = true;                 // If wires/sparks should tick while player is jumping

        // If the game is running
        public bool isRunning { get { return m_IsRunning; } }

        // Players spark jumper
        public SparkJumper sparkJumper { get { return m_SparkJumper; } }

        // Length of a segment
        public float segmentLength { get { return m_CachedSegmentDistance; } }

        // Spot for hiding inactive objects
        public Vector3 disabledSpot { get { return m_DisabledSpot ? m_DisabledSpot.position : Vector3.zero; } }

        // Score manager to track multiplier
        public ScoreManager scoreManager { get { return m_ScoreManager; } }

        #if UNITY_EDITOR
        [Header("Debug")]
        public bool m_Debug = true;                             // If debugging is enabled
        public bool m_DrawSegmentPlanes = false;                // If segment planes should be drawn
        [SerializeField] private bool m_UseDebugMesh = false;   // If debug mesh should be used
        [SerializeField] private Mesh m_DebugMesh;              // Debug mesh drawn instead of animated mesh
        [SerializeField] private Text m_DebugText;              // Text for writing debug data
        #endif

        // If tutorial settings should be used
        public bool tutorialMode { get; private set; }      
        
        void Awake()
        {
            if (m_ScoreManager)
            {
                m_ScoreManager.Initialize(this);
                m_ScoreManager.OnMultiplierUpdated += MultiplierUpdated;
            }
        }

        void Update()
        {
            float step = 0f;
         
            // Don't tick when player is jumping
            if (m_TickWhenJumping || !m_SparkJumper.isJumping)
            {
                Profiler.BeginSample("WireManager.Tick", this);

                WireStageProperties wireProps = GetStageWireProperties();

                if (sparkJumper.isDrifting)
                {
                    step = wireProps.m_SparkSpeed * wireProps.m_SparkDriftScale * Time.deltaTime;

                    // Move player with drift
                    Vector3 position = m_SparkJumper.GetPosition() + (WirePlane * step);
                    m_SparkJumper.SetPosition(position);
                }
                else
                {
                    // Step is influenced by multiplier stage
                    float gameSpeed = 1f, boostMultiplier = 1f;
                    if (m_ScoreManager)
                    {
                        gameSpeed = m_ScoreManager.multiplierStage + 1;
                        boostMultiplier = m_ScoreManager.boostActive ? m_BoostSpeedMultiplier : 1f;
                    }

                    step = wireProps.m_SparkSpeed * gameSpeed * boostMultiplier * Time.deltaTime;
                }

                for (int i = 0; i < m_Wires.activeCount; ++i)
                {
                    Wire wire = m_Wires.GetObject(i);

                    // We don't update the drifting wire (as it should already be finished)
                    if (wire == m_DriftingWire)
                        continue;

                    float progress = wire.TickWire(step);
                    if (progress >= 1f)
                    {
                        HandleDeactivatingWire(wire);

                        // Object pool swaps when deactivating objects, we need
                        // to update the swapped object since it is still active
                        --i;
                    }
                }

                Profiler.EndSample();
            }

            #if UNITY_EDITOR
            // Debug text
            if (m_DebugText)
                m_DebugText.text = string.Format("Step: {0}\nCached Segment Distance: {1}\n" +
                    "Wires Pool Size: {2}\nWires Active: {3}\nSparks Pool Size: {4}\nSparks Active: {5}",
                    step, m_CachedSegmentDistance, m_Wires.Count, m_Wires.activeCount, m_Sparks.Count, m_Sparks.activeCount);
            #endif
        }

        /// <summary>
        /// Initializes constants and hooks up events
        /// </summary>
        /// <returns>If no errors were encountered</returns>
        private bool Initialize()
        {
            if (!m_SparkJumper)
            {
                Debug.LogError("Failed to intialize wire manager as spark jumper is null");
                return false;
            }

            // Cache constants
            {
                m_CachedSegmentDistance = CalculateSegmentDistance();
            }

            // Hook events
            {
                m_SparkJumper.OnJumpToSpark += JumpToSpark;
            }

            return true;
        }

        /// <summary>
        /// Starts wires in tutorial mode. Any wire generation must be handled manually (calling GenerateRandomWire is allowed).
        /// Generates the default wire and spark for player to jump onto
        /// </summary>
        /// <param name="wireProps">Wire properties to use for tutorial mode</param>
        /// <returns>If no errors were encountered</returns>
        public bool StartTutorial(WireStageProperties wireProps, int initialSegments)
        {
            if (!m_IsRunning)
            {
                if (!Initialize())
                    return false;

                m_ActiveWireProperties = wireProps;

                if (m_SparkJumper)
                    m_SparkJumper.m_JumpTime = m_ActiveWireProperties.m_JumpTime;

                // Attach player to initial wire
                {
                    WireFactory factory = GetRandomWireFactory();
                    Wire spawnWire = GenerateWire(transform.position, initialSegments, 0, 0f, 0f, factory);

                    Assert.IsNotNull(spawnWire.spark);
                }

                m_IsRunning = true;
                tutorialMode = true;
                enabled = true;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Starts spawning wires. Can be used by default or after calling StartTutorial.
        /// </summary>
        /// <returns>If no errors were encountered</returns>
        public bool StartWires()
        {
            if (!m_IsRunning || tutorialMode)
            {
                if (!tutorialMode)
                {
                    if (!Initialize())
                        return false;

                    // Attach player to initial wire
                    {
                        WireFactory factory = GetRandomWireFactory();
                        Wire spawnWire = GenerateWire(transform.position, m_InitialSegments, 0, 0f, 0f, factory);

                        Assert.IsNotNull(spawnWire.spark);
                    }
                }

                tutorialMode = false;

                if (m_ScoreManager)
                {
                    m_ScoreManager.EnableScoring(true);
                    m_ActiveWireProperties = GetWireProperties(m_ScoreManager.multiplierStage);

                    if (m_SparkJumper)
                        m_SparkJumper.m_JumpTime = m_ActiveWireProperties.m_JumpTime;
                }

                // We set this after to use the correct wire properties
                SetWireGenerationEnabled(true);

                m_IsRunning = true;                
                enabled = true;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Stops spawning wires and updating sparks
        /// </summary>
        public void StopWires()
        {
            if (m_IsRunning)
            {
                if (m_ScoreManager)
                    m_ScoreManager.DisableScoring();

                SetWireGenerationEnabled(false);

                // Destroy all spawned objects
                //m_Sparks.Clear(true);
                //m_Wires.Clear(true);

                enabled = false;
                tutorialMode = false;
                m_IsRunning = false;
            }
        }

        /// <summary>
        /// Set if automatic wire generation is enabled
        /// </summary>
        /// <param name="enable">If to enable</param>
        public void SetWireGenerationEnabled(bool enable)
        {
            if (!tutorialMode && m_GeneratingWires != enable)
            {
                m_GeneratingWires = enable;

                if (m_GeneratingWires)
                {
                    m_WireSpawnRoutine = StartCoroutine(WireSpawnRoutine());
                }
                else
                {
                    StopCoroutine(m_WireSpawnRoutine);
                    m_WireSpawnRoutine = null;
                }
            }
        }

        /// <summary>
        /// Generates a random wire
        /// </summary>
        /// <param name="instantSpark">If spark should generate with wire</param>
        /// <returns>Randomly generated wire or null</returns>
        public Wire GenerateRandomWire(bool instantSpark)
        {
            const int maxAttempts = 5;
            WireStageProperties wireProps = GetStageWireProperties();

            if (m_Wires.activeCount >= wireProps.m_MaxWiresAtOnce)
                return null;

            Profiler.BeginSample("GenerateRandomWire", this);

            Vector3 spawnCenter = GetSpawnCircleCenter();

            Vector3 start = transform.position;
            bool success = true;

            // We don't want to loop too many times
            int attempts = 0;
            while (++attempts <= maxAttempts)
            {
                Vector2 circleOffset = Wires.GetRandomCircleOffset(wireProps.m_InnerSpawnRadius,
                    wireProps.m_OuterSpawnRadius, wireProps.m_BottomCircleCutoff, wireProps.m_TopCircleCutoff);

                int segmentRange = Random.Range(-wireProps.m_SpawnSegmentRange, wireProps.m_SpawnSegmentRange + 1);
                int segmentOffset = wireProps.m_SpawnSegmentOffset + segmentRange;

                start = spawnCenter + new Vector3(circleOffset.x, circleOffset.y, 0f);
                start.z += segmentOffset * m_CachedSegmentDistance;

                success = HasSpaceAtLocation(start, false);
                if (success)
                    break;
            }

            if (!success)
            {
                Debug.LogWarning(string.Format("Failed to generate wire after {0} attempts", maxAttempts), this);
                return null;
            }

            int segments = Random.Range(wireProps.m_MinSegments, wireProps.m_MaxSegments + 1);

            // We want sparks to appear instantly if player is drifting
            instantSpark = instantSpark | m_SparkJumper.isDrifting;
            int sparkDelay = instantSpark ? 0 : Random.Range(0, wireProps.m_SparkSpawnSegmentDelay + 1);

            // Wires can never be defective if switch interval is zero
            float onInterval = 0f;
            float offInterval = 0f;
            if (wireProps.m_SparkOnSwitchInterval > 0f && wireProps.m_SparkOffSwitchInterval > 0f)
            {
                float defectiveChance = wireProps.m_DefectiveWireChance;

                // Wire is defective if scaled chance is less than random number
                bool defective = defectiveChance > 0f ? Random.Range(0f, 100f) < (defectiveChance * 100f) : false;
                if (defective)
                {
                    onInterval = wireProps.m_SparkOnSwitchInterval;
                    offInterval = wireProps.m_SparkOffSwitchInterval;
                }
            }

            Profiler.EndSample();

            WireFactory factory = GetRandomWireFactory();
            return GenerateWire(start, segments, sparkDelay, onInterval, offInterval, factory);
        }

        /// <summary>
        /// Generates a random wire but with fixed properties (specified by arguements)
        /// </summary>
        /// <param name="segments">Wires length</param>
        /// <param name="instantSpark">If spark should generate with wire</param>
        /// <param name="tryDefective">If wire can be defective (false will never be defective)</param>
        /// <returns>Randomly generated wire or null</returns>
        public Wire GenerateRandomFixedWire(int segments, bool instantSpark, bool tryDefective)
        {
            const int maxAttempts = 5;
            Vector3 spawnCenter = GetSpawnCircleCenter();
            WireStageProperties wireProps = GetStageWireProperties();

            Vector3 start = transform.position;
            bool success = true;

            // We don't want to loop too many times
            int attempts = 0;
            while (++attempts <= maxAttempts)
            {
                Vector2 circleOffset = Wires.GetRandomCircleOffset(wireProps.m_InnerSpawnRadius,
                    wireProps.m_OuterSpawnRadius, wireProps.m_BottomCircleCutoff, wireProps.m_TopCircleCutoff);

                int segmentRange = Random.Range(-wireProps.m_SpawnSegmentRange, wireProps.m_SpawnSegmentRange + 1);
                int segmentOffset = wireProps.m_SpawnSegmentOffset + segmentRange;

                start = spawnCenter + new Vector3(circleOffset.x, circleOffset.y, 0f);
                start.z += segmentOffset * m_CachedSegmentDistance;

                success = HasSpaceAtLocation(start, false);
                if (success)
                    break;
            }

            if (!success)
            {
                Debug.LogWarning(string.Format("Failed to generate fixed wire after {0} attempts", maxAttempts), this);
                return null;
            }

            int sparkDelay = instantSpark ? 0 : Random.Range(0, wireProps.m_SparkSpawnSegmentDelay + 1);

            // Wires can never be defective if switch interval is zero
            float onInterval = 0f;
            float offInterval = 0f;
            if (tryDefective && wireProps.m_SparkOnSwitchInterval > 0f && wireProps.m_SparkOffSwitchInterval > 0f)
            {    
                float defectiveChance = wireProps.m_DefectiveWireChance;

                // Wire is defective if scaled chance is less than random number
                bool defective = defectiveChance > 0f ? Random.Range(0f, 100f) < (defectiveChance * 100f) : false;
                if (defective)
                {
                    onInterval = wireProps.m_SparkOnSwitchInterval;
                    offInterval = wireProps.m_SparkOffSwitchInterval;
                }
            }

            WireFactory factory = GetRandomWireFactory();
            return GenerateWire(start, segments, sparkDelay, onInterval, offInterval, factory);
        }

        /// <summary>
        /// Generates and activates a wire with given attributes
        /// </summary>
        /// <param name="start">Start position of the wire</param>
        /// <param name="segments">Segments of the wire</param>
        /// <param name="sparkDelay">Delay before spawning spark</param>
        /// <param name="onInterval">Interval for spark switching status being on</param>
        /// <param name="offInterval">Interval for spark switching status being off</param>
        /// <param name="factory">Factory for wires aesthetics</param>
        /// <returns>Wire with properties or null</returns>
        public Wire GenerateWire(Vector3 start, int segments, int sparkDelay, float onInterval, float offInterval, WireFactory factory)
        {
            Wire wire = GetWire();
            if (!wire)
                return null;

            wire.ActivateWire(start, segments, m_CachedSegmentDistance, factory);

            if (sparkDelay > 0)
                StartCoroutine(DelaySparkActivation(sparkDelay, wire, onInterval, offInterval));
            else
                GenerateSpark(wire, onInterval, offInterval);

            return wire;
        }

        /// <summary>
        /// Generates a spark, will move it to keep in line with segment planes
        /// </summary>
        /// <param name="wire">Wire to attach spark to</param>
        /// <param name="onInterval">Interval for wire switching between on and off</param>
        /// <returns>Spark with properties or null</returns>
        private Spark GenerateSpark(Wire wire, float onInterval, float offInterval)
        {
            Spark spark = GetSpark();
            if (!spark)
                return null;

            wire.ActivateSpark(spark, onInterval, offInterval);

            Vector3 position = m_SparkJumper.GetPosition();
            float distance = Mathf.Abs(position.z - transform.position.z);
            float offset = distance - (m_CachedSegmentDistance * Mathf.FloorToInt(distance / m_CachedSegmentDistance));

            spark.transform.position += WirePlane * offset;

            // Don't jump if drifting
            if (!m_SparkJumper.spark && !m_SparkJumper.isDrifting)
                m_SparkJumper.InstantJumpToSpark(spark);
            else
                PlayAudioClip(m_SparkSpawnAudioSource, spark.transform.position);
                
            return spark;
        }

        /// <summary>
        /// Handles deactivating the wire, including if player is attached to spark
        /// </summary>
        /// <param name="wire">Wire to deactivate</param>
        private void HandleDeactivatingWire(Wire wire)
        {
            // Resetting wire will have it drop its spark reference
            Spark spark = wire.spark;

            // Penalties for not jumping before reaching the end of a wire
            if (spark && spark.sparkJumper != null)
            {
                m_WireFailed = true;

                spark.DetachJumper();

                // Scoring is disabled in tutorial mode
                if (!tutorialMode && m_ScoreManager)
                {
                    m_ScoreManager.TryDecreaseMultiplier();

                    if (m_DriftingEnabled)
                        m_ScoreManager.DisableScoring();
                }

                // Drifting is disabled in tutorial mode
                if (!tutorialMode && m_DriftingEnabled)
                {
                    // Jumper drifts independant of spark
                    m_SparkJumper.SetDriftingEnabled(true);

                    m_DriftingWire = wire;
                    m_DriftRoutine = StartCoroutine(AutoJumpRoutine());

                    // We end here as we will deactivate the drifting wire later
                    return;
                }
                else
                {
                    JumpToClosestWire(wire);
                }         
            }

            DeactivateWire(wire);
        }

        /// <summary>
        /// Deactivates given wire (and wires spark), returning both to pool
        /// </summary>
        /// <param name="wire">Wire to deactivate</param>
        private void DeactivateWire(Wire wire)
        {
            if (wire)
            {
                Spark spark = wire.spark;

                wire.DeactivateWire();
                wire.transform.position = disabledSpot;
                m_Wires.DeactivateObject(wire);

                if (spark)
                {
                    spark.transform.position = disabledSpot;
                    m_Sparks.DeactivateObject(spark);        
                }
            }
        }

        /// <summary>
        /// Deactivates the wire the player is drifting from
        /// </summary>
        private void DeactivateDriftingWire()
        {
            DeactivateWire(m_DriftingWire);
            m_DriftingWire = null;
            m_DriftRoutine = null;
        }

        /// <summary>
        /// Get the segment the player is up to
        /// </summary>
        /// <returns>Segment of player</returns>
        public int GetJumpersSegment()
        {
            if (m_SparkJumper)
                return GetPositionSegment(m_SparkJumper.GetPosition());

            return 0;
        }

        /// <summary>
        /// Get the origin of the wire spawn circle
        /// </summary>
        /// <returns>Position in world space</returns>
        public Vector3 GetSpawnCircleCenter()
        {
            if (m_SparkJumper && m_SparkJumper.spark)
            {
                Vector3 center = m_SparkJumper.spark.transform.position;
                WireStageProperties wireProps = GetStageWireProperties();

                // We would use Ceil in thise case, but this function uses floor
                int segment = GetPositionSegment(center) + 1 + wireProps.m_SpawnSegmentOffset;
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
        private Wire FindClosestWireTo(Wire wire, bool requiresSpark)
        {
            if (!wire)
                return null;

            Wire closestWire = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < m_Wires.activeCount; ++i)
            {
                // Wire might require spark
                Wire w = m_Wires.GetObject(i);
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
        private Wire FindBestWireToJumpTo(Wire wire)
        {
            if (!wire)
                return null;

            Wire bestWire = null;
            if (m_Wires.activeCount > 0)
            {
                for (int i = 0; i < m_Wires.activeCount; ++i)
                {
                    // Wire requires spark
                    Wire w = m_Wires.GetObject(i);
                    if (w == wire || !w.spark)
                        continue;
                    
                    if (bestWire == null)
                        bestWire = w;

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
        private Wire GetWire()
        {
            if (m_Wires.canActivateObject)
                return m_Wires.ActivateObject();

            Wire wire = null;

            // Need to make a new wire object
            if (m_WirePrefab)
            {
                wire = Instantiate(m_WirePrefab);
            }
            else
            {
                GameObject gameObject = new GameObject("Wire");
                wire = gameObject.AddComponent<Wire>();
            }

            m_Wires.Add(wire);
            m_Wires.ActivateObject();

            return wire;
        }

        /// <summary>
        /// Get a random wire factory from array of factories
        /// </summary>
        /// <returns>Random factory or null if empty</returns>
        public WireFactory GetRandomWireFactory()
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
        private Spark GetSpark()
        {
            if (m_Sparks.canActivateObject)
                return m_Sparks.ActivateObject();   

            if (!m_SparkPrefab)
            {
                Debug.LogError("Unable to spawn spark as prefab is invalid", this);
                return null;
            }

            Vector3 position = m_DisabledSpot ? m_DisabledSpot.position : Vector3.zero;
            Spark spark = Instantiate(m_SparkPrefab, position, Quaternion.identity);

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
        private void JumpToClosestWire(Wire origin)
        {
            if (m_SparkJumper)
            {
                Wire closest = FindBestWireToJumpTo(origin);//FindClosestWireTo(origin, true);
                if (!closest)
                {
                    Debug.LogWarning("Failed to move spark jumper as no wires either exist or have sparks on them");
                    Debug.LogWarning("Generating new wire and jumping to it");

                    while (!closest)
                        closest = GenerateRandomWire(true);
                }

                m_WireFailed = true;
                m_SparkJumper.JumpToSpark(closest.spark, true);
            }
        }

        /// <summary>
        /// If a wire has space to spawn at location, assumes position will be in front of active wires
        /// </summary>
        /// <param name="position">Position of wire</param>
        /// <param name="ignoreZ">If Z axis should be ignored</param>
        /// <returns>If wire has space</returns>
        public bool HasSpaceAtLocation(Vector3 position, bool ignoreZ)
        {
            WireStageProperties wireProps = GetStageWireProperties();
            float sqrMinDistance = m_WireSpace * m_WireSpace;

            // Chance we might need this
            int start = GetPositionSegment(position);

            // We only consider active wires
            for (int i = 0; i < m_Wires.activeCount; ++i)
            {
                Wire wire = m_Wires.GetObject(i);
                Assert.IsNotNull(wire);

                Vector2 offset = position - wire.transform.position;
                float distance = offset.sqrMagnitude;

                // Might be in within min offset required, but not in in terms of the z axis
                if (distance < sqrMinDistance)
                {
                    bool invalid = ignoreZ || start <= GetPositionSegment(wire.end);
                    if (invalid)
                        return false;
                }
            }

            // We only consider active packets
            if (m_ScoreManager)
            {
                sqrMinDistance = m_ScoreManager.packetSpace * m_ScoreManager.packetSpace;

                for (int i = 0; i < m_ScoreManager.activePackets; ++i)
                {
                    DataPacket packet = m_ScoreManager.GetActivePacket(i);
                    Assert.IsNotNull(packet);

                    Vector2 offset = position - packet.transform.position;
                    float distance = offset.sqrMagnitude;

                    if (distance < sqrMinDistance)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets wire generation properties for index. Checks if properties exist for
        /// index, either creating a new set or getting best replacement if it doesn't
        /// </summary>
        /// <param name="index">Index of properties</param>
        /// <returns>Valid properties</returns>
        private WireStageProperties GetWireProperties(int index)
        {
            if (m_WireProperties == null || m_WireProperties.Length == 0)
                return new WireStageProperties();

            // We use the latest properties if index is still out of range
            index = Mathf.Clamp(index, 0, m_WireProperties.Length - 1);
            return m_WireProperties[index];
        }

        /// <summary>
        /// Get wire generation properties for current multiplier stage
        /// </summary>
        /// <returns>Valid properties</returns>
        private WireStageProperties GetStageWireProperties()
        {
            if (m_ActiveWireProperties != null)
                return m_ActiveWireProperties;

            int stage = 0;
            if (m_ScoreManager)
                stage = m_ScoreManager.multiplierStage;

            m_ActiveWireProperties = GetWireProperties(stage);
            return m_ActiveWireProperties;
        }
        
        /// <summary>
        /// Overrides the current wire stage properties
        /// </summary>
        /// <param name="wireProps">Wire properties</param>
        public void OverrideStageProperties(WireStageProperties wireProps)
        {
            if (wireProps != null)
                m_ActiveWireProperties = wireProps;
        }

        /// <summary>
        /// Refreshes the wire stage properties being used
        /// </summary>
        public void RefreshStageProperties()
        {
            if (m_ScoreManager)
                m_ActiveWireProperties = GetWireProperties(m_ScoreManager.multiplierStage);
        }

        /// <summary>
        /// Routine for generating wires
        /// </summary>
        private IEnumerator WireSpawnRoutine()
        {
            while (enabled)
            {
                WireStageProperties wireProps = GetStageWireProperties();
                float delay = Random.Range(wireProps.m_MinSpawnInterval, wireProps.m_MaxSpawnInterval);

                yield return new WaitForSeconds(delay);

                GenerateRandomWire(false);
            }
        }

        /// <summary>
        /// Delays the spark activation for a wire
        /// </summary>
        /// <param name="delay">Segments to pass before activating</param>
        /// <param name="wire">Wire to activate</param>
        /// <param name="onInterval">Interval for spark status remaining on</param>
        /// <param name="offInterval">Interval for spark status remaining off</param>
        private IEnumerator DelaySparkActivation(int delay, Wire wire, float onInterval, float offInterval)
        {
            yield return new WaitForSegment(this, GetJumpersSegment() + delay);
            GenerateSpark(wire, onInterval, offInterval);
        }

        /// <summary>
        /// Forces player to jump to suitable wire after drifting
        /// </summary>
        private IEnumerator AutoJumpRoutine()
        {
            yield return new WaitForSeconds(m_MaxDriftTime);

            // Jumping first is important (we need drifting wire to be valid in JumpToSpark callback)
            JumpToClosestWire(m_DriftingWire);

            DeactivateDriftingWire();
        }

        /// <summary>
        /// Notify that player has jumped to another spark
        /// </summary>
        private void JumpToSpark(Spark spark, bool finished)
        {
            if (finished)
            {
                if (!m_WireFailed)
                    m_ScoreManager.AwardJumpPoints();

                m_WireFailed = false;
            }
            else
            {
                // Drifting wire will be valid if player is jumping while drifting (without drift time running out)
                // We check this instead of WireFailed as the player auto jumps when drifting is disabled (in which WireFailed will also be true)
                if (m_DriftingWire)
                {
                    Assert.IsTrue(m_WireFailed);

                    StopCoroutine(m_DriftRoutine);
                    DeactivateDriftingWire();

                    // Scoring is disabled in tutorial mode
                    if (m_ScoreManager && !tutorialMode)
                        m_ScoreManager.EnableScoring(false);
                }

                if (PlayerJumpedOffWire != null)
                {
                    // Player jumped off wire while not drifting
                    PlayerJumpedOffWire.Invoke(spark.GetWire(), m_WireFailed);
                }
            }
        }

        /// <summary>
        /// Notify from score manager that players multiplier has changed
        /// </summary>
        private void MultiplierUpdated(float multiplier, int stage)
        {
            m_ActiveWireProperties = GetWireProperties(stage);

            if (m_SparkJumper)
                m_SparkJumper.m_JumpTime = m_ActiveWireProperties.m_JumpTime;
        }

        /// <summary>
        /// Plays audio source at location (with random pitch)
        /// </summary>
        /// <param name="source">Source of audio</param>
        /// <param name="position">Position of source</param>
        private void PlayAudioClip(AudioSource source, Vector3 position)
        {
            if (source)
            {
                source.transform.position = position;

                source.pitch = Random.Range(0.8f, 1.2f);
                source.time = 0f;

                source.Play();
            }
        }

        void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            if (m_Debug)
            {
                Vector3 center = GetSpawnCircleCenter();

                // We want to draw current properties while playing, while drawing preview while in inspector
                WireStageProperties wireProps = null;
                if (Application.isPlaying)
                    wireProps = GetStageWireProperties();
                else if (m_WirePropertiesStagePreview >= 0 && m_WirePropertiesStagePreview < m_WireProperties.Length)
                    wireProps = GetWireProperties(m_WirePropertiesStagePreview);

                // Draw spawn radius
                if (wireProps != null)
                {
                    Gizmos.color = Color.green;
                    Wires.DrawSpawnArea(wireProps.m_InnerSpawnRadius, wireProps.m_OuterSpawnRadius, center);

                    Gizmos.color = Color.red;
                    Wires.DrawCutoffGizmo(wireProps.m_BottomCircleCutoff, center, wireProps.m_OuterSpawnRadius, true);
                    Wires.DrawCutoffGizmo(wireProps.m_TopCircleCutoff, center, wireProps.m_OuterSpawnRadius, false);

                    // Draw score systems
                    if (m_ScoreManager)
                        m_ScoreManager.DrawDebugGizmos(center);
                }

                // Draw Wires
                Gizmos.color = Color.red;
                for (int i = 0; i < m_Wires.activeCount; ++i)
                {
                    Wire wire = m_Wires.GetObject(i);
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
                        for (int i = -3; i <= 3; ++i)
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
