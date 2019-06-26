using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TO5.Wires
{
    public class ScoreManager : MonoBehaviour
    {
        [Header("Score")]
        [SerializeField] private float m_ScorePerSecond = 10f;
        [SerializeField] private float m_JumpScore = 100f;

        [Header("Multiplier")]
        [SerializeField, Range(0, 32)] private int m_MultiplierStages = 2;
        [SerializeField] private int m_SegmentsBeforeStageIncrease = 15;

        [Header("UI")]
        public Text m_ScoreText;
        public Text m_MultiplierText;

        //#if UNITY_EDITOR
        [SerializeField] private Text m_DebugText;
       // #endif

        // Scores current multiplier
        public float multiplier { get { return m_Multiplier; } }

        private WireManagerAlt m_WireManager;
        private float m_Score;
        private float m_Multiplier;
        private int m_Stage;
        private Coroutine m_MultiplierTick;

        void Update()
        {
            m_Score += m_ScorePerSecond * m_Multiplier * Time.deltaTime;

            //#if UNITY_EDITOR
            // Debug text
            if (m_DebugText)
                m_DebugText.text = string.Format("Score: {0}\nMultiplier: {1}\nMultiplier Stage: {2}", Mathf.FloorToInt(m_Score), m_Multiplier, m_Stage);
            //#endif
        }

        /// <summary>
        /// Initialize manager to work with wire manager
        /// </summary>
        /// <param name="wireManager">Wire manager</param>
        public void Initialize(WireManagerAlt wireManager)
        {
            m_Score = 0f;
            m_Multiplier = 1f;
            m_Stage = 0;

            m_WireManager = wireManager;
        }
        
        /// <summary>
        /// Enables scoring functionality
        /// </summary>
        public void EnableScoring()
        {
            enabled = true;

            m_MultiplierTick = StartCoroutine(MultiplierTickRoutine());
        }

        /// <summary>
        /// Disables scoring functionality
        /// </summary>
        public void DisableScoring()
        {
            StopCoroutine(m_MultiplierTick);
            m_MultiplierTick = null;

            enabled = false;
        }

        /// <summary>
        /// Increases the multiplier by amount of stages
        /// </summary>
        /// <param name="stages">Stages to increase by</param>
        public void IncreaseMultiplier(int stages = 1)
        {
            SetMultiplierStage(m_Stage + stages);
        }

        /// <summary>
        /// Decreases the multiplier by amount of stages
        /// </summary>
        /// <param name="stages"></param>
        public void DecreaseMultiplier(int stages = 1)
        {
            SetMultiplierStage(m_Stage - stages);

            // Reset the multiplier tick routine
            StopCoroutine(m_MultiplierTick);
            m_MultiplierTick = StartCoroutine(MultiplierTickRoutine());
        }

        /// <summary>
        /// Sets the multiplier stage, updates multiplier value
        /// </summary>
        /// <param name="stage">Stage to set</param>
        private void SetMultiplierStage(int stage)
        {
            m_Stage = Mathf.Clamp(stage, 0, m_MultiplierStages);
            m_Multiplier = (1 << m_Stage);
        }

        /// <summary>
        /// Tick routine for increasing multiplier
        /// </summary>
        /// <returns></returns>
        private IEnumerator MultiplierTickRoutine()
        {
            while (enabled)
            {
                yield return new WaitForSegment(m_WireManager, m_WireManager.GetJumpersSegment() + m_SegmentsBeforeStageIncrease);
                IncreaseMultiplier(1);

                // No point in looping if at max stage
                if (m_Stage == m_MultiplierStages)
                    break;
            }
        }
    }
}
