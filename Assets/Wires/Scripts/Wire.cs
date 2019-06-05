using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    public class Wire : MonoBehaviour
    {
        public Spark spark { get; private set; }

        public float m_Distance = 15f;         // The distance of this wire
        public Vector3 End { get { return transform.position + WireManager.WirePlane * m_Distance; } }

        public SparkJumper m_SparkJumper;      // The jumper that is on this wire

        /// <summary>
        /// Ticks the spark, having it travel along the wire
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <returns></returns>
        public float TickSpark(float deltaTime)
        {
            if (!spark)
                return 1f;

            //spark.transform.position += WireManager.WirePlane * spark.m_Speed * Time.deltaTime;

            Vector3 offset = spark.transform.position - transform.position;
            float distance = offset.sqrMagnitude;

            if (m_SparkJumper)
            {
                Vector3 jumperPosition = m_SparkJumper.transform.position;
                jumperPosition.z = spark.transform.position.z;
                m_SparkJumper.transform.position = jumperPosition;
            }


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
