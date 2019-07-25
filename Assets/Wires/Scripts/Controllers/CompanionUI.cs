﻿using System.Collections;
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
        [SerializeField] private Slider m_ProgressSlider;       // Slider for displaying wire progress
        [SerializeField] private Slider m_BoostSlider;          // Slider for displaying built boost

        void Update()
        {
            if (m_SparkJumper)
            {
                m_ProgressSlider.value = m_SparkJumper.wireProgress;
            }

            if (m_ScoreManager)
            {
                m_ScoreText.text = string.Format(m_ScoreTextFormat, Mathf.FloorToInt(m_ScoreManager.score));
                m_MultiplierText.text = string.Format(m_MultiplierTextFormat, m_ScoreManager.totalMultiplier);
                m_BoostSlider.value = m_ScoreManager.boost;
            }
        }

        /// <summary>
        /// Set if HUD of companion should be rendered
        /// </summary>
        /// <param name="render">Enable HUD</param>
        public void SetRenderHUD(bool render)
        {
            if (m_Canvas)
                m_Canvas.gameObject.SetActive(render);
        }
    }
}
