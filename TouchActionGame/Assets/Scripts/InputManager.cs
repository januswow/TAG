using System.Collections;
using UnityEngine;
using common;

public class InputManager : MonoBehaviour {

	public SpineController controller;

	// INPUT DETECT SETTINGS
	const int defineSwipe = 500;
	const int defineJump = 4;
	const float defineTap = 0.15f;

	// FOR INPUTS
	bool touched = false;
	float touchTimeStart= 0;
	float touchTimeEnd= 0;
	float touchDuration = 0;
	Vector3 touchVecStart = Vector3.zero;
	Vector3 touchVecEnd = Vector3.zero;
	Vector3 touchDistance = Vector3.zero;
	Vector3 touchDirection = Vector3.zero;

//	// MOUSE STATE
//	enum MouseCmd
//	{
//		Swipe_Up = 0,
//		Swipe_Down,
//		Swipe_Left,
//		Swipe_Right,
//		Tap_Up,
//		Tap_Down,
//		Tap_Left,
//		Tap_Right,
//		Cmd_Max
//	}

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		// INPUTS RECEIVER -------------------------------------------

		// UPDATE TOUCH STATUS
		if (Input.GetMouseButton(0)) {
			touched = true;
		} else {
			touched = false;
		}

		// TOUCH DOWN
		if (Input.GetMouseButtonDown(0)) {
			touchTimeStart = Time.time;
			touchVecStart = Input.mousePosition;
		}

		// TOUCH UP
		if (Input.GetMouseButtonUp(0)) 
		{
			MouseCmd cmd = MouseCmd.Cmd_Max;

			touchTimeEnd = Time.time;
			touchVecEnd = Input.mousePosition;
			
			touchDuration = touchTimeEnd - touchTimeStart;
			touchDistance = touchVecEnd - touchVecStart;
			
			// TOUCH BEHAVIOR PARSING
			if (touchDistance.sqrMagnitude > defineSwipe) {
				// SWIPE
				touchDirection = touchDistance / touchDistance.magnitude;
				double touchAngle = Math.Atan2(touchVecEnd.y - touchVecStart.y, touchVecEnd.x - touchVecStart.x)/Math.PI*180;

				// GET SWIPE DIRECTION
				if (touchAngle > 45 && touchAngle < 135)
				{
					// SWIPE UP
					cmd = MouseCmd.Swipe_Up;
				} 
				else if (touchAngle < -45 && touchAngle > -135)
				{
					// SWIPE DOWN
					cmd = MouseCmd.Swipe_Down;
				}
				else if (touchAngle > 135 && touchAngle < -135)
				{
					// SWIPE LEFT
					cmd = MouseCmd.Swipe_Left;
				}
				else 
				{
					// SWIPE RIGHT
					cmd = MouseCmd.Swipe_Right;
				}
			} 
			else 
			{
				// IS NOT SWIPE
				Vector3 mousePos = (Vector3)touchVecEnd;
				mousePos.z = Camera.main.nearClipPlane;
				Vector3 moveVector = Camera.main.ScreenToWorldPoint(mousePos) - Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth*0.5f,0,Camera.main.nearClipPlane));
				
				if (touchVecEnd.y > Camera.main.pixelHeight*0.7f) {
					// TAP UP
					cmd = MouseCmd.Tap_Up;
				} 
				else if (moveVector.x > 0)
				{
					// TAP RIHGT
					cmd = MouseCmd.Tap_Right;
				} 
				else 
				{
					// TAP LEFT
					cmd = MouseCmd.Tap_Left;
				}
			}			
			controller.OnCommand(cmd);
		}
	}
}

