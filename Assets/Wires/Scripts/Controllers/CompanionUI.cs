using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TO5.Wires
{
    /// <summary>
    /// Handles the UI displayed on the players companion (including the bot mesh itself)
    /// </summary>
    [RequireComponent(typeof(CompanionVoice))]
    public class CompanionUI : MonoBehaviour
    {
        /// <summary>
        /// The companions anchor relative to it's parent
        /// </summary>
        [System.Serializable]
        public struct Anchor
        {
            public Vector3 m_Position;          // Position in local space
            public Vector3 m_Scale;             // Scale in local space
        }

        [SerializeField] private SparkJumper m_SparkJumper;         // Players spark jumper 
        [SerializeField] private ScoreManager m_ScoreManager;       // Games score manager
        [SerializeField] private Animator m_Animator;               // Companions animator
        private CompanionVoice m_Voice;                             // Companions voice

        // The companions voice script for speaking dialogue
        public CompanionVoice voice { get { return m_Voice; } }

        [Header("Game")]
        public string m_ScoreTextFormat = "Score: {0}";         // Formatting for score text ({0} is required, will be replaced by actual score)
        public string m_MultiplierTextFormat = "X{0}";          // Formatting for multiplier text ({0} is required, will be replaced by actual multiplier)

        [SerializeField] private Canvas m_GameCanvas;           // Canvas for companions game UI
        [SerializeField] private Text m_ScoreText;              // Text block for writing players score
        [SerializeField] private Text m_MultiplierText;         // Text block for writing players multiplier
        [SerializeField] private Image m_MultiplierImage;       // Image to manipulate based on auto multiplier increase
        [SerializeField] private Slider m_WireSlider;           // Slider for displaying wire progress

        [Header("Jump")]
        [SerializeField] private FloatingMovement m_FloatingMovement;       // Floating movement to optionally disable when jumping
        [SerializeField] private Transform m_JumpPivot;                     // Pivot to move when jumping
        public float m_JumpTime = 1.25f;                                    // Time that a jumps last
        public float m_IncHeight = 0.5f;                                    // Height of increase jump
        public float m_DecHeight = 0.2f;                                    // Height of decrease jump

        private Coroutine m_JumpRoutine = null;         // Jump routine currently running

        [Header("Stats")]
        public string m_ScoreStatsTextFormat = "Score: {0}";                // Formatting for score text on the stats UI ({0} is required, will be replaced by actual score)
        public string m_DistanceStatsTextFormat = "Distance: {0:0.0}m";     // Formatting for distance text on the stats UI ({0} is required, will be replaced by actual distance)

        [SerializeField] private Canvas m_StatsCanvas;          // Canvas for companions stats UI
        [SerializeField] private Text m_ScoreStatsText;         // Text block for writing players final score
        [SerializeField] private Text m_DistanceStatsText;      // Text block for writing players distance travelled

        [Header("Animation")]
        [SerializeField] private string m_EndWireAnim = "EndOfWire";            // Name of state for playing end of wire anim
        [SerializeField] private string m_MulIncAnim = "MulIncrease";           // Name of state for playing multiplier increased anim
        [SerializeField] private string m_MulDecAnim = "MulDecrease";           // Name of state for playing multiplier decreased anim
        [SerializeField, Min(0f)] private float m_IdleSpeedIncrease = 0.25f;    // Extra speed applied to idle animation per stage

        // Would ideally use a dictionary for these mode settings but dictionaries aren't exposed to the editor

        [Header("Anchor")]
        private Anchor m_GameAnchor;                                // Game anchor (overwritten on awake)
        [SerializeField] private Anchor m_TutorialAnchor;           // Anchor to use in tutorial mode
        public float m_SwitchAnchorSpeed = 1f;                      // Speed at which to switch anchors
        [SerializeField] private Transform m_LookAtTarget;          // Target for companion to look at (will not rotate if nothing is set)

        private Coroutine m_SwitchAnchorRoutine = null;     // Switch anchor routine currently running
        private float m_SwitchAnchorTime = 1f;              // Current time of anchor switch
        private bool m_AtGameAnchor = true;                 // If we are at (or switching to) the game anchor

        private bool m_DisplayGameUI = true;                // If game UI should be displayed
        private int m_PreviousStage = 0;                    // Players previous multiplier stage

        [Header("Debug")]
        [Tooltip("Displays FPS, will be hidden in non-development builds")]
        public Text m_FPSText;              // Text block to display the FPS with

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        int m_Frames = 0;                   // Frames passes this second
        int m_FPS = 0;                      // Frames rendered last second
        float m_FrameTime = 0f;             // Time passed since last second
        #endif

        void Awake()
        { 
            // Disable text in non development builds
            #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            if (m_FPSText)
                Destroy(m_FPSText.gameObject);
            #endif

            m_Voice = GetComponent<CompanionVoice>();

            if (m_SparkJumper)
                m_SparkJumper.OnDriftingUpdated += DriftingUpdated;

            if (m_ScoreManager)
            {
                m_PreviousStage = m_ScoreManager.multiplierStage;

                m_ScoreManager.OnMultiplierUpdated += MultiplierUpdated;
            }

            m_GameAnchor.m_Position = transform.localPosition;
            m_GameAnchor.m_Scale = transform.localScale;

            if (m_LookAtTarget)
                transform.LookAt(m_LookAtTarget);
        }

        void Update()
        {
            if (m_DisplayGameUI)
            {
                if (m_SparkJumper)
                    RefreshWireProgress(m_SparkJumper.wireProgress);

                if (m_ScoreManager)
                {
                    RefreshScoreText(m_ScoreManager.score);
                    //RefreshMultiplierText((int)m_ScoreManager.totalMultiplier);
                    RefreshMultiplierProgress(m_ScoreManager.multiplierProgress);
                }
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Update FPS
            {
                m_FrameTime += Time.deltaTime;
                m_Frames++;

                if (m_FrameTime >= 1f)
                {
                    m_FPS = m_Frames;
                    m_Frames = 0;
                    m_FrameTime -= 1f;
                }

                if (m_FPSText)
                    m_FPSText.text = string.Format("FPS: {0}", m_FPS);
            }
            #endif
        }

        /// <summary>
        /// Enables the companions UI and updates
        /// </summary>
        public void EnableCompanion()
        {
            ToggleEnabledUI(m_DisplayGameUI);
        }

        /// <summary>
        /// Disables the companions UI and updates
        /// </summary>
        public void DisableCompanion()
        {
            SetDisplayCanvases(false, false);
        }

        /// <summary>
        /// Renders the game UI, hiding stats UI
        /// </summary>
        public void ShowGameUI()
        {
            ToggleEnabledUI(true);
        }

        /// <summary>
        /// Renders the stats UI, hiding the game UI
        /// </summary>
        /// <param name="score">Score to display on stats</param>
        /// <param name="distance">Distance to display on stats</param>
        public void ShowStatsUI(float score, float distance)
        {
            if (m_ScoreStatsText)
                m_ScoreStatsText.text = string.Format(m_ScoreStatsTextFormat, Mathf.FloorToInt(score));

            if (m_DistanceStatsText)
                m_DistanceStatsText.text = string.Format(m_DistanceStatsTextFormat, distance);

            ToggleEnabledUI(false);
        }

        /// <summary>
        /// Set if the game UI or stats UI should be displayed
        /// </summary>
        /// <param name="enable">True to display Game UI, False to display Stats UI</param>
        private void ToggleEnabledUI(bool gameUI)
        {
            SetDisplayCanvases(gameUI, !gameUI);
            m_DisplayGameUI = gameUI;
        }

        /// <summary>
        /// Set which canvases will be rendered or disabled
        /// </summary>
        /// <param name="gameUI">Render game UI</param>
        /// <param name="statsUI">Render stats UI</param>
        private void SetDisplayCanvases(bool gameUI, bool statsUI)
        {
            if (m_GameCanvas)
                m_GameCanvas.gameObject.SetActive(gameUI);

            if (m_StatsCanvas)
                m_StatsCanvas.gameObject.SetActive(statsUI);
        }

        /// <summary>
        /// Move companion to its game anchor
        /// </summary>
        public void MoveToGameAnchor()
        {
            MoveToAnchor(true);
        }

        /// <summary>
        /// Move companion to its tutorial anchor
        /// </summary>
        public void MoveToTutorialAnchor()
        {
            MoveToAnchor(false);
        }

        /// <summary>
        /// Moves companion to desired anchor (if not already)
        /// </summary>
        /// <param name="gameAnchor">If to move to game anchor</param>
        private void MoveToAnchor(bool gameAnchor)
        {
            if (gameAnchor != m_AtGameAnchor)
            {
                if (m_SwitchAnchorRoutine != null)
                    StopCoroutine(m_SwitchAnchorRoutine);

                m_AtGameAnchor = gameAnchor;
                m_SwitchAnchorTime = Mathf.Clamp01(1f - m_SwitchAnchorTime);

                m_SwitchAnchorRoutine = StartCoroutine(SwitchAnchorRoutine());
            }
        }

        /// <summary>
        /// Interpolates between game and tutorial anchor 
        /// </summary>
        /// <param name="alpha">Stage of interpolation</param>
        private void InterpolateAnchor(float alpha)
        {
            // Easing function InOutCubic
            // See: https://easings.net/en
            if (alpha < 0.5f)
                alpha = 4f * alpha * alpha * alpha;
            else
                alpha = (alpha - 1f) * (2f * alpha - 2f) * (2f * alpha - 2f) + 1;

            transform.localPosition = Vector3.Lerp(m_GameAnchor.m_Position, m_TutorialAnchor.m_Position, alpha);
            transform.localScale = Vector3.Lerp(m_GameAnchor.m_Scale, m_TutorialAnchor.m_Scale, alpha);

            if (m_LookAtTarget)
                transform.LookAt(m_LookAtTarget);
        }

        /// <summary>
        /// Refreshes score text
        /// </summary>
        /// <param name="score">Score to display</param>
        public void RefreshScoreText(float score)
        {
            if (m_ScoreText)
                m_ScoreText.text = string.Format(m_ScoreTextFormat, Mathf.FloorToInt(score));
        }

        /// <summary>
        /// Refreshes multiplier text
        /// </summary>
        /// <param name="multiplier">Multiplier value</param>
        public void RefreshMultiplierText(int multiplier)
        {
            if (m_MultiplierText)
                m_MultiplierText.text = string.Format(m_MultiplierTextFormat, multiplier);
        }

        /// <summary>
        /// Refreshes multiplier auto increase progress
        /// </summary>
        /// <param name="progress">Auto increase progress</param>
        public void RefreshMultiplierProgress(float progress)
        {
            if (m_MultiplierImage)
                m_MultiplierImage.fillAmount = progress;
        }

        /// <summary>
        /// Refreshes wire progress
        /// </summary>
        /// <param name="progress">Wire progress</param>
        public void RefreshWireProgress(float progress)
        {
            if (m_WireSlider)
                m_WireSlider.value = progress;
        }

        /// <summary>
        /// Notify that player has entered drifting mode
        /// </summary>
        /// <param name="isEnabled">If drifting is enabled</param>
        private void DriftingUpdated(bool isEnabled)
        {
            if (isEnabled)
            {
                if (m_Animator)
                    m_Animator.Play(m_EndWireAnim);

                if (m_Voice)
                    m_Voice.PlayEndOfWireDialogue();
            }
        }

        /// <summary>
        /// Notify that multiplier stage has changed
        /// </summary>
        /// <param name="multiplier">New multiplier</param>
        /// <param name="stage">New stage</param>
        private void MultiplierUpdated(float multiplier, int stage)
        {
            RefreshMultiplierText((int)multiplier);

            if (m_PreviousStage != stage)
            {
                if (m_Animator)
                {
                    string stateName = stage > m_PreviousStage ? m_MulIncAnim : m_MulDecAnim;
                    m_Animator.Play(stateName);

                    m_Animator.SetFloat("idleMultiplier", 1f + m_IdleSpeedIncrease * stage);
                }

                if (m_JumpRoutine != null)
                    StopCoroutine(m_JumpRoutine);

                m_JumpRoutine = StartCoroutine(JumpRoutine(stage > m_PreviousStage ? m_IncHeight : -m_DecHeight));

                m_PreviousStage = stage;
            }
        }

        /// <summary>
        /// Routine for handling the companions jump
        /// </summary>
        /// <param name="offset">Offset from origin to move</param>
        private IEnumerator JumpRoutine(float offset)
        {
            if (m_FloatingMovement)
                m_FloatingMovement.enabled = false;

            float end = Time.time + m_JumpTime;
            while (Time.time < end)
            {
                float alpha = Mathf.Clamp01((end - Time.time) / m_JumpTime);
                alpha = Mathf.Pow((alpha * 2f) - 1f, 2f);

                if (m_JumpPivot)
                    m_JumpPivot.transform.localPosition = new Vector3(0f, Mathf.Lerp(0f, offset, 1f - alpha), 0f);

                yield return null;
            }

            if (m_JumpPivot)
                m_JumpPivot.transform.localPosition = Vector3.zero;

            if (m_FloatingMovement)
                m_FloatingMovement.enabled = true;

            m_JumpRoutine = null;
        }

        /// <summary>
        /// Routine that handles switching the companions anchor
        /// </summary>
        /// <returns></returns>
        private IEnumerator SwitchAnchorRoutine()
        {
            while (m_SwitchAnchorTime < 1f)
            {
                m_SwitchAnchorTime += Time.deltaTime * m_SwitchAnchorSpeed;
                m_SwitchAnchorSpeed = Mathf.Min(m_SwitchAnchorSpeed, 1f);

                InterpolateAnchor(m_AtGameAnchor ? 1f - m_SwitchAnchorTime : m_SwitchAnchorTime);

                yield return null;
            }

            InterpolateAnchor(m_AtGameAnchor ? 0f : 1f);
        }

        /// <summary>
        /// Sets if companion should enter boost mode 
        /// </summary>
        /// <param name="enable">If boost made is enabled</param>
        public void SetBoostModeEnabled(bool enable)
        {
            if (m_Animator)
                m_Animator.SetBool("boostActive", enable);
        }

        void OnDrawGizmos()
        {
            Transform parent = transform.parent ? transform.parent : transform;
            Vector3 gamePos = Application.isPlaying ? parent.TransformPoint(m_GameAnchor.m_Position) : transform.position;
            Vector3 tutPos = parent.TransformPoint(m_TutorialAnchor.m_Position);

            // Travel line
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(gamePos, tutPos);

            // Sizes
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(gamePos, Vector3.Scale(parent.lossyScale, m_GameAnchor.m_Scale));
            Gizmos.DrawWireCube(tutPos, Vector3.Scale(parent.lossyScale, m_TutorialAnchor.m_Scale));
        }
    }
}
