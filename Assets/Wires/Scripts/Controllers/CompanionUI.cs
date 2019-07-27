using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TO5.Wires
{
    /// <summary>
    /// Handles the UI displayed on the players companion
    /// </summary>
    public class CompanionUI : MonoBehaviour
    {
        [SerializeField] private SparkJumper m_SparkJumper;         // Players spark jumper
        [SerializeField] private ScoreManager m_ScoreManager;       // Games score manager

        public string m_ScoreTextFormat = "Score: {0}";         // Formatting for score text ({0} is required, will be replaced by actual score)
        public string m_MultiplierTextFormat = "X{0}";          // Formatting for multiplier text ({0} is required, will be replaced by actual multiplier)

        [SerializeField] private Canvas m_Canvas;               // Canvas for companions HUD
        [SerializeField] private Text m_ScoreText;              // Text block for writing players score
        [SerializeField] private Text m_MultiplierText;         // Text block for writing players multiplier
        [SerializeField] private Image m_MultiplierImage;       // Image to manipulate based on auto multiplier increase
        [SerializeField] private Slider m_BoostSlider;          // Slider for displaying built boost
        [SerializeField] private Slider m_WireSlider;           // Slider for displaying wire progress
        [SerializeField] private Image[] m_LivesList;           // Images used for displaying lives 
        public Color m_LifeActiveColor = Color.white;           // Color to use for active lives
        public Color m_LifeInactiveColor = Color.gray;          // Color to use for inactive lives

        private int m_ActiveLives = 0;          // Amount of lives player has

        void Update()
        {
            if (m_SparkJumper)
                RefreshWireProgress(m_SparkJumper.wireProgress);

            if (m_ScoreManager)
            {
                RefreshScoreText(m_ScoreManager.score);
                RefreshMultiplierText((int)m_ScoreManager.totalMultiplier);
                RefreshMultiplierProgress(m_ScoreManager.multiplierProgress);
                RefreshBoostProgress(m_ScoreManager.boost);
            }
        }

        void OnEnable()
        {
            if (m_Canvas)
                m_Canvas.gameObject.SetActive(true);

            if (m_ScoreManager)
            {
                m_ScoreManager.OnStageLivesUpdated += StageLivesUpdated;
                RefreshLivesList(m_ScoreManager.remainingLives);
            }
        }

        void OnDisable()
        {
            if (m_Canvas)
                m_Canvas.gameObject.SetActive(false);

            if (m_ScoreManager)
                m_ScoreManager.OnStageLivesUpdated -= StageLivesUpdated;
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
        /// Refreshes boost progress
        /// </summary>
        /// <param name="progress">Boost progress</param>
        public void RefreshBoostProgress(float progress)
        {
            if (m_BoostSlider)
                m_BoostSlider.value = progress;
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
        /// Refresh list of lives to match lives given
        /// </summary>
        /// <param name="lives">Lives remaining in stage</param>
        public void RefreshLivesList(int lives)
        {
            if (lives == m_ActiveLives)
                return;

            // Activate disabled images
            if (lives > m_ActiveLives)
            {
                int max = Mathf.Min(lives, m_LivesList.Length);
                for (int i = m_ActiveLives; i < max; ++i)
                {
                    Image image = m_LivesList[i];
                    if (image)
                        image.color = m_LifeActiveColor;
                }
            }
            // Deactivate enabled images
            else if (lives < m_LivesList.Length)
            {
                int max = Mathf.Max(m_ActiveLives, m_LivesList.Length);
                for (int i = lives; i < max; ++i)
                {
                    Image image = m_LivesList[i];
                    if (image)
                        image.color = m_LifeInactiveColor;
                }
            }

            m_ActiveLives = lives;
        }

        /// <summary>
        /// Notify that players stage lives has changed
        /// </summary>
        /// <param name="lives">Current amount of lives</param>
        private void StageLivesUpdated(int lives)
        {
            RefreshLivesList(lives);
        }
    }
}
