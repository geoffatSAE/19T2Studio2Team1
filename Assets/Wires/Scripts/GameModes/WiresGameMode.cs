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
        [SerializeField] protected WorldTheme m_WorldTheme;         // World theme to interact with
        [SerializeField] private bool m_AutoStart = true;           // If game should auto start
        
        protected float m_GameStart = -1f;          // When the game started

        void Start()
        {
            if (!m_WireManager)
            {
                Debug.LogError("Game mode has not been provided a wire manager");
                return;
            }

            if (m_AutoStart)
                TryStartGame();
        }

        /// <summary>
        /// Starts the game
        /// </summary>
        protected virtual void StartGame()
        {
            m_WireManager.StartWires();

            // We initialze theme after starting wires as the first wire needs to be generated
            if (m_WorldTheme)
                m_WorldTheme.Initialize(m_WireManager);

            m_GameStart = Time.time;
        }

        /// <summary>
        /// Ends the game
        /// </summary>
        protected virtual void EndGame()
        {
            m_WireManager.StopWires();
        }

        /// <summary>
        /// Attempts to start the game (checks if game state is valid)
        /// </summary>
        /// <returns>If game has started</returns>
        public bool TryStartGame()
        {
            if (!m_WireManager)
            {
                Debug.LogError("Unable to start game as no wire manager has been provided");
                return false;
            }

            StartGame();

            Debug.Log("Starting game");

            return true;
        }
    }
}
