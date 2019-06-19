using System.Collections.Generic;
using UnityEngine;

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

        // If an object can be activated
        public bool canActivateObject { get { return m_FirstPooledObject != null; } }

        private LinkedList<T> m_Objects = new LinkedList<T>();      // List of objects
        private LinkedListNode<T> m_FirstPooledObject = null;       // Node of first pooled object

        /// <summary>
        /// Adds an object to the pool
        /// </summary>
        /// <param name="value">Object to add</param>
        public void Add(T value)
        {
            m_FirstPooledObject = m_Objects.AddLast(value);
        }

        /// <summary>
        /// Removes an object from the pool
        /// </summary>
        /// <param name="value">Object to remove</param>
        public void Remove(T value)
        {
            LinkedListNode<T> node = m_Objects.Find(value);
            if (node != null)
            {
                if (node == m_FirstPooledObject)
                    m_FirstPooledObject = node.Next;

                m_Objects.Remove(node);
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
                T value = m_FirstPooledObject.Value;
                m_FirstPooledObject = m_FirstPooledObject.Next;
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
            LinkedListNode<T> node = m_Objects.Find(value);
            if (node != null)
            {
                m_Objects.Remove(node);
                m_Objects.AddLast(node);
            }
        }
    }
}
