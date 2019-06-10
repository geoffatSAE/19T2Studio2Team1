using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Base for the player controller, provides interface for interacting with wires
    /// </summary>
    public class SparkJumper : MonoBehaviour
    {
        /// <summary>
        /// Delegate for when this jumper has jumped to a different spark
        /// </summary>
        /// <param name="spark">Spark that has been jumped to (Can be null)</param>
        public delegate void JumpedToSpark(Spark spark);

        public JumpedToSpark OnJumpToSpark;     // Event for when jumping to new spark, This is called when starting transition

        protected Spark m_Spark;                    // The current spark we are on

        public float m_JumpTime = 0.75f;            // Time for jumping between sparks
        private bool m_JumpingToSpark = false;      // If in process of jumping to spark
        private Spark m_PendingSpark;               // The spark we are jumping to

        /// <summary>
        /// Jumps to given spark (unless already jumping
        /// </summary>
        /// <param name="spark"></param>
        public void JumpToSpark(Spark spark)
        {
            //if (m_JumpingToSpark)
              //  return;

            if (m_Spark)
                m_Spark.SetJumper(null);

            m_Spark = spark;

            //if (m_Spark)
            //   m_Spark.SetJumper(this);

            if (OnJumpToSpark != null)
                OnJumpToSpark.Invoke(m_Spark);

            StartCoroutine(TransitionToSpark(m_Spark));
        }

        private IEnumerator TransitionToSpark(Spark spark)
        {
            m_PendingSpark = spark;

            if (m_PendingSpark)
            {
                m_JumpingToSpark = true;

                Vector3 from = transform.position;

                float end = Time.time + m_JumpTime;
                float now = Time.time;

                while (now < end && m_PendingSpark)
                {
                    now = Time.time;

                    // Alpha we be reverse what we want
                    float alpha = Mathf.Clamp01((end - now) / m_JumpTime);
                    transform.position = Vector3.Lerp(m_PendingSpark.transform.position, from, alpha);

                    yield return null;
                }

                if (m_PendingSpark)
                    m_PendingSpark.SetJumper(this);

                m_PendingSpark = null;

                m_JumpingToSpark = false;
            }
        }

        /// <summary>
        /// Traces the world for other sparks, jumping to them if allowed
        /// </summary>
        /// <param name="origin">Origin of the trace</param>
        /// <param name="direction">Direction of the trace</param>
        protected void TraceSpark(Vector3 origin, Quaternion direction)
        {
            TraceSpark(new Ray(origin, direction * Vector3.forward));   
        }

        /// <summary>
        /// Traces the world for other sparks, jumping to them if allowed
        /// </summary>
        /// <param name="ray">Ray to use</param>
        protected void TraceSpark(Ray ray)
        {
            // Avoid raycast if not required
            if (m_JumpingToSpark)
                return;

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, Physics.AllLayers, QueryTriggerInteraction.Collide))
            {
                GameObject hitObject = hit.collider.gameObject;

                Spark spark = hitObject.GetComponent<Spark>();
                if (spark && spark.CanRide)
                    JumpToSpark(spark);
            }
        }
    }
}
