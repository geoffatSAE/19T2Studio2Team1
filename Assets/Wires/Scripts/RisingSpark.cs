﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    public class RisingSpark : MonoBehaviour
    {
        /// <summary>
        /// State of the rising spark
        /// </summary>
        [System.Serializable]
        public struct State
        {
            public Vector3 m_Position;       // Local position
            public Vector3 m_Scale;          // Local scale
        }

        [SerializeField] private State m_StartingState;                 // State we start at
        [SerializeField] private State m_EndingState;                   // State we end at
        [SerializeField] private Transform m_OverrideAnchor;            // The transform we move locally too (will use parent by default)

        [SerializeField] private WiresGameMode m_GameMode;              // Game mode to sync with
        [SerializeField] private float m_FallbackDuration = 600f;       // Fallback duration to use if game mode has no set game length

        void Awake()
        {
            if (!m_OverrideAnchor)
                m_OverrideAnchor = transform.parent;

            // Default spot
            Interpolate(0f);

            if (m_GameMode)
                m_GameMode.OnGameStarted += OnGameStart;
            else
                Debug.LogWarning("Rising spark will not play as no game mode has been provided");
        }

        private void Interpolate(float alpha)
        {
            transform.localPosition = Vector3.Lerp(m_StartingState.m_Position, m_EndingState.m_Position, alpha);
            transform.localScale = Vector3.Lerp(m_StartingState.m_Scale, m_EndingState.m_Scale, alpha);
        }

        private IEnumerator RiseRoutine(float duration)
        {
            if (duration > 0f)
            {
                float end = Time.time + duration;
                while (Time.time < end)
                {
                    float alpha = 1f - Mathf.Clamp01((end - Time.time) / duration);
                    Interpolate(alpha);

                    yield return null;
                }
            }

            Interpolate(1f);
        }

        private void OnGameStart()
        {
            float duration = m_FallbackDuration;
            if (m_GameMode)
            {
                // If we know the games expected length, we can let the routine finish
                // as is instead of relying on when the game actually ends
                WiresArcade arcade = m_GameMode as WiresArcade;
                if (arcade)
                    duration = arcade.arcadeLength;
                else
                    m_GameMode.OnGameFinished += OnGameFinished;
            }

            StartCoroutine(RiseRoutine(duration));
        }

        private void OnGameFinished()
        {
            StopCoroutine("RiseRoutine");
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Helper function for drawing visualization of rising sparks path
        /// </summary>
        /// <param name="localPosition">Local position of spark</param>
        /// <returns>World space position</returns>
        private Vector3 TransformStatePosition(Vector3 localPosition)
        {
            if (m_OverrideAnchor)
                return m_OverrideAnchor.TransformPoint(localPosition);
            else
                return localPosition;
        }
        
        void OnDrawGizmos()
        {
            Vector3 start = Vector3.zero, end = Vector3.zero, origin = Vector3.zero;

            if (Application.isPlaying || m_OverrideAnchor)
            {
                start = TransformStatePosition(m_StartingState.m_Position);
                end = TransformStatePosition(m_EndingState.m_Position);
            }
            else
            {
                // We use parent as default (only gets set in awake, so it's not avaliable in scene view)
                m_OverrideAnchor = transform.parent;

                start = TransformStatePosition(m_StartingState.m_Position);
                end = TransformStatePosition(m_EndingState.m_Position);

                m_OverrideAnchor = null;
            }

            // Line from us
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, start);

            // Line from anchor
            Gizmos.color = Color.blue;
            if (m_OverrideAnchor)
                Gizmos.DrawLine(m_OverrideAnchor.position, start);
            else if (transform.parent)
                Gizmos.DrawLine(transform.parent.position, start);

            // Path
            Gizmos.color = Color.red;
            Gizmos.DrawLine(start, end);

            // Size
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(start, Vector3.Scale(transform.lossyScale, m_StartingState.m_Scale));
            Gizmos.DrawWireCube(end, Vector3.Scale(transform.lossyScale, m_EndingState.m_Scale));
        }
        #endif
    }
}
