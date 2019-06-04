using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    public class WireSegment : MonoBehaviour
    {
        private Transform m_Start;      // Start of this section
        private Transform m_End;        // End of this section

        public bool isValid { get { return m_Start != null && m_End != null; } }

        public bool Travel(Spark spark, out float over)
        {
            bool finished = false;
            over = 0f;
            if (isValid)
            {
                float distance = (m_End.position - m_Start.position).magnitude;
                float travelled = (spark.transform.position - m_Start.position).magnitude;

                float alpha = travelled / distance;
                float step = spark.m_Speed * Time.deltaTime;

                float newAlpha = Mathf.Min(1f, alpha + step);

                spark.transform.position = Vector3.Lerp(m_Start.position, m_End.position, newAlpha);
                
                if (newAlpha >= 1f)
                {
                    finished = true;
                    over = alpha + step - 1f;
                }
            }

            return finished;
        }

        public void SetSparkAtPoint(Spark spark, float alpha)
        {
            if (isValid)
                spark.transform.position = Vector3.Lerp(m_Start.position, m_End.position, alpha);
        }
    }
}
