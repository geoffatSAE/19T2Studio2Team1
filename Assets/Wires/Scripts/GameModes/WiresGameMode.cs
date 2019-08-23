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
        /// <summary>
        /// Delegate for when the game state has changed
        /// </summary>
        public delegate void GameStateChanged();

        public GameStateChanged OnGameStarted;          // Event for when game starts
        public GameStateChanged OnGameFinished;         // Event for when game finishes

        [SerializeField] protected WireManager m_WireManager;       // Wire manager to interact with
        [SerializeField] protected WorldTheme m_WorldTheme;         // World theme to interact with
        [SerializeField] private bool m_AutoStart = true;           // If game should auto start
        
        protected float m_GameStart = -1f;          // When the game started
        protected float m_GameLength = -1f;         // Time game lasted for

        // Time that has passed since starting game
        public float gameTime { get { return m_GameStart >= 0f ? Time.time - m_GameStart : 0f; } }

        // Length of the played match (-1 if game is still in progress)
        public float gameLength { get { return m_GameLength; } }

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
            m_GameStart = Time.time;
            m_GameLength = -1f;

            m_WireManager.StartWires();

            if (m_WorldTheme)
                m_WorldTheme.Initialize(m_WireManager);

            if (OnGameStarted != null)
                OnGameStarted.Invoke();
        }

        /// <summary>
        /// Ends the game
        /// </summary>
        protected virtual void EndGame()
        {
            m_GameLength = gameTime;

            if (m_WorldTheme)
                m_WorldTheme.NotifyGameFinished();

            if (OnGameFinished != null)
                OnGameFinished.Invoke();
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

            return true;
        }
    }
}
