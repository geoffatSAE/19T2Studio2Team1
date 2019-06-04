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
        protected Spark m_Spark;

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
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                GameObject hitObject = hit.collider.gameObject;
                JumpToSpark(hitObject.GetComponent<Spark>());
            }
        }

        private void JumpToSpark(Spark spark)
        {
            if (spark && spark != m_Spark)
            {
                 
            }
        }
    }
}
