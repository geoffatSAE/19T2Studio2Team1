﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Orbits transforms around their parent using Lissajous knot (https://en.wikipedia.org/wiki/Lissajous_knot)
    /// </summary>
    public class Orbiter : MonoBehaviour
    {
        [SerializeField] private Transform[] m_Transforms;              // Transform to orbit around
        public float m_Speed = 1f;                                      // Speed of orbiting

        [Header("Lissajous")]
        public Vector3 m_LissajousN = new Vector3(3f, 4f, 7f);          // N value for lissajous knot
        public Vector3 m_LissajousO = new Vector3(0.1f, 0.7f, 0f);      // O value for lissajous knot
        public float m_Offset = 2.5f;                                   // Amount to offset transforms by from origin
        public float m_Step = 2.5f;                                     // Step in between each transform

        private float m_Time = 0f;          // Time that we have ticked

        void Awake()
        {
            m_Time = 0f;
            Interpolate(0f);
        }

        void Update()
        {
            m_Time += Time.deltaTime * m_Speed;
            Interpolate(m_Time);
        }

        /// <summary>
        /// Interpolates the transforms
        /// </summary>
        /// <param name="time">Time of interpolation (can be any value)</param>
        private void Interpolate(float time)
        {
            if (m_Transforms == null || m_Transforms.Length == 0)
                return;

            for (int i = 0; i < m_Transforms.Length; ++i)
            {
                Transform trans = m_Transforms[i];
                float transTime = time + m_Step * i;

                trans.localPosition = LissajousKnot(m_LissajousN, m_LissajousO, transTime, m_Offset);
            }
        }

        /// <summary>
        /// Parametric equation used for each axis of the knot calculation
        /// </summary>
        private float LSJK(float n, float o, float time, float offset)
        {
            return Mathf.Cos(n * time + o) * offset;
        }

        /// <summary>
        /// Calculates a point along a lissajous knot with offset applied
        /// </summary>
        private Vector3 LissajousKnot(Vector3 n, Vector3 o, float time, float offset)
        {
            Vector3 value = Vector3.zero;
            value.x = LSJK(n.x, o.x, time, offset);
            value.y = LSJK(n.y, o.y, time, offset);
            value.z = LSJK(n.z, o.z, time, offset);
            return value;
        }
    }
}
