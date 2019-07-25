using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Manages the theme of the world based on wire player is on
    /// </summary>
    [RequireComponent(typeof(WorldMusic), typeof(WorldColor), typeof(WorldAesthetics))]
    public class WorldTheme : MonoBehaviour
    {
        private WireManager m_WireManager;              // Wire manager we associate with
        private WorldMusic m_WorldMusic;                // Handler for games music
        private WorldColor m_WorldColor;                // Handler for games color
        private WorldAesthetics m_WorldAesthetics;      // Handler for games aesthetics
        private Wire m_CurrentWire;                     // Current wire player is on

        private bool m_IsJumping;               // If player is jumping
        private bool m_IsDrifting;              // If player is drifting
        private int m_MultiplierStage;          // Stage of multiplier
        private bool m_BoostActive;             // If boost is active

        void Awake()
        {
            m_WorldMusic = GetComponent<WorldMusic>();
            m_WorldColor = GetComponent<WorldColor>();
            m_WorldAesthetics = GetComponent<WorldAesthetics>();
        }

        /// <summary>
        /// Initializes the world theme
        /// </summary>
        /// <param name="wireManager">Manager to work with</param>
        public void Initialize(WireManager wireManager)
        {
            m_WireManager = wireManager;
            m_IsJumping = false;
            m_IsDrifting = false;
            m_MultiplierStage = 0;
            m_BoostActive = false;

            if (m_WireManager)
            {
                // We listen for when multipler has changed
                ScoreManager scoreManager = m_WireManager.scoreManager;
                if (scoreManager)
                {
                    scoreManager.OnMultiplierUpdated += MultiplierUpdated;
                    scoreManager.OnBoostModeUpdated += BoostModeUpdated;

                    m_MultiplierStage = scoreManager.multiplierStage;
                    m_BoostActive = scoreManager.boostActive;
                }

                // We listen for when the player jumps to handle aesthetic changes
                SparkJumper jumper = m_WireManager.sparkJumper;
                if (jumper != null)
                {
                    jumper.OnJumpToSpark += JumpToSpark;
                    jumper.OnDriftingUpdated += DriftingUpdated;

                    if (jumper.spark)
                    {
                        m_CurrentWire = jumper.spark.GetWire();

                        m_WorldAesthetics.SetActiveAesthetics(m_CurrentWire);
                        m_WorldAesthetics.SetIntensity(m_MultiplierStage);

                        WireFactory factory = m_CurrentWire.factory;
                        if (factory)
                        {
                            m_WorldMusic.SetPendingMusic(factory.GetMusic(m_MultiplierStage));
                            m_WorldColor.SetActiveColor(factory.skyboxColor);
                        }

                        // Instant blend
                        BlendThemes(1f);
                    }
                }

                m_WorldAesthetics.SetBoostParticlesEnabled(m_BoostActive, m_WorldAesthetics.m_BoostParticlesSpeed);
            }
        }

        /// <summary>
        /// Notify from players spark jumper when jumping between wires
        /// </summary>
        private void JumpToSpark(Spark spark, bool finished)
        {
            m_IsJumping = !finished;

            if (finished)
            {
                // Drifting can disable the boost particles even while active
                if (m_BoostActive)
                    m_WorldAesthetics.SetBoostParticlesEnabled(true, m_WorldAesthetics.m_BoostParticlesSpeed);

                StopCoroutine("BlendThemesRoutine");
                BlendThemes(1f);        
            }
            else
            {
                m_CurrentWire = spark.GetWire();

                m_WorldAesthetics.SetPendingAethetics(m_CurrentWire);
                m_WorldAesthetics.SetBoostParticlesEnabled(false, m_WorldAesthetics.m_BoostDissapateSpeed);

                WireFactory factory = m_CurrentWire.factory;
                if (factory)
                {
                    m_WorldMusic.SetPendingMusic(factory.GetMusic(m_MultiplierStage));
                    m_WorldColor.SetActiveColor(factory.skyboxColor);
                }

                if (m_WireManager)
                    StartCoroutine(BlendThemesRoutine(m_WireManager.sparkJumper));
            }
        }

        /// <summary>
        /// Notify from player that they have either started/stopped drifting
        /// </summary>
        /// <param name="isEnabled"></param>
        private void DriftingUpdated(bool isEnabled)
        {
            m_IsDrifting = isEnabled;

            m_WorldColor.SetGrayscaleEnabled(isEnabled);

            if (isEnabled)
            {
                m_WorldAesthetics.SetWarningSignEnabled(false);
                m_WorldAesthetics.SetBoostParticlesEnabled(false, m_WorldAesthetics.m_BoostDissapateSpeed);
            }
        }
        
        /// <summary>
        /// Notify from score manager that players multiplier has changed
        /// </summary>
        private void MultiplierUpdated(float multiplier, int stage)
        {
            m_MultiplierStage = stage;

            if (m_CurrentWire)
            {
                WireFactory factory = m_CurrentWire.factory;
                if (factory)
                    m_WorldMusic.SetActiveMusic(factory.GetMusic(m_MultiplierStage));

                m_WorldAesthetics.SetIntensity(m_MultiplierStage);
            } 
        }

        /// <summary>
        /// Notify that boost mode has been switched on or off
        /// </summary>
        /// <param name="active">If boost is active</param>
        private void BoostModeUpdated(bool active)
        {
            m_BoostActive = active;

            if (!m_IsJumping && !m_IsDrifting)
                m_WorldAesthetics.SetBoostParticlesEnabled(active, m_WorldAesthetics.m_BoostParticlesSpeed);          
        }

        /// <summary>
        /// Blends all aesthetics
        /// </summary>
        /// <param name="alpha">Alpha between 0 and 1</param>
        private void BlendThemes(float alpha)
        {
            m_WorldMusic.BlendMusic(alpha);
            m_WorldColor.BlendColors(alpha);
            m_WorldAesthetics.BlendAesthetics(alpha);
        }

        /// <summary>
        /// Routine for blending themes of wires
        /// </summary>
        /// <param name="jumper">Players spark jumper</param>
        private IEnumerator BlendThemesRoutine(SparkJumper jumper)
        {
            if (!jumper)
                yield break;

            while (jumper.isJumping)
            {
                BlendThemes(jumper.jumpProgress);
                yield return null;
            }
        }
    }
}
