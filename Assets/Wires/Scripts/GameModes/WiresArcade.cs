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
        [SerializeField] private float m_MinGameTime = 180f;            // Min amount of game time required in seconds
        [SerializeField] private float m_MaxGameTime = 600f;            // Max amount of game time required in seconds
        [SerializeField] private int m_MinSegmentsTravelled = 300;      // The minimum amount of segments that must pass before finishing

        private bool m_GameCanFinish;           // If the game can finish
        private bool m_GameFinished;            // If the game has finished

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
                    m_DebugText.text = string.Format("Game Time: {0}\nSegments Travelled: {1}\nGame Can Finish: {2}\nGame Finished: {3}",
                        Mathf.FloorToInt(Time.time - m_GameStart), m_WireManager.GetJumpersSegment(), m_GameCanFinish, m_GameFinished);                  
                }
            }
        }
        #endif

        // WireGameMode interface
        protected override void StartGame()
        {
            base.StartGame();

            StartCoroutine(GameTimerRoutine());
            StartCoroutine(SegmentCheckRoutine());

            m_GameCanFinish = false;
            m_GameFinished = false;
        }

        // WireGameMode interface
        protected override void EndGame()
        {
            base.EndGame();

            StopCoroutine("GameTimerRoutine");
            StopCoroutine("SegmentCheckRoutine");

            m_GameFinished = true;
        }

        /// <summary>
        /// Routine for handling the games timer
        /// </summary>
        private IEnumerator GameTimerRoutine()
        {
            yield return new WaitForSeconds(m_MinGameTime);

            if (m_GameCanFinish)
            {
                EndGame();
                yield break;
            }
            else
            {
                m_GameCanFinish = true;
            }

            yield return new WaitForSeconds(m_MaxGameTime - m_MinGameTime);

            EndGame();
        }

        /// <summary>
        /// Routine for handling when player has passed required segments
        /// </summary>
        private IEnumerator SegmentCheckRoutine()
        {
            if (m_WireManager)
            {
                yield return new WaitForSegment(m_WireManager, m_WireManager.GetJumpersSegment() + m_MinSegmentsTravelled);

                if (m_GameCanFinish)
                {
                    EndGame();
                    yield break;
                }
                else
                {
                    m_GameCanFinish = true;
                }
            }
        }
    }
}
