using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    public class Wire : MonoBehaviour
    {
        public Spark spark { get; private set; }
        public SparkJumper sparkJumper;

        public float m_Distance = 15f;         // The distance of this wire
        public Vector3 End { get { return transform.position + WireManager.WirePlane * m_Distance; } }

        /// <summary>
        /// Ticks the spark, having it travel along the wire
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <returns></returns>
        public float TickSpark(float deltaTime)
        {
            if (!spark)
                return 1f;

            spark.transform.position += WireManager.WirePlane * spark.m_Speed * Time.deltaTime;

            Vector3 offset = spark.transform.position - transform.position;
            float distance = offset.sqrMagnitude;

            float alpha = distance / (End - transform.position).sqrMagnitude;
            return alpha;
        }

        public void SpawnSpark(Spark prefab)
        {
            spark = Instantiate(prefab, transform.position, Quaternion.identity);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.15f);
            Gizmos.DrawLine(transform.position, transform.position + (WireManager.WirePlane * m_Distance));
        }
    }
}
