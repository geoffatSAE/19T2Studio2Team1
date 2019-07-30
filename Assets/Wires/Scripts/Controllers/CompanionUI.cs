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

        [Header("Game")]
        public string m_ScoreTextFormat = "Score: {0}";         // Formatting for score text ({0} is required, will be replaced by actual score)
        public string m_MultiplierTextFormat = "X{0}";          // Formatting for multiplier text ({0} is required, will be replaced by actual multiplier)

        [SerializeField] private Canvas m_GameCanvas;           // Canvas for companions game UI
        [SerializeField] private Text m_ScoreText;              // Text block for writing players score
        [SerializeField] private Text m_MultiplierText;         // Text block for writing players multiplier
        [SerializeField] private Image m_MultiplierImage;       // Image to manipulate based on auto multiplier increase
        [SerializeField] private Slider m_BoostSlider;          // Slider for displaying built boost
        [SerializeField] private Slider m_WireSlider;           // Slider for displaying wire progress
        [SerializeField] private Image[] m_LivesList;           // Images used for displaying lives 
        public Color m_LifeActiveColor = Color.white;           // Color to use for active lives
        public Color m_LifeInactiveColor = Color.gray;          // Color to use for inactive lives

        [Header("Stats")]
        public string m_ScoreStatsTextFormat = "Score: {0}";                // Formatting for score text on the stats UI ({0} is required, will be replaced by actual score)
        public string m_DistanceStatsTextFormat = "Distance: {0:0.0}m";     // Formatting for distance text on the stats UI ({0} is required, will be replaced by actual distance)

        [SerializeField] private Canvas m_StatsCanvas;          // Canvas for companions stats UI
        [SerializeField] private Text m_ScoreStatsText;         // Text block for writing players final score
        [SerializeField] private Text m_DistanceStatsText;      // Text block for writing players distance travelled

        private bool m_DisplayGameUI = true;    // If game UI should be displayed
        private int m_ActiveLives = 0;          // Amount of lives player has

        public Text m_FPSText;

        int frames = 0;
        int fps = 0;
        float time = 0f;

        void Update()
        {
            if (m_DisplayGameUI)
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

            time += Time.deltaTime;
            frames++;

            if (time >= 1f)
            {
                fps = frames;
                frames = 0;
                time = Mathf.Repeat(time, 1f);
            }

            if (m_FPSText)
                m_FPSText.text = string.Format("FPS: {0}", fps);
        }

        void OnEnable()
        {
            ToggleEnabledUI(m_DisplayGameUI);

            if (m_ScoreManager)
            {
                m_ScoreManager.OnStageLivesUpdated += StageLivesUpdated;
                RefreshLivesList(m_ScoreManager.remainingLives);
            }
        }

        void OnDisable()
        {
            SetDisplayCanvases(false, false);

            if (m_ScoreManager)
                m_ScoreManager.OnStageLivesUpdated -= StageLivesUpdated;
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
