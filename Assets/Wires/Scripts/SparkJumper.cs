using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Liminal.Wires
{
    /// <summary>
    /// Base for the player controller for the wires game mode
    /// </summary>
    public class SparkJumper : MonoBehaviour
    {
        protected Spark m_Spark;
        [SerializeField] private LayerMask m_SparkLayers;

        /// <summary>
        /// Traces the world for other sparks, jumping to them if allowed
        /// </summary>
        /// <param name="origin">Origin of the trace</param>
        /// <param name="direction">Direction of the trace</param>
        protected void TraceSpark(Vector3 origin, Quaternion direction)
        {
            Ray ray = new Ray(origin, direction * Vector3.forward);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, m_SparkLayers, QueryTriggerInteraction.Ignore))
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
