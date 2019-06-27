using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        private bool m_GameCanFinish;
        private bool m_GameFinished;

        private IEnumerator GameTimerRoutine()
        {
            yield return new WaitForSeconds(m_MinGameTime);

            if (m_GameCanFinish)
            {
                // TODO: Finish game
            }
            else
            {
                m_GameCanFinish = true;
            }

            yield return new WaitForSeconds(m_MaxGameTime - m_MinGameTime);

            // TODO: Finish game
        }

        private IEnumerator SegmentCheckRoutine()
        {
            if (m_WireManager)
            {
                yield return new WaitForSegment(m_WireManager, m_WireManager.GetJumpersSegment() + m_MinSegmentsTravelled);

                if (m_GameCanFinish)
                {
                    // TODO: Finish game
                }
                else
                {
                    m_GameCanFinish = true;
                }
            }
        }
    }
}
