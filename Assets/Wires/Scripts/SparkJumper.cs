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

        public JumpedToSpark OnJumpToSpark;     // Event for when jumping to new spark
        protected Spark m_Spark;                // The current spark we are on


        public void JumpToSpark(Spark spark)
        {
            if (m_Spark)
                m_Spark.SetJumper(null);

            m_Spark = spark;

            if (m_Spark)
                m_Spark.SetJumper(this);

            if (OnJumpToSpark != null)
                OnJumpToSpark.Invoke(m_Spark);
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
