using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace TO5.Wires
{
    /// <summary>
    /// Game mode for the arcade version of wires
    /// </summary>
    public class WiresArcade : WiresGameMode
    {
        [Header("Arcade")]
        [SerializeField] private float m_GameTime = 480f;       // How long the game lasts for (in seconds)

        private Wire m_FinalWire;           // The final wire (exit wire)

        [Header("Tutorial")]
        [SerializeField] private bool m_EnableTutorial = true;                  // If tutorial is enabled (at start of game)
        [SerializeField] private int m_TutorialInitialSegments = 20;            // Segments of the initial wire in tutorial mode
        [SerializeField] private int m_TutorialWireSegments = 20;               // Segments per wire in tutorial mode
        [SerializeField] private float m_TutorialWireRadius = 5f;               // Spawn radius of wires in tutorial mode
        [SerializeField] private float m_TutorialSparkSpeed = 1f;               // Spark speed in tutorial mode
        [SerializeField] private float m_TutorialJumpTime = 1.5f;               // Jump time in tutorial mode
        [SerializeField] private Canvas m_TutorialDisplay;                      // HUD to display during tutorial mode

        private bool m_TutorialActive = false;              // If tutorial is active

        #if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private Text m_DebugText;      // Text for writing debug data
        #endif

        #if UNITY_EDITOR
        void Update()
        {
            if (m_GameStart != -1f)
            {
                if (m_DebugText)
                {
                    int playersSegment = m_WireManager.GetPositionSegment(WireManager.WirePlane * m_WireManager.sparkJumper.wireDistanceTravelled);

                    m_DebugText.text = string.Format("Game Time: {0}\nSegments Travelled: {1}\nPlayer SegmentsTravelled: {2}",
                        Mathf.FloorToInt(Time.time - m_GameStart), m_WireManager.GetJumpersSegment(), playersSegment);        
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

            m_GameStart = Time.time;
        }

        // WireGameMode interface
        protected override void EndGame()
        {
            StopCoroutine("GameTimerRoutine");
            StartCoroutine(SpawnEndingWireRoutine());
        }

        /// <summary>
        /// Starts game in tutorial mode
        /// </summary>
        private void StartTutorial()
        {
            m_WireManager.PlayerJumpedOffWire += TutorialJumpedOffWire;

            // Custom properties (we can ignore intervals as we will handle it)
            WireStageProperties tutorialProps = new WireStageProperties();
            tutorialProps.m_InnerSpawnRadius = m_TutorialWireRadius;
            tutorialProps.m_OuterSpawnRadius = m_TutorialWireRadius;
            tutorialProps.m_BottomCircleCutoff = 0f;
            tutorialProps.m_MinSegments = m_TutorialWireSegments;
            tutorialProps.m_MaxSegments = m_TutorialWireSegments;
            tutorialProps.m_SpawnSegmentRange = 0;
            tutorialProps.m_DefectiveWireChance = 0f;
            tutorialProps.m_SparkSpeed = m_TutorialSparkSpeed;
            tutorialProps.m_JumpTime = m_TutorialJumpTime;

            if (m_WireManager.StartTutorial(tutorialProps, m_TutorialInitialSegments))
            {
                // Initial wire to spawn for player (others will spawn if they failed to jump of current one)
                StartCoroutine(TutorialWireSpawnRoutine());

                if (m_TutorialDisplay)
                    m_TutorialDisplay.gameObject.SetActive(true);

                SparkJumper sparkJumper = m_WireManager.sparkJumper;             
                if (sparkJumper.m_Companion)
                    sparkJumper.m_Companion.SetRenderHUD(false);

                if (sparkJumper.m_ScreenFade)
                    sparkJumper.m_ScreenFade.FadeIn();

                m_TutorialActive = true;
            }
        }

        /// <summary>
        /// Cleans up any values set for tutorial mode
        /// </summary>
        private void EndTutorial()
        {
            if (m_TutorialActive)
            {
                m_WireManager.PlayerJumpedOffWire -= TutorialJumpedOffWire;

                if (m_TutorialDisplay)
                    m_TutorialDisplay.gameObject.SetActive(false);

                SparkJumper sparkJumper = m_WireManager.sparkJumper;
                if (sparkJumper.m_Companion)
                    sparkJumper.m_Companion.SetRenderHUD(true);

                if (sparkJumper.m_ScreenFade)
                    sparkJumper.m_ScreenFade.ClearFade();

                m_TutorialActive = false;

                StartArcade();
            }
        }
        
        /// <summary>
        /// Starts arcade mode
        /// </summary>
        private void StartArcade()
        {
            m_WireManager.StartWires();
            StartCoroutine(GameTimerRoutine());
        }

        /// <summary>
        /// Skips the tutorial (if active)
        /// </summary>
        public void SkipTutorial()
        {
            if (m_TutorialActive)
            {
                StopCoroutine("TutorialWireSpawnRoutine");
                EndTutorial();
            }
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
        /// Routine for generating initial wire player can jump to
        /// </summary>
        private IEnumerator TutorialWireSpawnRoutine()
        {
            yield return new WaitForSegment(m_WireManager, m_TutorialWireSegments / 2);
            m_WireManager.GenerateRandomWire(true);
        }

        /// <summary>
        /// Routine for generating the final wire the player can jump onto
        /// </summary>
        private IEnumerator SpawnEndingWireRoutine()
        {
            while (true)
            {
                m_FinalWire = m_WireManager.GenerateRandomFixedWire(10000, true, false);
                if (!m_FinalWire)
                    yield return null;
                else
                    break;
            }

            // We need to listen for when player actually jumps to this wire
            if (m_WireManager.sparkJumper)
                m_WireManager.sparkJumper.OnJumpToSpark += FinaleJumpToSpark;

            m_WireManager.SetWireGenerationEnabled(false);

            // TODO: Disable packet generation
            // if (m_WireManager.scoreManager)
            //    m_WireManager.scoreManager
        }

        /// <summary>
        /// Routine for handling the end game (displaying score and exiting)
        /// </summary>
        private IEnumerator DisplayStatsAndExitRoutine()
        {
            if (m_WireManager.scoreManager)
                m_WireManager.scoreManager.DisableScoring();

            SparkJumper sparkJumper = m_WireManager.sparkJumper;
            if (sparkJumper)
            {
                sparkJumper.m_JumpingEnabled = false;
                sparkJumper.OnJumpToSpark -= FinaleJumpToSpark;

                // TODO: Display score and whatnot
            }

            yield return new WaitForSeconds(5f);

            if (sparkJumper && sparkJumper.m_ScreenFade)
            {
                ScreenFade screenFade = sparkJumper.m_ScreenFade;
                screenFade.OnFadeFinished += FinaleFadeFinished;
                screenFade.FadeOut();
            }
            else
            {
                // Pretent screen instantly faded out
                FinaleFadeFinished(1f);
            }
        }


        /// <summary>
        /// Notify that player has jumped off a wire during the tutorial
        /// </summary>
        /// <param name="failed">If player failed to jump off wire</param>
        private void TutorialJumpedOffWire(bool failed)
        {
            if (!failed)
                EndTutorial();
            else
                m_WireManager.GenerateRandomWire(true);
        }

        /// <summary>
        /// Notify that player has jumped to a new spark during end game
        /// </summary>
        /// <param name="spark">Spark player jumped to</param>
        /// <param name="finished">If jump just finished</param>
        private void FinaleJumpToSpark(Spark spark, bool finished)
        {
            if (m_FinalWire && spark.GetWire() == m_FinalWire)
                StartCoroutine(DisplayStatsAndExitRoutine());
        }

        /// <summary>
        /// Notify that screen fade out has finished
        /// </summary>
        /// <param name="endAlpha">Last alpha of screen fade</param>
        private void FinaleFadeFinished(float endAlpha)
        {
            // for now
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
