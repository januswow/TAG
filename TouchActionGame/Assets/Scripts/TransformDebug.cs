using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SkeletonAnimation))]

public class TransformDebug : MonoBehaviour {
		
	// for debug
	public GUIText debug_Event;
	public GUIText debug_AniName;
	public GUIText debug_Grounded;
	public GUIText debug_AnimState;
	
	SpineController2 controller;
	Color groundedColor = Color.black;
	Color animTimelineColor = Color.white;
	//float gorundedLine = 0;

	bool mouseIsDown = false;
	Vector3 mouseDownPos = new Vector3();
	float lastX;
	string lastAnimState;
	int drawTimelineHeight = 10;

	List<float> graphValue = new List<float>();
	List<string> animStateRec = new List<string>();
	List<Color> timelineColorRec = new List<Color>();
	List<int> timelineHeightRec = new List<int>();

	SkeletonAnimation skeletonAnimation;

	void Awake () {
		skeletonAnimation = GetComponent<SkeletonAnimation>();
		
	}

	// Use this for initialization
	void Start () {
		controller = this.GetComponent<SpineController2>();

		skeletonAnimation.state.Start += AnimationStartListener;
		skeletonAnimation.state.End += AnimationEndListener;
	}
	
	// Update is called once per frame
	void Update () {
		// DRAW SCREEN CENTER
		Debug.DrawLine(Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth*0.5f,0,Camera.main.nearClipPlane)), Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth*0.5f,Camera.main.pixelHeight,Camera.main.nearClipPlane)), Color.gray);
		// DRAW JUMP HEIGHT
		Debug.DrawLine(Camera.main.ScreenToWorldPoint(new Vector3(0, Camera.main.pixelHeight*0.7f,Camera.main.nearClipPlane)), Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight*0.7f,Camera.main.nearClipPlane)), Color.gray);
		
		// DISPLAY AnimState
		if (controller.State != lastAnimState) {
			animStateRec.Insert(0, controller.State);
			for (int i = 0; i < animStateRec.Count; i++) {
				if (i==0) {
					debug_AnimState.text = animStateRec[i];
				} else {
					debug_AnimState.text += "\n" + animStateRec[i];
				}
			}
			if (animStateRec.Count > 7) animStateRec.RemoveAt(6);
		}

		// DRAW PATH OF TOUCH
		if (Input.GetMouseButtonDown(0)) {
			mouseDownPos = Input.mousePosition;
		}
		if (Input.GetMouseButton(0)) {
			Debug.DrawLine(Camera.main.ScreenToWorldPoint(new Vector3(mouseDownPos.x, mouseDownPos.y, 10)), Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10)), Color.gray);
		}

		// DRAW PATH OF ROOT'S HEIGHT
		graphValue.Insert(0, transform.position.y);
		//Debug.DrawLine(new Vector3(0, 0, 0), new Vector3(0, transform.position.y, 0), Color.green, Time.deltaTime);
		for (int i = 0; i<graphValue.Count; i++) {
			if (i<graphValue.Count-1) {
				float myX = transform.position.x + i*0.02f;
				Debug.DrawLine(new Vector3(myX, graphValue[i], 0), new Vector3(myX+0.02f, graphValue[i+1], 0), Color.green);
				//Debug.DrawLine(new Vector3(myX, 0, 0), new Vector3(myX, graphValue[i], 0), Color.green);
			}
		}
		if (graphValue.Count > 300) graphValue.RemoveAt(300);

		// DRAW STATUS OF GROUNDED
		if (controller.grounded) {
			debug_Grounded.text = "Grounded";
			if ((transform.position.x - lastX)==0) {
				groundedColor = Color.black;
			} else if ((transform.position.x - lastX)<0) {
				groundedColor = Color.red;
			} else if ((transform.position.x - lastX)>0) {
				groundedColor = Color.yellow;
			}
		} else {
			debug_Grounded.text = "Not Grounded";
			groundedColor = Color.white;
		}
		Debug.DrawLine(new Vector3(transform.position.x-8, transform.position.y, 0), new Vector3(transform.position.x+8, transform.position.y, 0), groundedColor);
		lastX = transform.position.x;
		lastAnimState = controller.State;

		// DRAW ANIMATION TIMELINE
//		timelineStringRec.Insert(0, controller.CurrentAnimation);
//		for (int i = 0; i<timelineStringRec.Count-1; i++)
//		{
//			if (timelineStringRec[i] != timelineStringRec[i+1]) {
//				GUIText timelineStringGUIText = new GUIText();
//				//timelineStringGUIText.text = timelineStringRec[i];
//				timelineStringGUIText.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(i*3, Camera.main.pixelHeight*0.9f,Camera.main.nearClipPlane));
//			}
//			//Debug.DrawLine(Camera.main.ScreenToWorldPoint(new Vector3(i*3, Camera.main.pixelHeight*0.9f,Camera.main.nearClipPlane)), Camera.main.ScreenToWorldPoint(new Vector3((i+1)*3, Camera.main.pixelHeight*0.9f,Camera.main.nearClipPlane)), timelineColorRec[i]);
//		}
		timelineColorRec.Insert(0, animTimelineColor);
		timelineHeightRec.Insert(0, drawTimelineHeight);
		for (int i = 0; i<timelineColorRec.Count-1; i++)
		{
			Color thisFrameColor = (i < 	timelineColorRec.Count) ? timelineColorRec[i] : Color.white;
			Debug.DrawLine(Camera.main.ScreenToWorldPoint(new Vector3(i*2, timelineHeightRec[i],Camera.main.nearClipPlane)), Camera.main.ScreenToWorldPoint(new Vector3((i+1)*2, timelineHeightRec[i],Camera.main.nearClipPlane)), thisFrameColor);
		}
		if (timelineColorRec.Count>400) timelineColorRec.RemoveAt(400);
	}

	public void Event (Spine.AnimationState state, int trackIndex, Spine.Event e) {
		// EVENT DISPLAY
		debug_Event.text = e.ToString();
		debug_Event.fontSize = debug_Event.fontSize != 20 ? 20 : 15;
	}

	// call when Spine animation START
	void AnimationStartListener (Spine.AnimationState state, int trackIndex) {
		// DEBUG animation name
		debug_AniName.text = state.GetCurrent(0).ToString();
		drawTimelineHeight = (drawTimelineHeight == 10) ? 1:10;
		switch (state.GetCurrent(0).ToString()) {
		case "jump" :
			animTimelineColor = Color.green;
			break;
		case "fall" :
			animTimelineColor = Color.red;
			break;
		case "forward" :
			animTimelineColor = Color.blue;
			break;
		case "dashBackward" :
			animTimelineColor = Color.black;
			break;
		default :
			animTimelineColor = Color.white;
			break;
		}

	}

	void AnimationEndListener (Spine.AnimationState state, int trackIndex) {
		// DEBUG animation name
		//debug_AniName.text = state.GetCurrent(0).ToString();
		//animTimelineColor = Color.magenta;
	}
}
