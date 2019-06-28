using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace TO5
{
    /// <summary>
    /// Simple object pool designed for unity objects
    /// </summary>
    /// <typeparam name="T">Any Unity.Object</typeparam>
    public class ObjectPool<T> where T : Object
    {
        // The amount of objects in the pool
        public int Count { get { return m_Objects.Count; } }

        // The amount of objects active
        public int activeCount { get { return m_ActiveCount; } }

        // If an object can be activated
        public bool canActivateObject { get { return m_Objects.Count > 0 && m_ActiveCount < m_Objects.Count; } }

        private List<T> m_Objects = new List<T>();      // List of objects
        private int m_ActiveCount = 0;                  // Amount of objects active

        /// <summary>
        /// Adds an object to the pool
        /// </summary>
        /// <param name="value">Object to add</param>
        public void Add(T value)
        {
            m_Objects.Add(value);
        }

        /// <summary>
        /// Removes an object from the pool
        /// </summary>
        /// <param name="value">Object to remove</param>
        public void Remove(T value)
        {
            int index = m_Objects.FindIndex(item => item == value);
            if (index != -1)
            {
                if (index < m_ActiveCount)
                    --m_ActiveCount;

                m_Objects.RemoveAt(index);
            }
        }

        /// <summary>
        /// Activates the first pooled object
        /// </summary>
        /// <returns>Activated object or null if none remaining</returns>
        public T ActivateObject()
        {
            if (canActivateObject)
            {
                T value = m_Objects[m_ActiveCount];
                ++m_ActiveCount;
                return value;
            }

            return null;
        }

        /// <summary>
        /// Deactivates the given object
        /// </summary>
        /// <param name="value">Object to deactivate</param>
        public void DeactivateObject(T value)
        {
            int index = m_Objects.FindIndex(item => item == value);
            if (index != -1)
            {
                // Object isn't active if equal or greater
                if (index < m_ActiveCount)
                {
                    Assert.IsTrue(m_ActiveCount > 0);

                    Swap(index, m_ActiveCount - 1);
                    --m_ActiveCount;
                }
            }
        }

        /// <summary>
        /// Clears the pool
        /// </summary>
        /// <param name="destroyObjects">If all objects in pool should be destroyed</param>
        public void Clear(bool destroyObjects)
        {
            if (destroyObjects)
                foreach (T value in m_Objects)
                    Object.Destroy(value);

            m_Objects.Clear();
            m_ActiveCount = 0;
        }

        /// <summary>
        /// Get object at index
        /// </summary>
        /// <param name="index">Index of object</param>
        /// <returns>Object at index</returns>
        public T GetObject(int index)
        {
            if (index >= 0 && index < m_Objects.Count)
                return m_Objects[index];

            return null;
        }

        /// <summary>
        /// Swaps values at indices
        /// </summary>
        /// <param name="lhs">First index</param>
        /// <param name="rhs">Second index</param>
        private void Swap(int lhs, int rhs)
        {
            T l = m_Objects[lhs];
            T r = m_Objects[rhs];

            m_Objects[lhs] = r;
            m_Objects[rhs] = l;
        }
    }
}
