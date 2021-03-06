﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace TO5.Wires
{
    /// <summary>
    /// Manages the score of the player and the active multiplier
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        private static readonly string m_TickAutoMultiplierIncrease = "TickAutoMultiplierIncrease";     // Name of function to invoke for auto multiplier increase

        /// <summary>
        /// Delegate to notify the players multiplier has changed
        /// </summary>
        /// <param name="multiplier">New multiplier</param>
        /// <param name="stage">New stage</param>
        public delegate void MultiplierStageUpdated(float multiplier, int stage);

        /// <summary>
        /// Delegate to notify the player lives has changed
        /// </summary>
        /// <param name="lives">Lives remaining</param>
        public delegate void StageLivesUpdated(int lives);

        /// <summary>
        /// Delegate to notify a packet has been despawned
        /// </summary>
        /// <param name="packet">Packet that despawned</param>
        /// <param name="wasCollected">If the packet was collected rather than expired</param>
        public delegate void PacketDespawned(DataPacket packet, bool wasCollected);

        public MultiplierStageUpdated OnMultiplierUpdated;                      // Event for when multiplier has changed
        public StageLivesUpdated OnStageLivesUpdated;                           // Event for when lives has changed
        public PacketDespawned OnPacketDespawned;                               // Event for when a packet despawns

        private bool m_IsRunning = false;               // If scoring is enabled (including packet generation)
        private bool m_GeneratingPackets = false;       // If packets are being generated automatically

        [Header("Score")]
        [SerializeField] private float m_ScorePerSecond = 1f;           // Score player earns per second
        [SerializeField] private float m_JumpScore = 100f;              // Score player earns when jumping (not when forced to jump)
        [SerializeField] private float m_PacketScore = 250f;            // Score player earns when collecting a data packet

        [Header("Multiplier")]
        [SerializeField, Range(0, 32)] private int m_MultiplierStages = 2;      // The amount of stages for the multiplier
        [SerializeField] private bool m_AllowDecrease = false;                  // If player can lose their multiplier

        [SerializeField] private MultiplierStageProperties[] m_MultiplierStageProperties;       // Properties releated to a multiplier stage

        public AudioSource m_MultiplierAudioSource;                             // Audio source to play multiplier sounds with
        public AudioClip m_MultiplierIncreaseClip;                              // Sound to play when multiplier increases
        public ParticleSystem m_MultiplierIncreaseParticles;                    // Root particle system to all increase particles (longest system should be root)

        [Header("Multiplier|Decrease")]
        [SerializeField, Min(1)] private int m_StageLives = 4;                  // Amount of 'lives' player has per stage before multiplier decrease
        [SerializeField] private int m_StageHandicap = 2;                       // Handicap is applied when stage resets is less or greater to this
        public AudioClip m_MultiplierDecreaseClip;                              // Sound to play when multiplier decreases
        public AudioClip m_LifeLostClip;                                        // Clip to play when a live has been lost
        public ParticleSystem m_MultiplierDecreaseParticles;                    // Root particle system to all decrease particles (longest system should be root)

        private float m_MultiplierStart = -1f;                      // Time multiplier increase interval started
        private float m_MultiplierInterval = 0f;                    // Interval of multiplier increase
        private int m_StageResets = 0;                              // Amount of times current stage has been reset
        private int m_RemainingLives = 0;                           // Lives remaining in current stage
        private ParticleSystem m_MultiplierParticles;               // Current particle system being played

        [Header("Packets")]
        [SerializeField] private DataPacket m_PacketPrefab;                         // Prefab for data packets
        [SerializeField] private float m_PacketSpace = 2f;                          // The space packet should have (avoid overlap)
        [SerializeField] private float m_MinPacketSpawnRadius = 4f;                 // Min radius from active wire packets should spawn
        [SerializeField] private float m_MaxPacketSpawnRadius = 15f;                // Max radius from active wire packets should spawn 
        [SerializeField, Range(0, 1)] public float m_BottomCircleCutoff = 0.7f;     // Cutoff from bottom of spawn circle (no packets will spawn in cutoff)
        [SerializeField, Range(0, 1)] public float m_TopCircleCutoff = 1f;          // Cutoff from top of spawn circle (no packets will spawn in cutoff)

        [SerializeField, Min(0)] private int m_MaxBonusSegments = 4;                // Max amount of packets that give bonus segments, resets when jumping

        public AudioClip m_PacketSpawnSound;                    // Spawn sound for an individual packet
        public AudioClip m_PacketClusterSpawnSound;             // Spawn sound for a cluster of packets
        public AudioSource m_PacketSpawnAudioSource;            // Audio source for packets spawning                          
        public AudioSource m_PacketAudioSource;                 // Audio source for packets (moves depending on players location)
        public ParticleSystem m_PacketCollectedParticles;       // Particle system to play when a packet is collected
        public AudioSource m_BonusSegmentsReachedSource;        // Audio source for reaching max bonus limit

        private ObjectPool<ParticleSystem> m_PacketCollectedSystems = new ObjectPool<ParticleSystem>();     // Pool of particle systems used for packet collection

        private MultiplierStageProperties m_ActiveMultiplierProperties;                 // Properties for current stage
        private ObjectPool<DataPacket> m_DataPackets = new ObjectPool<DataPacket>();    // Packets being managed
        private PacketStageProperties m_ActivePacketProperties;                         // Properties for current stage
        private int m_PacketSpawnsSinceLastCluster = 0;                                 // Amount of random packets spawn attempts since the last packet cluster
        private int m_PacketsCollectedSinceJump = 0;                                    // Amount of packets that has been collected since the last spark jump

        #if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private Text m_DebugText;      // Text for writing debug data
        #endif

        // If scoring is enabled
        public bool isRunning { get { return m_IsRunning; } }

        // Current score
        public float score { get { return m_Score; } }

        // Current multiplier
        public float multiplier { get { return m_Multiplier; } }

        // Multipliers current stage
        public int multiplierStage { get { return m_Stage; } }

        // Max multiplier stage
        public int maxMultiplierStage { get { return m_MultiplierStages; } }

        // Total multipler
        public float totalMultiplier { get { return m_Multiplier; } }

        // Time till next multiplier increase (1 if at max stage)
        public float multiplierProgress { get { return GetMultiplierProgress(); } }

        // Lives remaining before stage decrease
        public int remainingLives { get { return m_RemainingLives; } }

        // Space required for packets
        public float packetSpace { get { return m_PacketSpace; } }

        // Amount of packets active
        public int activePackets { get { return m_DataPackets.activeCount; } }

        private WireManager m_WireManager;              // Manager for wires
        private SparkJumper m_SparkJumper;              // Players spark jumper
        private float m_Score;                          // Players score
        private float m_Multiplier;                     // Players multiplier
        private int m_Stage;                            // Multiplier stage
        private Coroutine m_PacketSpawnRoutine;         // Coroutine for spawning packets

        // If tutorial settings should be used
        public bool tutorialMode { get; private set; }

        // The players companions voice
        private CompanionVoice companionVoice { get { return m_SparkJumper ? m_SparkJumper.companionVoice : null; } }

        void Update()
        {
            Profiler.BeginSample("ScoreManager.Tick", this);

            if (m_IsRunning)
            {
                AddScore(m_ScorePerSecond * Time.deltaTime);

                // Tick packets
                {
                    float step = Time.deltaTime;

                    for (int i = 0; i < m_DataPackets.activeCount; ++i)
                    {
                        DataPacket packet = m_DataPackets.GetObject(i);
                        packet.TickPacket(step);
                    }
                }
            }

            // Move multiplier system
            if (m_MultiplierParticles && m_MultiplierParticles.IsAlive(true))
            {
                if (m_SparkJumper && m_SparkJumper.companion)
                    m_MultiplierParticles.transform.position = m_SparkJumper.companion.transform.position;
            }

            UpdatePacketAudioSource();
            UpdatePacketParticles();

            Profiler.EndSample();

            #if UNITY_EDITOR
            // For testing
            if (m_IsRunning)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
                    IncreaseMultiplier();
                if (Input.GetKeyDown(KeyCode.DownArrow))
                    DecreaseMultiplier();
            }

            // Debug text
            if (m_DebugText)
            {
                m_DebugText.text = string.Format("Score: {0}\nMultiplier: {1}\nMultiplier Stage: {2}\nPackets Pool Size: {3}\nPackets Active: {4}\nRemaining Lives: {5}\nHandicap Active: {6}",
                    Mathf.FloorToInt(m_Score), m_Multiplier, m_Stage, m_DataPackets.Count, m_DataPackets.activeCount, m_RemainingLives, m_StageResets >= m_StageHandicap);
            }
            #endif
        }

        /// <summary>
        /// Initialize manager to work with wire manager
        /// </summary>
        /// <param name="wireManager">Wire manager</param>
        public void Initialize(WireManager wireManager)
        {
            m_WireManager = wireManager;
            m_SparkJumper = wireManager.sparkJumper;

            if (m_SparkJumper)
            {
                m_SparkJumper.OnJumpToSpark += JumpedToSpark;
            }

            m_Score = 0f;
            m_Multiplier = 1f;
            m_Stage = 0;

            m_ActivePacketProperties = GetPacketProperties(m_Stage);

            m_PacketSpawnsSinceLastCluster = 0;
        }

        /// <summary>
        /// Enables partial scoring functionality (for tutorial purposes)
        /// </summary>
        /// <param name="packetProps">Packet properties to use for tutorial mode</param>
        public void EnableTutorial(PacketStageProperties packetProps)
        {
            if (!m_IsRunning)
            {
                m_ActivePacketProperties = packetProps;

                tutorialMode = true;
                m_IsRunning = true;
            }
        }

        /// <summary>
        /// Enables scoring functionality
        /// </summary>
        /// <param name="reset">If properties should reset</param>
        public void EnableScoring(bool reset)
        {
            //if (m_IsRunning && !tutorialMode)
            //    return;

            m_IsRunning = true;
            tutorialMode = false;

            if (reset)
            {
                m_Score = 0f;
                m_Multiplier = 1f;
                m_Stage = 0;
                m_StageResets = 0;
                SetRemainingLives(m_StageLives);

                m_ActiveMultiplierProperties = GetMultiplierProperties(m_Stage);
                m_ActivePacketProperties = GetPacketProperties(m_Stage);

                // Need to notify listeners of changes
                {
                    if (OnMultiplierUpdated != null)
                        OnMultiplierUpdated.Invoke(m_Multiplier, m_Stage);
                }
            }

            ActivateMultiplierTick(reset);
            SetPacketGenerationEnabled(true);
        }

        /// <summary>
        /// Disables scoring functionality
        /// </summary>
        /// <param name="partial">If scoring is only partially disabled</param>
        public void DisableScoring(bool partial = false)
        {
            if (!m_IsRunning)
                return;

            SetPacketGenerationEnabled(false);

            //if (partial)
            //    CancelInvoke(m_TickAutoSpawnPacket);

            // We need this to be true for the new multiplier increase routine
            m_IsRunning = partial;
            tutorialMode = false;
        }

        /// <summary>
        /// Set if automatic packet generation is enabled
        /// </summary>
        /// <param name="enable">If to enable</param>
        public void SetPacketGenerationEnabled(bool enable)
        {
            if (!tutorialMode && m_GeneratingPackets != enable)
            {
                m_GeneratingPackets = enable;

                if (m_GeneratingPackets)
                {
                    if (m_PacketSpawnRoutine == null)
                        m_PacketSpawnRoutine = StartCoroutine(PacketSpawnRoutine());
                }
                else if (m_PacketSpawnRoutine != null)
                {
                    StopCoroutine(m_PacketSpawnRoutine);
                    m_PacketSpawnRoutine = null;
                }
            }
        }

        /// <summary>
        /// Increases the multiplier by amount of stages
        /// </summary>
        /// <param name="stages">Stages to increase by</param>
        public void IncreaseMultiplier(int stages = 1)
        {
            if (m_IsRunning && !tutorialMode)
                if (SetMultiplierStage(m_Stage + stages))
                    PlayMultiplierAesthetics(true);
        }

        /// <summary>
        /// Decreases the multiplier by amount of stages
        /// </summary>
        /// <param name="stages"></param>
        public void DecreaseMultiplier(int stages = 1)
        {
            if (!m_AllowDecrease)
                return;

            if (!m_IsRunning || tutorialMode)
                return;

            if (SetMultiplierStage(m_Stage - stages))
                PlayMultiplierAesthetics(false);

            // Reset the multiplier tick routine (resets regardless of if stage actually changed)
            ActivateMultiplierTick(true);
        }

        /// <summary>
        /// Sets the multiplier stage, updates multiplier value
        /// </summary>
        /// <param name="stage">Stage to set</param>
        /// <returns>If stage has changed</returns>
        private bool SetMultiplierStage(int stage)
        {
            stage = Mathf.Clamp(stage, 0, m_MultiplierStages);
            if (stage != m_Stage)
            {
                // Multiplier decrease might be disabled
                if (!m_AllowDecrease && stage < m_Stage)
                    return false;

                m_Stage = stage;
                m_Multiplier = 1 << m_Stage;

                m_StageResets = 0;
                SetRemainingLives(m_StageLives);

                // Update both multiplier and packet properties
                RefreshActiveStageProperties();

                if (OnMultiplierUpdated != null)
                    OnMultiplierUpdated.Invoke(m_Multiplier, m_Stage);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to decrease the multiplier (based on resets)
        /// </summary>
        /// <returns>If multiplier was decreased</returns>
        public bool TryDecreaseMultiplier()
        {
            if (!m_IsRunning)
                return false;

            ++m_StageResets;

            if (m_RemainingLives <= 0)
            {
                DecreaseMultiplier(1);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resets the multiplier stage
        /// </summary>
        /// <param name="resetHandicaps">If handicaps should also be reset</param>
        public void ResetMultiplier()
        {
            m_Multiplier = 1f;
            m_Stage = 0;
            m_StageResets = 0;
            SetRemainingLives(m_StageLives);
        }

        /// <summary>
        /// Adds amount scaled by total multiplier to current score
        /// </summary>
        /// <param name="amount">Amount to add</param>
        public void AddScore(float amount)
        {
            if (m_IsRunning && !tutorialMode)
                m_Score += amount * totalMultiplier;
        }

        /// <summary>
        /// Adds jump points to players score
        /// </summary>
        public void AwardJumpPoints()
        {
            AddScore(m_JumpScore * totalMultiplier);
        }

        /// <summary>
        /// Get current progress of automatic multiplier increase
        /// </summary>
        /// <returns>Progress of auto increase</returns>
        public float GetMultiplierProgress()
        {
            if (m_MultiplierInterval == -1)
                return 0f;

            if (m_Stage == m_MultiplierStages || m_MultiplierInterval <= 0f)
                return 1f;

            float end = m_MultiplierStart + m_MultiplierInterval;
            return 1f - ((end - Time.time) / m_MultiplierInterval);
        }

        /// <summary>
        /// Activates the auto multiplier tick routine
        /// </summary>
        /// <param name="reset">If active tick should be reset</param>
        private void ActivateMultiplierTick(bool reset = true)
        {
            if (m_IsRunning && !tutorialMode)
            {
                if (m_Stage >= m_MultiplierStages)
                    return;

                if (reset)
                    CancelInvoke(m_TickAutoMultiplierIncrease);
                else if (IsInvoking(m_TickAutoMultiplierIncrease))
                    return;

                MultiplierStageProperties mulStage = GetStageMultiplierProperties();
                float interval = mulStage.m_Duration;

                m_MultiplierStart = Time.time;
                m_MultiplierInterval = interval;

                Invoke(m_TickAutoMultiplierIncrease, interval);
            }
        }

        /// <summary>
        /// Disables the multiplier tick
        /// </summary>
        public void DisableMultiplierTick()
        {
            CancelInvoke(m_TickAutoMultiplierIncrease);
        }

        /// <summary>
        /// Notify that multiplier interval has elapsed
        /// </summary>
        private void TickAutoMultiplierIncrease()
        {
            if (m_IsRunning && !tutorialMode)
            {
                IncreaseMultiplier(1);

                // This will fall out if at max multiplier stage
                ActivateMultiplierTick();
            }
        }

        /// <summary>
        /// Generates a data packet randomly located in the world
        /// </summary>
        /// <param name="tryCluster">If a cluster of packets can possibly be spawned</param>
        /// <returns>Packet or null</returns>
        public DataPacket GenerateRandomPacket(bool tryCluster)
        {
            PacketStageProperties packetProps = GetStagePacketProperties();

            if (packetProps.m_Lifetime <= 0f)
                return null;

            // Try to spawn a cluster of packets if possible
            if (tryCluster && m_PacketSpawnsSinceLastCluster >= packetProps.m_ClusterRate)
            {
                bool cluster = packetProps.m_ClusterChance > 0f ? Random.Range(0f, 100f) < (packetProps.m_ClusterChance * 100f) : false;
                if (cluster)
                {
                    DataPacket packet = GeneratePacketCluster();
                    if (packet)
                        PlayPacketSpawnSound(true);

                    m_PacketSpawnsSinceLastCluster = 0;

                    return packet;
                }
            }

            Profiler.BeginSample("GenerateRandomPacket", this);

            const int maxAttempts = 5;
            Vector3 spawnCenter = m_WireManager.GetSpawnCircleCenter() + WireManager.WirePlane * (m_WireManager.segmentLength * packetProps.m_MinSpawnOffset);

            Vector3 position = Vector3.zero;
            bool success = false;

            // We don't want to loop to many times
            int attempts = 0;
            while (++attempts <= maxAttempts)
            {
                Vector2 circleOffset = Wires.GetRandomCircleOffset(m_MinPacketSpawnRadius,
                    m_MaxPacketSpawnRadius, m_BottomCircleCutoff, m_TopCircleCutoff);

                position = spawnCenter + new Vector3(circleOffset.x, circleOffset.y, 0f);

                // We expect to spawn far in front of wires
                success = m_WireManager.HasSpaceAtLocation(position, true);
                if (success)
                    break;
            }

            if (!success)
            {
                Debug.LogWarning(string.Format("Failed to generate packet after {0} attempts", maxAttempts), this);
                return null;
            }

            float speed = Random.Range(packetProps.m_MinSpeed, packetProps.m_MaxSpeed);

            Profiler.EndSample();

            ++m_PacketSpawnsSinceLastCluster;
            return GeneratePacket(position, speed, packetProps.m_Lifetime);
        }

        /// <summary>
        /// Generates a data packet, immediately activating it
        /// </summary>
        /// <param name="position">Position of the packet</param>
        /// <param name="speed">Speed of the packet</param>
        /// <param name="lifetime">Lifetime of the packet</param>
        /// <param name="forCluster">If packet is for a cluster</param>
        /// <returns>Packet or null</returns>
        public DataPacket GeneratePacket(Vector3 position, float speed, float lifetime, bool forCluster = false)
        {
            if (lifetime <= 0f)
                return null;

            DataPacket packet = GetPacket();
            if (!packet)
                return null;

            packet.Activate(position, speed, lifetime);

            if (m_ActivePacketProperties != null)
                packet.SetAnimationSpeed(m_ActivePacketProperties.m_AnimSpeed);

            if (!forCluster)
                PlayPacketSpawnSound(false);

            if (m_PacketAudioSource && !m_PacketAudioSource.isPlaying)
                m_PacketAudioSource.Play();

            return packet;
        }

        /// <summary>
        /// Generates a random cluster of packets
        /// </summary>
        /// <returns>Last packet generated or null</returns>
        private DataPacket GeneratePacketCluster()
        {
            PacketStageProperties packetProps = GetStagePacketProperties();

            Profiler.BeginSample("GeneratePacketCluster", this);

            const int maxAttempts = 5;
            Vector3 spawnCenter = m_WireManager.GetSpawnCircleCenter() + WireManager.WirePlane * (m_WireManager.segmentLength * packetProps.m_MinSpawnOffset);

            DataPacket packet = null;

            #if UNITY_EDITOR
            int packetsSpawned = 0;
            #endif

            int clusterSize = Random.Range(packetProps.m_MinPacketsPerCluster, packetProps.m_MaxPacketsPerCluster + 1);
            for (int i = 0; i < clusterSize; ++i)
            {
                Vector3 position = Vector3.zero;
                bool success = false;

                // We don't want to loop to many times
                int attempts = 0;
                while (++attempts <= maxAttempts)
                {
                    int randomSegmentOffset = Random.Range(-packetProps.m_ClusterSpawnRange, packetProps.m_ClusterSpawnRange + 1);
                    Vector3 planeOffset = WireManager.WirePlane * (m_WireManager.segmentLength * randomSegmentOffset);

                    Vector2 circleOffset = Wires.GetRandomCircleOffset(m_MinPacketSpawnRadius,
                        m_MaxPacketSpawnRadius, m_BottomCircleCutoff, m_TopCircleCutoff);

                    position = spawnCenter + planeOffset + new Vector3(circleOffset.x, circleOffset.y, 0f);

                    // We expect to spawn far in front of wires
                    success = m_WireManager.HasSpaceAtLocation(position, true);
                    if (success)
                        break;
                }

                if (!success)
                {
                    Debug.LogWarning(string.Format("Failed to generate packet after {0} attempts for cluster", maxAttempts), this);
                    continue;
                }

                float speed = Random.Range(packetProps.m_MinSpeed, packetProps.m_MaxSpeed);

                DataPacket newPacket = GeneratePacket(position, speed, packetProps.m_Lifetime, true);
                if (newPacket != null)
                    packet = newPacket;

                #if UNITY_EDITOR
                if (newPacket != null)
                    ++packetsSpawned;
                #endif
            }

            #if UNITY_EDITOR
            Debug.Log(string.Format("Packet Cluster Spawn Results - Cluster Size: {0}, Packets Spawned: {1}", clusterSize, packetsSpawned));
            #endif

            Profiler.EndSample();

            return packet;
        }

        /// <summary>
        /// Helper for getting a packet from the pool (spawns one if needed)
        /// </summary>
        /// <returns>Packet or null</returns>
        private DataPacket GetPacket()
        {
            if (m_DataPackets.canActivateObject)
                return m_DataPackets.ActivateObject();

            if (!m_PacketPrefab)
            {
                Debug.LogError("Unable to spawn data prefab as prefab is invalid", this);
                return null;
            }

            Vector3 position = m_WireManager ? m_WireManager.disabledSpot : Vector3.zero;
            DataPacket packet = Instantiate(m_PacketPrefab, position, Quaternion.identity);

            // Hook events
            {
                packet.OnCollected += PacketCollected;
                packet.OnSeekComplete += PacketSeekComplete;
                packet.OnExpired += PacketExpired;
            }

            m_DataPackets.Add(packet);
            m_DataPackets.ActivateObject();

            return packet;
        }

        /// <summary>
        /// Deactivates the given packet
        /// </summary>
        /// <param name="packet">Packet to deactivate</param>
        /// <param name="wasCollected">If packet was collected</param>
        private void DeactivatePacket(DataPacket packet, bool wasCollected)
        {
            packet.Deactivate();
            m_DataPackets.DeactivateObject(packet);

            if (m_DataPackets.activeCount == 0)
                if (m_PacketAudioSource)
                    m_PacketAudioSource.Stop();


            if (OnPacketDespawned != null)
                OnPacketDespawned.Invoke(packet, wasCollected);
        }

        /// <summary>
        /// Refreshes the active properties references based on current multiplier
        /// </summary>
        private void RefreshActiveStageProperties()
        {
            m_ActiveMultiplierProperties = GetMultiplierProperties(m_Stage);
            m_ActivePacketProperties = m_ActiveMultiplierProperties.m_PacketProperties;
        }

        /// <summary>
        /// Gets properties that relate to stage specified by index. This handles if index
        /// or properties is valid and will always return a valid properties instance
        /// </summary>
        /// <param name="index">Index of properties</param>
        /// <returns>Valid properties</returns>
        private MultiplierStageProperties GetMultiplierProperties(int index)
        {
            if (m_MultiplierStageProperties == null || m_MultiplierStageProperties.Length == 0)
                return new MultiplierStageProperties();

            // We use the latest properties if index is still out of range
            index = Mathf.Clamp(index, 0, m_MultiplierStageProperties.Length - 1);
            return m_MultiplierStageProperties[index];
        }

        /// <summary>
        /// Get multiplier properties for current multiplier stage
        /// </summary>
        /// <returns>Valid properties</returns>
        private MultiplierStageProperties GetStageMultiplierProperties()
        {
            if (m_ActiveMultiplierProperties != null)
                return m_ActiveMultiplierProperties;

            m_ActiveMultiplierProperties = GetMultiplierProperties(m_Stage);
            return m_ActiveMultiplierProperties;
        }

        /// <summary>
        /// Gets packet generation properties for index. Checks if properties exist for
        /// index, either creating a new set or getting best replacement if it doesn't
        /// </summary>
        /// <param name="index">Index of properties</param>
        /// <returns>Valid properties</returns>
        private PacketStageProperties GetPacketProperties(int index)
        {
            MultiplierStageProperties mulProps = GetMultiplierProperties(index);
            return mulProps.m_PacketProperties;
        }

        /// <summary>
        /// Get packet generation parameters for current multiplier stage
        /// </summary>
        /// <returns>Valid properties</returns>
        private PacketStageProperties GetStagePacketProperties()
        {
            if (m_ActivePacketProperties != null)
                return m_ActivePacketProperties;

            m_ActivePacketProperties = GetPacketProperties(m_Stage);
            return m_ActivePacketProperties;
        }

        /// <summary>
        /// Set the lives remaining for current stage
        /// </summary>
        /// <param name="lives">Lives remaining</param>
        private void SetRemainingLives(int lives)
        {
            lives = Mathf.Max(0, lives);

            // Always keep one life on lowest stage
            if (lives == 0 && m_Stage == 0)
                lives = 1;

            if (m_RemainingLives != lives)
            {
                m_RemainingLives = lives;

                if (m_MultiplierAudioSource && m_LifeLostClip)
                {
                    m_MultiplierAudioSource.clip = m_LifeLostClip;
                    m_MultiplierAudioSource.Play();
                }

                if (OnStageLivesUpdated != null)
                    OnStageLivesUpdated.Invoke(m_RemainingLives);
            }
        }

        /// <summary>
        /// Notify that player has collected given packet
        /// </summary>
        private void PacketCollected(DataPacket packet)
        {
            // Assuming packet is now chasing the player companion
            SpawnPacketCollectedParticles(packet);
        }

        /// <summary>
        /// Notify that packet has reached the companion
        /// </summary>
        /// <param name="packet"></param>
        private void PacketSeekComplete(DataPacket packet)
        {
            AddScore(m_PacketScore);

            // Extend wire of player if we can
            if (m_PacketsCollectedSinceJump < m_MaxBonusSegments)
            {
                ++m_PacketsCollectedSinceJump;
                bool nowLocked = m_PacketsCollectedSinceJump == m_MaxBonusSegments;

                if (nowLocked && m_BonusSegmentsReachedSource)
                    m_BonusSegmentsReachedSource.Play();

                PacketStageProperties packetProps = GetStagePacketProperties();
                if (m_WireManager)
                    m_WireManager.ExtendActiveWire(packetProps.m_BonusSegments, nowLocked);
            }

            // Allow packets trails to also disappear
            StartCoroutine(DelayPacketDeactivationRoutine(packet));

            CompanionVoice voice = companionVoice;
            if (voice)
                voice.PlayPacketCollectedDialogue();
        }

        /// <summary>
        /// Notify that given packet has expired
        /// </summary>
        private void PacketExpired(DataPacket packet)
        {
            DeactivatePacket(packet, false);
        }

        /// <summary>
        /// Delays the deactivation of the packet based on expected delay
        /// </summary>
        /// <param name="packet">Packet to deactivate</param>
        private IEnumerator DelayPacketDeactivationRoutine(DataPacket packet)
        {
            // Has possibly of changing expected delay,
            // so we must call it first for accurate time
            packet.DisableTrails();

            float delay = packet.expectedDelayTime;
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            DeactivatePacket(packet, true);
        }

        /// <summary>
        /// Routine for spawning packets
        /// </summary>
        private IEnumerator PacketSpawnRoutine()
        {
            while (m_IsRunning)
            {
                PacketStageProperties packetProps = GetStagePacketProperties();
                float delay = Random.Range(packetProps.m_MinSpawnInterval, packetProps.m_MaxSpawnInterval);

                yield return new WaitForSeconds(delay);

                GenerateRandomPacket(true);
            }
        }

        /// <summary>
        /// Gets active packet at index
        /// </summary>
        /// <param name="index">Index of packet</param>
        /// <returns>Active packet or null</returns>
        // TODO: This function only exists for HasSpaceAtLocation in WireManager
        public DataPacket GetActivePacket(int index)
        {
            if (index < m_DataPackets.activeCount)
                return m_DataPackets.GetObject(index);

            return null;
        }

        /// <summary>
        /// Plays aesthetics for when multiplier either increases/decreases
        /// </summary>
        /// <param name="increase">If to play increase aethetics</param>
        private void PlayMultiplierAesthetics(bool increase)
        {
            if (m_MultiplierAudioSource)
            {
                AudioClip clip = increase ? m_MultiplierIncreaseClip : m_MultiplierDecreaseClip;
                if (clip)
                {
                    m_MultiplierAudioSource.clip = clip;
                    m_MultiplierAudioSource.Play();
                }
            }

            m_MultiplierParticles = increase ? m_MultiplierIncreaseParticles : m_MultiplierDecreaseParticles;
            if (m_MultiplierParticles)
            {
                if (m_SparkJumper && m_SparkJumper.companion)
                    m_MultiplierParticles.transform.position = m_SparkJumper.companion.transform.position;

                m_MultiplierParticles.gameObject.SetActive(true);
                m_MultiplierParticles.Play(true);
            }

            CompanionVoice voice = companionVoice;
            if (voice)
            {
                if (increase)
                    voice.PlayMultiplierIncreaseDialogue();
                else
                    voice.PlayMultiplierDecreaseDialogue();
            }
        }

        /// <summary>
        /// Plays the spawn sound for a packet generating
        /// </summary>
        /// <param name="cluster">If cluster sound should be played instead</param>
        private void PlayPacketSpawnSound(bool cluster)
        {
            if (m_PacketSpawnAudioSource)
            {
                AudioClip clip = cluster ? m_PacketClusterSpawnSound : m_PacketSpawnSound;
                if (clip)
                {
                    m_PacketSpawnAudioSource.clip = clip;
                    m_PacketSpawnAudioSource.time = 0f;
                    m_PacketSpawnAudioSource.Play();
                }
            }
        }

        /// <summary>
        /// Updates the position of the packet audio source to match position of packet closest to players vicew
        /// </summary>
        // TODO: Could move along tick to prevent another for loop
        private void UpdatePacketAudioSource()
        {
            if (!m_WireManager || !m_WireManager.sparkJumper)
                return;

            if (m_PacketAudioSource)
            {
                // We only need to move if packets are active
                if (m_DataPackets.activeCount > 0)
                {
                    Vector3 center = m_WireManager.sparkJumper.GetPlayerPosition();

                    float closest = float.MaxValue;
                    DataPacket closestPacket = null;

                    // Move source to closest
                    for (int i = 0; i < m_DataPackets.activeCount; ++i)
                    {
                        DataPacket packet = m_DataPackets.GetObject(i);

                        float distance = (packet.transform.position - center).sqrMagnitude;

                        if (distance < closest)
                        {
                            closest = distance;
                            closestPacket = packet;
                        }
                    }

                    if (closestPacket)
                        m_PacketAudioSource.transform.position = closestPacket.transform.position;
                }
            }
        }

        /// <summary>
        /// Spawns a particle system for when a packet has been collected
        /// </summary>
        /// <param name="packet">Packet that was collected</param>
        private void SpawnPacketCollectedParticles(DataPacket packet)
        {
            if (!packet)
                return;

            ParticleSystem system = null;
            if (m_PacketCollectedSystems.canActivateObject)
            {
                system = m_PacketCollectedSystems.ActivateObject();
                system.transform.position = packet.transform.position;
            }
            else if (m_PacketCollectedParticles != null)
            {
                system = Instantiate(m_PacketCollectedParticles, packet.transform.position, Quaternion.identity);
                m_PacketCollectedSystems.Add(system);
                m_PacketCollectedSystems.ActivateObject();
            }
            else
            {
                return;
            }

            system.time = 0f;
            system.Play();
        }

        /// <summary>
        /// Updates which particles are active or inactive
        /// </summary>
        private void UpdatePacketParticles()
        {
            for (int i = 0; i < m_PacketCollectedSystems.activeCount; ++i)
            {
                ParticleSystem system = m_PacketCollectedSystems.GetObject(i);
                if (!system.IsAlive())
                {
                    m_PacketCollectedSystems.DeactivateObject(i);
                    --i;
                }
            }
        }

        /// <summary>
        /// Notify that player has jumped to another spark
        /// </summary>
        /// <param name="spark">Spark player jumped to</param>
        /// <param name="finished">If jump has finished or just started</param>
        private void JumpedToSpark(Spark spark, bool finished)
        {
            if (finished)
                m_PacketsCollectedSinceJump = 0;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Called by wire manager to draw debug gizmos
        /// </summary>
        public void DrawDebugGizmos(Vector3 center)
        {
            Gizmos.color = Color.magenta;
            Wires.DrawSpawnArea(m_MinPacketSpawnRadius, m_MaxPacketSpawnRadius, center);

            Gizmos.color = Color.white;
            Wires.DrawCutoffGizmo(m_BottomCircleCutoff, center, m_MaxPacketSpawnRadius, true);
            Wires.DrawCutoffGizmo(m_TopCircleCutoff, center, m_MaxPacketSpawnRadius, false);
        }
#endif
    }
}
