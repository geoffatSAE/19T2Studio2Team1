using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TO5.Wires
{
    /// <summary>
    /// Game mode for the arcade version of wires
    /// </summary>
    public class WiresArcade : WiresGameMode
    {
        [Header("Arcade")]
        [SerializeField] private float m_GameTime = 480f;       // How long the game lasts for (in seconds)

        [Header("Tutorial")]
        [SerializeField] private bool m_EnableTutorial = true;                                              // If tutorial is enabled (at start of game)
        [SerializeField] private WireStageProperties m_TutorialSettings = new WireStageProperties();        // Settings for tutorials

        #if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private Text m_DebugText;      // Text for writing debug data
#endif

        private bool m_TutActive = false;

        #if UNITY_EDITOR
        void Update()
        {
            if (true)//m_GameStart != -1f)
            {
                if (m_DebugText)
                {
                    int playersSegment = m_WireManager.GetPositionSegment(WireManager.WirePlane * m_WireManager.sparkJumper.wireDistanceTravelled);

                    //m_DebugText.text = string.Format("Game Time: {0}\nSegments Travelled: {1}\nPlayer SegmentsTravelled: {2}\nGame Can Finish: {3}\nGame Finished: {4}",
                    //    Mathf.FloorToInt(Time.time - m_GameStart), m_WireManager.GetJumpersSegment(), playersSegment, m_GameCanFinish, m_GameFinished);        

                    m_DebugText.text = string.Format("Tutorial Active: {0}", m_TutActive);
                }
            }
        }
        #endif

        // WireGameMode interface
        protected override void StartGame()
        {          
            if (m_EnableTutorial)
                StartTutorial();
            else
                StartArcade();

            if (m_WorldTheme)
                m_WorldTheme.Initialize(m_WireManager);
        }

        // WireGameMode interface
        protected override void EndGame()
        {
            base.EndGame();

            StopCoroutine("GameTimerRoutine");
        }

        private void StartTutorial()
        {
            m_WireManager.PlayerJumpedOffWire += TutorialJumpedOffWire;
            m_WireManager.StartWiresManual(10, m_TutorialSettings);

            m_TutActive = true;
            m_WireManager.m_Tutorial = true;
        }

        private void EndTutorial()
        {
            m_WireManager.m_Tutorial = false;

            m_WireManager.PlayerJumpedOffWire -= TutorialJumpedOffWire;
            StartArcade();

            m_TutActive = false;
        }

        private void StartArcade()
        {
            if (!m_WireManager.isRunning)
            {
                m_WireManager.StartWires();
            }
            else
            {
                ScoreManager scoreManager = m_WireManager.scoreManager;
                if (scoreManager)
                    scoreManager.EnableScoring(true);

                m_WireManager.RefreshStageProperties();
            }

            StartCoroutine(GameTimerRoutine());
        }

        /// <summary>
        /// Routine for handling the games timer
        /// </summary>
        private IEnumerator GameTimerRoutine()
        {
            yield return new WaitForSeconds(m_GameTime);
            EndGame();
        }

        /// <summary>
        /// Notify that player has jumped off a wire during the tutorial
        /// </summary>
        /// <param name="failed">If player failed to jump off wire</param>
        private void TutorialJumpedOffWire(bool failed)
        {
            if (!failed)
                EndTutorial();
        }
    }
}
