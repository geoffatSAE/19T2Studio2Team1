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
        /// <summary>
        /// Steps of the tutorial
        /// </summary>
        private enum TutorialStep
        {
            None,
            Jumping,        
            Packets,
            Extension
        }

        [Header("Arcade")]
        [SerializeField] private float m_ArcadeLength = 480f;                                                               // How long the game lasts for (in seconds)
        [SerializeField] private float m_PostArcadeLength = 10f;                                                            // How long to display game stats for before exiting

        private float m_StartingDistance = 0f;          // Distance player had already travelled at the start of the game

        [Header("Finale")]
        [SerializeField] private FinaleSequence m_FinaleSequence;                                                           // Script for handling the finale
        [SerializeField] private bool m_SkipFinale = false;                                                                 // If finale sequence should be skipped
        [SerializeField] private FireworksHandler m_Fireworks;                                                              // Fireworks to activate during post game
     
        private Wire m_FinalWire;                       // The final wire (exit wire)

        [Header("Tutorial")]
        [SerializeField] private bool m_EnableTutorial = true;                  // If tutorial is enabled (at start of game)
        [SerializeField] private int m_TutorialInitialSegments = 20;            // Segments of the initial wire in tutorial mode
        [SerializeField] private int m_TutorialWireSegments = 20;               // Segments per wire in tutorial mode
        [SerializeField] private float m_TutorialWireRadius = 5f;               // Spawn radius of wires in tutorial mode
        [SerializeField] private float m_TutorialSparkSpeed = 1f;               // Spark speed in tutorial mode
        [SerializeField] private float m_TutorialJumpTime = 1.5f;               // Jump time in tutorial mode
        [SerializeField] private int m_TutorialPacketOffset = 50;               // Offset from player packets spawn in tutorial mode
        [SerializeField] private float m_TutorialPacketSpeed = 7.5f;            // Packet speed in tutorial mode
        [SerializeField] private float m_TutorialPacketLifetime = 15f;          // Lifetime of packets in tutorial mode
        [SerializeField] private int m_TutorialBonusSegments = 15;              // Bonus segments when collecting packets in tutorial mode
        [SerializeField] private TutorialUI m_TutorialUI;                       // UI to display during tutorial mode

        private bool m_TutorialActive = false;                          // If tutorial is active   
        private int m_TutorialWiresSpawned = 0;                         // Amount of tutorial wires that have been spawned
        private TutorialStep m_TutorialStep = TutorialStep.None;        // Current step of tutorial
        private bool m_TutorialWireDefective = false;                   // If last wire spawned by tutorial was defective

        // The length of the arcade game mode
        public float arcadeLength { get { return m_ArcadeLength; } }

        // The length of post game state
        public float postArcadeLength { get { return m_PostArcadeLength; } }

        #if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private Text m_DebugText;      // Text for writing debug data
        #endif

        #if UNITY_EDITOR
        void Update()
        {
            if (m_DebugText)
            {
                int playersSegment = m_WireManager.GetPositionSegment(WireManager.WirePlane * m_WireManager.sparkJumper.wireDistanceTravelled);

                m_DebugText.text = string.Format("Game Time: {0}\nSegments Travelled: {1}\nPlayer SegmentsTravelled: {2}",
                    Mathf.FloorToInt(gameTime), m_WireManager.GetJumpersSegment(), playersSegment);
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

            if (scoreManager)
                scoreManager.DisableMultiplierTick();

            if (m_FinaleSequence && !m_SkipFinale)
            {
                m_FinaleSequence.OnSequenceFinished += FinaleSequenceFinished;
                m_FinaleSequence.Activate(m_WireManager, m_WorldTheme);
            }
            else
            {
                StartCoroutine(SpawnEndingWireRoutine());
            }
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
            tutorialProps.m_TopCircleCutoff = 0.8f;
            tutorialProps.m_MinSegments = m_TutorialWireSegments;
            tutorialProps.m_MaxSegments = m_TutorialWireSegments;
            tutorialProps.m_SpawnSegmentRange = 0;
            tutorialProps.m_DefectiveWireChance = 1f;                   // We manually control if wires are defective or not
            tutorialProps.m_SparkSpeed = m_TutorialSparkSpeed;
            tutorialProps.m_JumpTime = m_TutorialJumpTime;

            if (m_WireManager.StartTutorial(tutorialProps, m_TutorialInitialSegments))
            {
                m_TutorialActive = true;
                m_TutorialWiresSpawned = 0;
                m_TutorialStep = TutorialStep.None;

                // Switch to tutorial UI
                {
                    if (m_TutorialUI)
                    {
                        m_TutorialUI.StartSlides();

                        if (m_TutorialUI.skipButton)
                            m_TutorialUI.skipButton.onClick.AddListener(SkipTutorial);
                    }

                    if (sparkJumper.companion)
                    {
                        CompanionUI companion = sparkJumper.companion;
                        companion.DisableCompanion();
                        companion.MoveToTutorialAnchor();
                    }

                    if (sparkJumper.m_ScreenFade)
                    {
                        sparkJumper.m_ScreenFade.OnFadeFinished += TutorialFadeFinished;
                        sparkJumper.m_ScreenFade.FadeIn();
                    }
                }

                NextTutorialStep();
            }
            else
            {
                // Fallback to normal game
                StartArcade();
            }
        }

        /// <summary>
        /// Cleans up any values set for tutorial mode
        /// </summary>
        private void EndTutorial(bool skipped)
        {
            if (m_TutorialActive)
            {
                StopCoroutine("TutorialWireSpawnRoutine");

                m_WireManager.PlayerJumpedOffWire -= TutorialJumpedOffWire;

                if (scoreManager)
                {
                    scoreManager.OnPacketDespawned -= TutorialPacketDespawned;
                }

                // Switch to game UI
                {
                    if (m_TutorialUI)
                    {
                        m_TutorialUI.EndSlides();

                        if (m_TutorialUI.skipButton)
                            m_TutorialUI.skipButton.onClick.RemoveListener(SkipTutorial);

                        Destroy(m_TutorialUI.gameObject);
                    }

                    if (sparkJumper.companion)
                    {
                        CompanionUI companion = sparkJumper.companion;
                        companion.EnableCompanion();
                        companion.MoveToGameAnchor();

                        if (companion.voice)
                        {
                            if (skipped)
                                companion.voice.PlayTutorialSkippedDialogue();
                            else
                                companion.voice.PlayTutorialFinishedDialogue();
                        }
                    }

                    if (sparkJumper.m_ScreenFade)
                        sparkJumper.m_ScreenFade.ClearFade();
                }

                m_TutorialActive = false;
                m_TutorialWiresSpawned = 0;
                m_TutorialStep = TutorialStep.None;

                StartArcade();
            }
        }
        
        /// <summary>
        /// Starts arcade mode
        /// </summary>
        private void StartArcade()
        {
            if (m_WireManager.StartWires())
            {
                Invoke("EndGame", m_ArcadeLength);

                m_StartingDistance = m_WireManager.sparkJumper.GetPosition().z;

                m_GameStart = Time.time;

                if (OnGameStarted != null)
                    OnGameStarted.Invoke();
            }
        }

        /// <summary>
        /// Skips the tutorial (if active)
        /// </summary>
        public void SkipTutorial()
        {
            if (m_TutorialActive)
                EndTutorial(true);
        }

        /// <summary>
        /// Routine for generating delayed wires during the tutorial
        /// </summary>
        private IEnumerator TutorialWireSpawnRoutine(Wire wire)
        {
            if (!wire)
                yield break;

            while (m_TutorialActive)
            {
                if (wire.sparkProgress > 0.5f)
                {
                    bool defective = false;// m_TutorialStep == TutorialStep.Defective;

                    // Repeat until a wire is spawned
                    Wire newWire = m_WireManager.GenerateRandomFixedWire(m_TutorialWireSegments, true, defective);
                    while (!newWire)
                        newWire = m_WireManager.GenerateRandomFixedWire(m_TutorialWireSegments, true, defective);

                    // We play first tutorial dialogue when first wire spawns in
                    if (m_TutorialWiresSpawned <= 0)
                    {
                        if (sparkJumper && sparkJumper.companionVoice)
                            sparkJumper.companionVoice.PlayTutorialDialogue(0);
                    }

                    m_TutorialWireDefective = defective;
                    ++m_TutorialWiresSpawned;

                    yield break;
                }
                else
                {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Routine for delaying the start of the next tutorial step for duration
        /// </summary>
        /// <param name="delay">Delay to wait</param>
        private IEnumerator TutorialDelayStepRoutine(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            else
                yield return null;

            NextTutorialStep();
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
            if (sparkJumper)
                sparkJumper.OnJumpToSpark += FinaleJumpToSpark;

            m_WireManager.SetWireGenerationEnabled(false);

            if (scoreManager)
                scoreManager.SetPacketGenerationEnabled(false);
        }

        /// <summary>
        /// Routine for handling the end game (displaying score and exiting)
        /// </summary>
        private IEnumerator DisplayStatsAndExitRoutine()
        {
            float score = 0f;
            float distance = 0f;

            if (scoreManager)
            {
                scoreManager.DisableScoring();
                score = m_WireManager.scoreManager.score;
            }

            if (sparkJumper)
            {
                sparkJumper.m_ControlsEnabled = false;
                sparkJumper.OnJumpToSpark -= FinaleJumpToSpark;

                distance = Mathf.Abs(sparkJumper.GetPosition().z - m_StartingDistance);

                if (sparkJumper.companion)
                    sparkJumper.companion.ShowStatsUI(score, distance);
            }

            yield return new WaitForSeconds(m_PostArcadeLength);

            if (sparkJumper && sparkJumper.m_ScreenFade)
            {
                ScreenFade screenFade = sparkJumper.m_ScreenFade;
                screenFade.OnFadeFinished += FinaleFadeFinished;
                screenFade.m_FadeColor = Color.gray;
                screenFade.FadeOut();
            }
            else
            {
                // Pretent screen instantly faded out
                FinaleFadeFinished(1f);
            }
        }

        /// <summary>
        /// Notify that screen fade in has finished when starting tutorial
        /// </summary>
        /// <param name="endAlpha">Last alpha of screen fade</param>
        private void TutorialFadeFinished(float endAlpha)
        {
            if (sparkJumper)
            {
                sparkJumper.m_ScreenFade.OnFadeFinished -= TutorialFadeFinished;

                if (sparkJumper.companionVoice)
                    sparkJumper.companionVoice.PlayIntroDialogue();
            }
        }

        /// <summary>
        /// Notify that player has jumped off a wire during the tutorial
        /// </summary>
        /// <param name="wire">Wire player is jumping to</param>
        /// <param name="failed">If player failed to jump off wire</param>
        private void TutorialJumpedOffWire(Wire wire, bool failed)
        {
            if (!failed)
            {
                // Chance with non defective wire spawning in earlier than defective wire step
                // starting. This checks if last generated wire was defective (we don't need to
                // check the wire specifically since we only spawn one wire at a time in tutorial mode)
                if ((m_TutorialStep == TutorialStep.Jumping && !m_TutorialWireDefective))// ||
                    //(m_TutorialStep == TutorialStep.Defective && m_TutorialWireDefective))
                {
                    NextTutorialStep();
                }
            }

            // Don't spawn if tutorial is over
            if (m_TutorialActive)
                StartCoroutine(TutorialWireSpawnRoutine(wire));
        }

        /// <summary>
        /// Notify that a packet had despawned during tutorial mode
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="wasCollected"></param>
        private void TutorialPacketDespawned(DataPacket packet, bool wasCollected)
        {
            if (!scoreManager)
                EndTutorial(false);

            if (wasCollected)
            {
                NextTutorialStep();
            }
            else
            {
                // Repeat until a packet is spawned
                DataPacket newPacket = scoreManager.GenerateRandomPacket(false);
                while (!newPacket)
                    newPacket = scoreManager.GenerateRandomPacket(false);
            }
        }

        /// <summary>
        /// Notify that the finale sequence has finished
        /// </summary>
        private void FinaleSequenceFinished()
        {
            StartCoroutine(SpawnEndingWireRoutine());
        }

        /// <summary>
        /// Notify that player has jumped to a new spark during end game
        /// </summary>
        /// <param name="spark">Spark player jumped to</param>
        /// <param name="finished">If jump just finished</param>
        private void FinaleJumpToSpark(Spark spark, bool finished)
        {
            if (m_FinalWire && spark.GetWire() == m_FinalWire && finished)
            {
                StartCoroutine(DisplayStatsAndExitRoutine());

                if (sparkJumper && sparkJumper.companionVoice)
                    sparkJumper.companionVoice.PlayGameFinishedDialogue();

                if (m_Fireworks)
                    m_Fireworks.Activate();
            }
        }

        /// <summary>
        /// Notify that screen fade out has finished after game has finished
        /// </summary>
        /// <param name="endAlpha">Last alpha of screen fade</param>
        private void FinaleFadeFinished(float endAlpha)
        {
            // @Liminal: This is the last function that executes (after finale and fading out)
            // You can put whatever you wish here

            // for now
            #if UNITY_EDITOR
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            #else
            Application.Quit(0);
            #endif
        }

        /// <summary>
        /// Starts next step of tutorial
        /// </summary>
        private void NextTutorialStep()
        {
            if (m_TutorialActive)
            {
                int step = -1;

                switch (m_TutorialStep)
                {
                    case TutorialStep.None:
                        StartJumpingTutorial();
                        
                        // We call this tutorial dialogue elsewhere
                        //step = 0;
                        break;

                    case TutorialStep.Jumping:
                        StartPacketTutorial();
                        step = 1;
                        break;
                    
                    case TutorialStep.Packets:
                        StartExtensionTutorial();
                        step = 2;
                        break;

                    case TutorialStep.Extension:
                        EndTutorial(false);
                        break;
                }

                if (m_TutorialActive)
                {
                    if (m_TutorialUI)
                        m_TutorialUI.NextSlide();

                    if (sparkJumper && sparkJumper.companionVoice)
                        sparkJumper.companionVoice.PlayTutorialDialogue(step);
                }
            }
        }

        /// <summary>
        /// Starts the jumping section of the tutorial
        /// </summary>
        private void StartJumpingTutorial()
        {
            m_TutorialStep = TutorialStep.Jumping;

            // Initial wire to spawn for player (others will spawn if they failed to jump of current one)
            Wire wire = m_WireManager.sparkJumper.wire;
            StartCoroutine(TutorialWireSpawnRoutine(wire));
        }

        /// <summary>
        /// Starts the packet section of the tutorial
        /// </summary>
        private void StartPacketTutorial()
        {
            m_TutorialStep = TutorialStep.Packets;

            if (scoreManager)
            {
                // Custom properties (we can ignore intervals as we will handle it)
                PacketStageProperties tutorialProps = new PacketStageProperties();
                tutorialProps.m_MinSpawnOffset = m_TutorialPacketOffset;
                tutorialProps.m_MinSpeed = m_TutorialPacketSpeed;
                tutorialProps.m_MaxSpeed = m_TutorialPacketSpeed;
                tutorialProps.m_Lifetime = m_TutorialPacketLifetime;
                tutorialProps.m_BonusSegments = m_TutorialBonusSegments;
                tutorialProps.m_ClusterChance = 0f;

                scoreManager.EnableTutorial(tutorialProps);
                scoreManager.OnPacketDespawned += TutorialPacketDespawned;

                DataPacket newPacket = scoreManager.GenerateRandomPacket(false);
                while (!newPacket)
                    newPacket = scoreManager.GenerateRandomPacket(false);
            }
            else
            {
                // We can't progress onto next tutorial
                EndTutorial(false);
            }
        }

        /// <summary>
        /// Starts the wire extension section of the tutoril
        /// </summary>
        private void StartExtensionTutorial()
        {
            m_TutorialStep = TutorialStep.Extension;

            if (scoreManager)
            {
                scoreManager.OnPacketDespawned -= TutorialPacketDespawned;
                scoreManager.SetPacketGenerationEnabled(false);
            }

            float delay = 0f;
            if (sparkJumper && sparkJumper.companionVoice)
                delay = sparkJumper.companionVoice.PlayTutorialDialogue(2);

            // Additional half a second of delay
            StartCoroutine(TutorialDelayStepRoutine(delay + 0.5f));
        }
    }
}
