using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TO5
{
    /// <summary>
    /// Input module for PC UI interaction
    /// Thank you to EmmaEwert (https://forum.unity.com/threads/fake-mouse-position-in-4-6-ui-answered.283748/, not perfect but it works) 
    /// </summary>
    public class InputModulePC : StandaloneInputModule
    {
        // StandaloneInputModule Interface
        protected override MouseState GetMousePointerEventData(int id)
        {
            CursorLockMode lockState = Cursor.lockState;

            Cursor.lockState = CursorLockMode.None;
            MouseState mouseState = base.GetMousePointerEventData(id);

            Cursor.lockState = lockState;

            return mouseState;
        }

        // StandaloneInputModule Interface
        protected override void ProcessMove(PointerEventData pointerEvent)
        {
            CursorLockMode lockState = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;

            base.ProcessMove(pointerEvent);

            Cursor.lockState = lockState;
        }

        // StandaloneInputModule Interface
        protected override void ProcessDrag(PointerEventData pointerEvent)
        {
            CursorLockMode lockState = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;

            base.ProcessDrag(pointerEvent);

            Cursor.lockState = lockState;
        }
    }
}
