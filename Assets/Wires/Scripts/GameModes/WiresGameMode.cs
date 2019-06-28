using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Game mode for a session of wires, handles starting and finishing a session
    /// </summary>
    public abstract class WiresGameMode : MonoBehaviour
    {
        [SerializeField] protected WireManager m_WireManager;       // Wire manager to interact with
        [SerializeField] private bool m_AutoStart = true;           // If game should auto start

        void Start()
        {
            if (!m_WireManager)
            {
                Debug.LogError("Game mode has not been provided a wire manager");
                return;
            }

            if (m_AutoStart)
                StartGame();
        }

        /// <summary>
        /// Starts the game
        /// </summary>
        protected virtual void StartGame()
        {
            m_WireManager.StartWires();
        }

        /// <summary>
        /// Ends the game
        /// </summary>
        protected virtual void EndGame()
        {
            m_WireManager.StopWires();
        }
    }
}
