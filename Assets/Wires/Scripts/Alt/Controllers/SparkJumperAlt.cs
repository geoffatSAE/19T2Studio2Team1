using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{ 
    /// <summary>
    /// Controller for the player, can not be instanced by itself.
    /// Provides core functionallity for player interaction
    /// </summary>
    public abstract class SparkJumperAlt : MonoBehaviour
    {
        [SerializeField, Min(0f)] float m_JumpTime = 0.75f;
        [SerializeField] LayerMask m_SparkLayer = Physics.AllLayers;

        private bool m_IsJumping = false;

        public void JumpToSpark(SparkAlt spark)
        {
            if (!spark.canJumpTo)
                return;
        }

        public void TraceSpark(Vector3 origin, Quaternion direction)
        {
            TraceSpark(new Ray(origin, direction * Vector3.forward));
        }

        public void TraceSpark(Ray ray)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, m_SparkLayer))
            {
                GameObject hitObject = hit.collider.gameObject;

                SparkAlt spark = hitObject.GetComponent<SparkAlt>();
                if (spark)
                    JumpToSpark(spark);      
            }
        }
    }
}
