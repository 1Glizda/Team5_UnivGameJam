/*
 * Copyright (c) 2020 Razeware LLC
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * Notwithstanding the foregoing, you may not use, copy, modify, merge, publish, 
 * distribute, sublicense, create a derivative work, and/or sell copies of the 
 * Software in any work that is designed, intended, or marketed for pedagogical or 
 * instructional purposes related to programming, coding, application development, 
 * or information technology.  Permission for such use, copying, modification,
 * merger, publication, distribution, sublicensing, creation of derivative works, 
 * or sale is expressly withheld.
 *    
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace RW.MonumentValley
{

    // allows a target Transform to be rotated based on mouse click and drag
    [RequireComponent(typeof(Collider))]
    public class DragSpinner : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public enum SpinAxis
        {
            X,
            Y,
            Z
        }

        // transform to spin
        [SerializeField] private Transform targetToSpin;

        // axis of rotation
        [SerializeField] private SpinAxis spinAxis = SpinAxis.X;

        // used to calculate angle to mouse pointer
        [SerializeField] private Transform pivot;

        // minimum distance in pixels before activating mouse drag
        [SerializeField] private int minDragDist = 10;

        //[SerializeField] private Linker linker;

        // vector from pivot to mouse pointer
        private Vector2 directionToMouse;

        // are we currently spinning?
        private bool isSpinning;

        private bool isActive;

        // angle (degrees) from clicked screen position 
        private float angleToMouse;

        // angle to mouse on previous frame
        private float previousAngleToMouse;

        // Vector representing axis of rotation
        private Vector3 axisDirection;

        public UnityEvent snapEvent;

        private float timeCount;

        void Start()
        {
            switch (spinAxis)
            {
                case (SpinAxis.X):
                    axisDirection = Vector3.right;
                    break;
                case (SpinAxis.Y):
                    axisDirection = Vector3.up;
                    break;
                case (SpinAxis.Z):
                    axisDirection = Vector3.forward;
                    break;
            }
            EnableSpinner(true);
        }

        // begin spin drag
        public void OnBeginDrag(PointerEventData data)
        {
            if (!isActive)
            {
                return;
            }

            isSpinning = true;

            // get the angle to the mouse position on down frame
            Vector3 inputPosition = new Vector3(data.position.x, data.position.y, 0f);
            directionToMouse = inputPosition - Camera.main.WorldToScreenPoint(pivot.position);

            // store the angle to mouse pointer on down frame
            previousAngleToMouse = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;

        }

        // end spin on mouse release; then round to right angle
        public void OnEndDrag(PointerEventData data)
        {
            if (isActive)
            {
                SnapSpinner();
            }
        }

        public void OnDrag(PointerEventData data)
        {
            if (isSpinning && Camera.main != null && pivot != null && isActive)
            {
                // get the angle to the current mouse position
                Vector3 inputPosition = new Vector3(data.position.x, data.position.y, 0f);
                directionToMouse = inputPosition - Camera.main.WorldToScreenPoint(pivot.position);
                angleToMouse = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;

                // if we have dragged a minimum threshold, rotate the target to follow the mouse movements around the pivot
                // (left-handed coordinate system; positive rotations are clockwise)
                if (directionToMouse.magnitude > minDragDist)
                {
                    Vector3 newRotationVector = (previousAngleToMouse - angleToMouse) * axisDirection;
                    targetToSpin.Rotate(newRotationVector);
                    previousAngleToMouse = angleToMouse;
                }
            }
        }

        // release and snap to 90-degrees interval
        private void SnapSpinner()
        {
            isSpinning = false;

            // snap to nearest 90-degree interval
            RoundToRightAngles(targetToSpin);

            // invoke event (e.g. to update the SpinnerControl)
            if (snapEvent != null)
            {
                snapEvent.Invoke();
            }
        }

        // round to nearest 90 degrees
        private void RoundToRightAngles(Transform xform)
        {
            float roundedXAngle = Mathf.Round(xform.eulerAngles.x / 90f) * 90f;
            float roundedYAngle = Mathf.Round(xform.eulerAngles.y / 90f) * 90f;
            float roundedZAngle = Mathf.Round(xform.eulerAngles.z / 90f) * 90f;

            xform.eulerAngles = new Vector3(roundedXAngle, roundedYAngle, roundedZAngle);
        }

        //enable/disable
        public void EnableSpinner(bool state)
        {
            isActive = state;

            // force snap the spinner on disable
            if (!isActive)
            {
                SnapSpinner();
            }
        }


    }

}