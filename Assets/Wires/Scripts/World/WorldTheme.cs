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

        private int m_MultiplierStage;          // Stage of multiplier

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
            m_MultiplierStage = 0;

            if (m_WireManager)
            {
                // We listen for when multipler has changed
                ScoreManager scoreManager = m_WireManager.scoreManager;
                if (scoreManager)
                {
                    scoreManager.OnMultiplierUpdated += MultiplierUpdated;
                    m_MultiplierStage = scoreManager.multiplierStage;
                }

                // We listen for when the player jumps to handle aesthetic changes
                SparkJumper jumper = m_WireManager.sparkJumper;
                if (jumper != null)
                {
                    jumper.OnJumpToSpark += JumpToSpark;

                    if (jumper.spark)
                    {
                        m_CurrentWire = jumper.spark.GetWire();

                        m_WorldAesthetics.SetActiveAesthetics(m_CurrentWire);

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
            }
        }

        /// <summary>
        /// Notify from players spark jumper when jumping between wires
        /// </summary>
        private void JumpToSpark(Spark spark, bool finished)
        {
            if (finished)
            {
                StopCoroutine("BlendThemesRoutine");
                BlendThemes(1f);        
            }
            else
            {
                m_CurrentWire = spark.GetWire();

                m_WorldAesthetics.SetPendingAethetics(m_CurrentWire);

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
        /// Notify from score manager that players multiplier has changed
        /// </summary>
        /// <param name="multiplier"></param>
        /// <param name="stage"></param>
        private void MultiplierUpdated(float multiplier, int stage)
        {
            m_MultiplierStage = stage;

            if (m_CurrentWire)
            {
                WireFactory factory = m_CurrentWire.factory;
                if (factory)
                    m_WorldMusic.SetActiveMusic(factory.GetMusic(m_MultiplierStage));
            } 
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
