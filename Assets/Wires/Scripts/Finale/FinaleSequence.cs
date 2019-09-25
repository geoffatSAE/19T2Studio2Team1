using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Handles the sequence of the finale in Wires
    /// </summary>
    public class FinaleSequence : MonoBehaviour
    {
        // TODO: Make properties private

        /// <summary>
        /// Delegate for when the finale sequence has finished
        /// </summary>
        public delegate void SequenceFinished();

        public SequenceFinished OnSequenceFinished;     // Event for when finale is finished

        [Header("Sequence")]
        [Min(0f)] public float m_Duration = 15f;                // Duration of the finale
        [Min(0f)] public float m_AutoControlStart = 7.5f;       // At what point we take control of the player
        public bool m_AutoDisable = false;                      // If sequence automatically disables itself (audio)

        [Header("Aesthetics")]
        [Range(0f, 1f)] public float m_FinalBorderAlpha = 0.5f;     // Final alpha of the outer border when finale is done
        private float m_StartBorderAlpha = 0.1f;                    // Value of borders alpha when we started

        [Header("Generation|Fixed")]
        public int m_MinSegments = 18;                              // Min segments per during finale
        public int m_MaxSegments = 22;                              // Max segments per during finale
        public int m_SpawnSegmentOffset = 10;                       // Offset from current segment to spawn wires during finale
        [Min(0)] public int m_SpawnSegmentRange = 3;                // Range from offset to spawn wires (between -Value and Value)

        public float m_InnerSpawnRadius = 3f;                       // Inner radius of spawn circle during finale      
        public float m_OuterSpawnRadius = 20f;                      // Outer radius of spawn circle during finale
        [Range(0, 1)] public float m_BottomCircleCutoff = 0.7f;     // Cutoff from bottom of spawn circle during finale
        [Range(0, 1)] public float m_TopCircleCutoff = 1f;          // Cutoff from top of spawn circle during finale
        public int m_MaxWiresAtOnce = 20;                           // Max wires that can be active at once during finale

        [Header("Generation|Dynamic")]
        public float m_StartMinSpawnInterval = 2f;                  // Min spawn interval of wires at the start of the finale
        public float m_StartMaxSpawnInterval = 3f;                  // Max spawn interval of wires at the start of the finale
        public float m_EndMinSpawnInterval = 1f;                    // Min spawn interval of wires at the end of the finale           
        public float m_EndMaxSpawnInterval = 2f;                    // Max spawn interval of wires at the end of the finale

        public float m_StartSparkSpeed = 1f;                        // Speed of the sparks at the start of the finale
        public float m_EndSparkSpeed = 2f;                          // Speed of the sparks at the end of the finale

        public float m_StartJumpTime = 0.75f;                       // Jump time for player at the start of the finale
        public float m_EndJumpTime = 0.5f;                          // Jump time for player at the end of the finale

        [Header("Audio")]
        [SerializeField] private AudioSource m_BuildupSource;                       // Source of buildup
        [SerializeField, Range(0f, 3f)] private float m_StartingPitch = 1f;         // Starting pitch of buildup sound
        [SerializeField, Range(0f, 3f)] private float m_EndingPitch = 2f;           // Ending pitch of buildup sound

        private WireManager m_WireManager;                      // Wire manager to manipulate
        private WorldTheme m_WorldTheme;                        // World theme to manipulate
        private float m_StartTime = -1f;                        // Time at which finale was activated
        private WireStageProperties m_WireProperties = null;    // Wire generation properties we set

        void Awake()
        {
            // Must be manually activated
            enabled = false;
        }

        void Update()
        {
            if (m_StartTime >= 0f)
            {
                float end = m_StartTime + m_Duration;
                float remaining = end - Time.time;
                float alpha = remaining / m_Duration;

                alpha = Mathf.Clamp01(1f - alpha);

                if (m_WireManager && m_WireManager.sparkJumper)
                {
                    SparkJumper sparkJumper = m_WireManager.sparkJumper;

                    // Remaining is inversed, so we check if its greater
                    bool autoControl = remaining >= m_AutoControlStart;

                    if (autoControl != sparkJumper.m_ControlsEnabled)
                        if (sparkJumper.companionVoice)
                            sparkJumper.companionVoice.PlayFinaleCheerDialogue();

                    sparkJumper.m_ControlsEnabled = autoControl;
                    sparkJumper.m_JumpTime = m_WireProperties.m_JumpTime;
                }

                InterpolateAesthetics(alpha);
                InterpolateStageProps(alpha);
                InterpolateBuildup(alpha);

                if (alpha >= 1f)
                    InternalFinished(false);
            }
        }

        void OnDisable()
        {
            if (m_BuildupSource)
                m_BuildupSource.Stop();

            if (m_StartTime >= 0f)
                InternalFinished(false);
        }

        /// <summary>
        /// Activates the finale sequence. This will override the current stage
        /// </summary>
        /// <param name="wireManager">Wire manager to control</param>
        /// <param name="worldTheme">World theme to control</param>
        public void Activate(WireManager wireManager, WorldTheme worldTheme)
        {
            if (m_StartTime < 0f)
            {
                if (!wireManager)
                {
                    Debug.Log("Invalid WireManager passed to Activate()", this);
                    return;
                }

                if (!worldTheme)
                {
                    Debug.Log("Invalid WorldTheme passed to Activate()", this);
                    return;
                }

                m_WireManager = wireManager;
                m_WireManager.m_DriftingEnabled = false;

                m_WorldTheme = worldTheme;
                m_StartBorderAlpha = m_WorldTheme.worldAesthetics.borderAlpha;

                // Setup custom stage
                GenerateStageProperties();
                m_WireManager.OverrideStageProperties(m_WireProperties);
                m_WireManager.ResetSpawnWireTick();

                if (m_BuildupSource)
                {
                    m_BuildupSource.pitch = m_StartingPitch;
                    m_BuildupSource.loop = true;
                    m_BuildupSource.Play();
                }

                SparkJumper sparkJumper = m_WireManager.sparkJumper;
                if (sparkJumper)
                {
                    if (sparkJumper.companion)
                        sparkJumper.companion.SetBoostModeEnabled(true);

                    if (sparkJumper.companionVoice)
                        sparkJumper.companionVoice.PlayFinaleDialogue();                
                }

                m_StartTime = Time.time;
                enabled = true;
            }
        }

        /// <summary>
        /// Deactivates and cancels the finish sequence
        /// </summary>
        public void Deactivate()
        {
            InternalFinished(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="finished"></param>
        private void InternalFinished(bool finished)
        {
            if (m_StartTime >= 0f)
            {
                InterpolateAesthetics(1f);
                InterpolateStageProps(1f);
                InterpolateBuildup(1f);

                SparkJumper sparkJumper = m_WireManager.sparkJumper;
                if (sparkJumper)
                {
                    if (sparkJumper.companion)
                        sparkJumper.companion.SetBoostModeEnabled(false);
                }

                m_WireManager = null;
                m_WorldTheme = null;
                m_StartTime = -1f;

                if (OnSequenceFinished != null)
                    OnSequenceFinished.Invoke();

                if (m_AutoDisable)
                    enabled = false;
            }
        }

        /// <summary>
        /// Generates the wire stage properties used to override managers stage
        /// </summary>
        private void GenerateStageProperties()
        {
            m_WireProperties = new WireStageProperties();
            m_WireProperties.m_MinSegments = m_MinSegments;
            m_WireProperties.m_MaxSegments = m_MaxSegments;
            m_WireProperties.m_SpawnSegmentOffset = m_SpawnSegmentOffset;
            m_WireProperties.m_SpawnSegmentRange = m_SpawnSegmentRange;

            m_WireProperties.m_InnerSpawnRadius = m_InnerSpawnRadius;
            m_WireProperties.m_OuterSpawnRadius = m_OuterSpawnRadius;
            m_WireProperties.m_BottomCircleCutoff = m_BottomCircleCutoff;
            m_WireProperties.m_TopCircleCutoff = m_TopCircleCutoff;
            m_WireProperties.m_MaxWiresAtOnce = m_MaxWiresAtOnce;

            m_WireProperties.m_SparkSpawnSegmentDelay = 0;
            m_WireProperties.m_DefectiveWireChance = 0f;

            InterpolateStageProps(0f);
        }

        /// <summary>
        /// Interpolates the aesthetics of the world we change during finale
        /// </summary>
        /// <param name="alpha">Alpha of interpolation</param>
        private void InterpolateAesthetics(float alpha)
        {
            if (m_WorldTheme)
            {
                m_WorldTheme.worldAesthetics.borderAlpha = Mathf.Lerp(m_StartBorderAlpha, m_FinalBorderAlpha, alpha);
            }
        }

        /// <summary>
        /// Interpolates the properties of the finales custom stage
        /// </summary>
        /// <param name="alpha">Alpha of interpolation</param>
        private void InterpolateStageProps(float alpha)
        {
            if (m_WireProperties != null)
            {
                m_WireProperties.m_MinSpawnInterval = Mathf.Lerp(m_StartMinSpawnInterval, m_EndMinSpawnInterval, alpha);
                m_WireProperties.m_MaxSpawnInterval = Mathf.Lerp(m_StartMaxSpawnInterval, m_EndMaxSpawnInterval, alpha);
                m_WireProperties.m_SparkSpeed = Mathf.Lerp(m_StartSparkSpeed, m_EndSparkSpeed, alpha);
                m_WireProperties.m_JumpTime = Mathf.Lerp(m_StartJumpTime, m_EndJumpTime, alpha);
            }
        }

        /// <summary>
        /// Interpolates the pitch of the build up sound
        /// </summary>
        /// <param name="alpha">Alpha of interpolation</param>
        private void InterpolateBuildup(float alpha)
        {
            if (m_BuildupSource)
            {
                float pitch = Mathf.Lerp(m_StartingPitch, m_EndingPitch, alpha);
                m_BuildupSource.pitch = pitch;
            }
        }
    }
}
