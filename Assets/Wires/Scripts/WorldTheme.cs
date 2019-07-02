using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Manages the theme of the world based on wire player is on
    /// </summary>
    [RequireComponent(typeof(WorldMusic), typeof(WorldColor))]
    public class WorldTheme : MonoBehaviour
    {
        private WireManager m_WireManager;      // Wire manager we associate with
        private WorldMusic m_WorldMusic;        // Handler for games music
        private WorldColor m_WorldColor;        // Handler for games color
        private Wire m_From;                    // Wire we are travelling from
        private Wire m_To;                      // Wire we are travelling to

        void Awake()
        {
            m_WorldMusic = GetComponent<WorldMusic>();
            m_WorldColor = GetComponent<WorldColor>();
        }

        /// <summary>
        /// Initializes the world theme
        /// </summary>
        /// <param name="wireManager">Manager to work with</param>
        public void Initialize(WireManager wireManager)
        {
            m_WireManager = wireManager;

            if (m_WireManager)
            {
                // We listen for when the player jumps to handle aesthetic changes
                SparkJumper jumper = m_WireManager.sparkJumper;
                if (jumper != null)
                {
                    jumper.OnJumpToSpark += JumpToSpark;

                    if (jumper.spark)
                    {
                        Wire wire = jumper.spark.GetWire();
                        WireFactory factory = wire.factory;
                        if (factory)
                        {
                            m_WorldMusic.SetActiveMusic(factory.music);
                            m_WorldColor.SetActiveColor(factory.skyboxColor);

                            // Instant blend
                            BlendThemes(1f);
                        }

                        m_To = wire;
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
                m_From = m_To;
                m_To = spark.GetWire();

                WireFactory factory = m_To.factory;
                if (factory)
                {
                    m_WorldMusic.SetActiveMusic(factory.music);
                    m_WorldColor.SetActiveColor(factory.skyboxColor);

                    StartCoroutine(BlendThemesRoutine(m_WireManager.sparkJumper));
                }

                if (m_WireManager)
                    StartCoroutine(BlendThemesRoutine(m_WireManager.sparkJumper));
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
