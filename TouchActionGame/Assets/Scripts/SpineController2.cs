using UnityEngine;
using System.Collections;
using Spine;
using System;

[RequireComponent(typeof(SkeletonAnimation))]

public class SpineController2 : MonoBehaviour {

	AnimState m_state = AnimState.Ready;
	AnimState m_queueState = AnimState.Max;

	public string State { get { return m_state.ToString(); } }
	// 判斷是否為swipe行為的距離值
	const int defineSwipe = 500;
	const int defineJump = 4;
	const float defineTap = 0.15f;
	//const float defineGravity = 0.8f;
		
	public ParticleSystem fxAtkHit;
	public ParticleSystem fxFootstep;
	public ParticleSystem fxCharge;

	LayerMask layerGround = new LayerMask();

	BoxCollider2D collider;
	Rigidbody2D rigibody;
	SkeletonAnimation skeletonAnimation;

	Queue stateQueue = new Queue();

	// FOR INPUTS
	bool touched = false;
	float touchTimeStart= 0;
	float touchTimeEnd= 0;
	float touchDuration = 0;
	Vector3 touchVecStart = Vector3.zero;
	Vector3 touchVecEnd = Vector3.zero;
	Vector3 touchDistance = Vector3.zero;
	Vector3 touchDirection = Vector3.zero;

	// FOR PLAYER CONTROLL
	string currentAnimation = "";
	string lastAnimation = "";
	public string CurrentAnimation { get {return currentAnimation; } }
	public string LastAnimation { get {return lastAnimation; } }
	public bool grounded = true;
	int jumpCount = 0;
	bool jumpFinish = false;
	bool charging = false;
	bool cancelable = true;
	bool recovering = false;
	bool queueable = false;
	float queueableTime = 0;
	float queueDuration = 0.3f;

	enum AnimState {
		Ready,
		Forward,
		Backward,
		DashForward,
		DashBackward,
		Jump,
		AirJump,
		Fall,
		Attack,
		Max
	}

	enum MouseCmd
	{
		Swipe_Up = 0,
		Swipe_Down,
		Swipe_Left,
		Swipe_Right,
		Tap_Up,
		Tap_Down,
		Tap_Left,
		Tap_Right,
		Cmd_Max
	}

	//AnimState currentState;

	void Awake () {
		layerGround = LayerMask.GetMask("Ground");

		skeletonAnimation = GetComponent<SkeletonAnimation>();
		collider = GetComponent<BoxCollider2D>();
		rigibody = GetComponent<Rigidbody2D>();
	}

	// Use this for initialization
	void Start () {
		// EVENT SETUP
		skeletonAnimation.state.Event += Event;
		skeletonAnimation.state.Start += AnimationStartListener;
		skeletonAnimation.state.End += AnimationEndListener;
		skeletonAnimation.state.Complete += AnimationCompleteListener;

		m_state = AnimState.Ready;
		SetAnimation(0, "ready", true);

		skeletonAnimation.state.TimeScale = .7f;
	}
	
	// Update is called once per frame
	void Update () {
		//Debug.DrawLine(Camera.main.WorldToViewportPoint(new Vector3(0,0,0)), new Vector3(1,1,0), Color.cyan, Time.deltaTime);
		if (Time.time > queueableTime) queueable = true;
		
		bool lastGrounded = grounded;
		// UPDATE GROUNDED STATUS
		grounded = Physics2D.OverlapArea(collider.bounds.max, collider.bounds.min, layerGround);

		// RESET ANIMATION TO READY WHEN CHARACTER HITS GROUND
		if (lastGrounded == false && grounded == true) {
			jumpCount = 0;
			jumpFinish = false;
			ResetAnimation();
		}

		// INPUTS RECEIVER -------------------------------------------
		// UPDATE VALUES FOR TOUCH
		if (Input.GetMouseButton(0)) {
			touched = true;
		} else {
			touched = false;
		}
		if (Input.GetMouseButtonDown(0)) {
			touchTimeStart = Time.time;
			touchVecStart = Input.mousePosition;
		}
		if (Input.GetMouseButtonUp(0)) 
		{
			MouseCmd cmd = MouseCmd.Cmd_Max;

			touchTimeEnd = Time.time;
			touchVecEnd = Input.mousePosition;

			touchDuration = touchTimeEnd - touchTimeStart;
			touchDistance = touchVecEnd - touchVecStart;

			// TOUCH BEHAVIOR PARSING
			if (touchDistance.sqrMagnitude > defineSwipe) {
				touchDirection = touchDistance / touchDistance.magnitude;
				double touchAngle = Math.Atan2(touchVecEnd.y - touchVecStart.y, touchVecEnd.x - touchVecStart.x)/Math.PI*180;

				if (touchAngle > 45 && touchAngle < 135)
				{
					cmd = MouseCmd.Swipe_Up;
					fxAtkHit.transform.position = transform.position + new Vector3(3,5,0);
				} 
				else if (touchAngle < -45 && touchAngle > -135)
				{
					cmd = MouseCmd.Swipe_Down;
					fxAtkHit.transform.position = transform.position + new Vector3(3,0,0);
				}
				else if (touchAngle > 135 && touchAngle < -135)
				{
					cmd = MouseCmd.Swipe_Left;
					fxAtkHit.transform.position = transform.position + new Vector3(-3,2,0);
				}
				else 
				{
					cmd = MouseCmd.Swipe_Right;
					fxAtkHit.transform.position = transform.position + new Vector3(3,3,0);
				}
			} 
			else 
			{
				//Debug.Log("======TAP OR HOLD======");
				Vector3 mousePos = (Vector3)touchVecEnd;
				mousePos.z = Camera.main.nearClipPlane;
				Vector3 moveVector = Camera.main.ScreenToWorldPoint(mousePos) - Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth*0.5f,0,Camera.main.nearClipPlane));

				if (touchVecEnd.y > Camera.main.pixelHeight*0.7f) {
				//if (moveVector.y > defineJump) {
					//Debug.Log("INPUT - UP");
					cmd = MouseCmd.Tap_Up;
					//	TODO
					if (grounded) 
					{
						//Debug.Log("FIRST JUMP!!");
						//AnimStateMachine(AnimState.Jump);
					} 
					else
					{
						//Debug.Log("SECOND JUMP!!");
						//AnimStateMachine(AnimState.AirJump);
					}
				} 
				else if (moveVector.x > 0)
				{
					if (grounded)
					{
						cmd = MouseCmd.Tap_Right;
					}
				} 
				else 
				{
					if (grounded) 
					{
						//Debug.Log("DASHBACKWARD!!");
						cmd = MouseCmd.Tap_Left;
						//AnimStateMachine(AnimState.DashBackward);
					}
				}
			}

			OnCommand(cmd);
		}
	}

	void OnCommand (MouseCmd cmd) {

		// m_state IS THE FOLLOWING STATE
		//Debug.Log ("Now State:" + m_state + " , cmd:" + cmd);
		switch (m_state) 
		{
		case AnimState.Ready :
			switch(cmd)
			{
			case MouseCmd.Tap_Up:
				SetAnimation(0, "jump", false, true);
				m_state = AnimState.Jump;
				break;
			case MouseCmd.Tap_Down:
				break;
			case MouseCmd.Tap_Left:
				SetAnimation(0, "dashBackward", false, true);
				m_state = AnimState.DashBackward;
				break;
			case MouseCmd.Tap_Right:
				skeletonAnimation.state.Start += StartEventForward;
				SetAnimation(0, "forward", true, true);
				m_state = AnimState.Forward;
				break;
			case MouseCmd.Swipe_Right:
				SetAnimation(0, "attackForward", false, true);
				m_state = AnimState.Attack;
				break;
			}
			break;

		case AnimState.Forward :
			switch(cmd)
			{
			case MouseCmd.Tap_Up:
				SetAnimation(0, "jump", false, true);
				m_state = AnimState.Jump;
				break;
			case MouseCmd.Tap_Down:
				break;
			case MouseCmd.Tap_Left:
				SetAnimation(0, "dashBackward", true);
				m_state = AnimState.DashBackward;
				break;
			case MouseCmd.Tap_Right:
				break;
			case MouseCmd.Swipe_Right:
				SetAnimation(0, "attackForward", false, true);
				m_state = AnimState.Attack;
				break;
			}
			break;
		case AnimState.Backward :
			break;
		case AnimState.DashForward :
			break;
		case AnimState.DashBackward :
			switch(cmd)
			{
			case MouseCmd.Tap_Up:
				if (queueable) {
					SetAnimation(0, "jump", false, true);
					m_state = AnimState.Jump;
				}
				//m_queueState = AnimState.Jump;
				break;
			case MouseCmd.Tap_Down:
				break;
			case MouseCmd.Tap_Left:
				break;
			case MouseCmd.Tap_Right:
				//SetAnimation(0, "ready", true);
				//m_state = AnimState.Ready;
				break;
			}
			break;
		case AnimState.Jump :
			switch(cmd)
			{
			case MouseCmd.Tap_Up:
				if (!jumpFinish) {
					skeletonAnimation.state.SetAnimation(0, "jump", false);
					m_state = AnimState.AirJump;
				}
				break;
			case MouseCmd.Tap_Down:
				break;
			case MouseCmd.Tap_Left:
				break;
			case MouseCmd.Tap_Right:
				break;
			}
			break;
		case AnimState.AirJump :
			break;
		case AnimState.Fall :
			switch(cmd)
			{
			case MouseCmd.Tap_Up:
				if (!jumpFinish) {
					skeletonAnimation.state.SetAnimation(0, "jump", false);
					//SetAnimation(0, "jump", false, true);
					m_state = AnimState.AirJump;
				}
				break;
			}
			break;
		case AnimState.Attack :
			switch(cmd)
			{
			case MouseCmd.Tap_Up:
				SetAnimation(0, "jump", false, true);
				m_state = AnimState.Jump;
				break;
			case MouseCmd.Tap_Down:
				break;
			case MouseCmd.Tap_Left:
				SetAnimation(0, "dashBackward", true, true);
				m_state = AnimState.DashBackward;
				break;
			case MouseCmd.Tap_Right:
				SetAnimation(0, "forward", true, true);
				m_state = AnimState.Forward;
				break;
			case MouseCmd.Swipe_Right:
				SetAnimation(0, "attackForward", false, false);
				m_state = AnimState.Attack;
				break;
			}
			break;

		}
	}


	void ResetAnimation ()
	{
		m_state = AnimState.Ready;
		SetAnimation(0, "ready", true);
		rigibody.isKinematic = false;
	}

	void SetAnimation (int trackIndex, string name, bool loop, bool instant = true, float delay = 0) {
		// DON NOT SET ANIMATION WHEN THE ANIMATIONS ARE SAME ONE
		if (name != currentAnimation) {
			if (instant) 
			{
				//Debug.Log("SET ANIMATION: " + name);
				skeletonAnimation.state.SetAnimation(trackIndex, name, loop);
			} else {
				//Debug.Log("ADD ANIMATION: " + name);
				skeletonAnimation.state.AddAnimation(trackIndex, name, loop, delay);
			}
		}
	}

	// DEFINE EVENT FUNCTIONS ---------------------------------------------------------------
	public void Event (Spine.AnimationState state, int trackIndex, Spine.Event e) {
		//Debug.Log(trackIndex + " " + state.GetCurrent(trackIndex) + ": event " + e + ", " + e.Int);

		// EVENT LISTENER
		switch (e.ToString()) {
			case "footstep" :
				fxFootstep.Play();
				break;
			case "charge" :
				if (charging) {
					// CHARGE END
					fxCharge.Stop();
					charging = false;
					//cancelable = false;
				} else {
					// START CHARGING
					fxCharge.Play();
					charging = true;
				}
				break;
			case "recovery" :
				cancelable = true;
				break;
		}
	}
	
	// call when Spine animation START
	void AnimationStartListener (Spine.AnimationState state, int trackIndex) {
		//Debug.Log ("[Start]: " + state);
		// GET CURRENT ANIMATION
		currentAnimation = state.GetCurrent(trackIndex).ToString();
		cancelable = false;

		queueable = false;
		queueableTime = Time.time + state.GetCurrent(trackIndex).endTime - queueDuration;

		// SET isKinematic TO TRUE, SO CHARACTER WILL NOT EFFECT BY GRAVITY
		if (currentAnimation == "jump" || currentAnimation == "airJump") {
			rigibody.isKinematic = true;
			jumpCount += 1;
			if (jumpCount > 1 ) {
				jumpFinish = true;
			}
		}
	}
	
	// call when Spine animation COMPLETE
	void AnimationCompleteListener (Spine.AnimationState state, int trackIndex , int loopCount) {
		lastAnimation = state.GetCurrent(trackIndex).ToString();
		cancelable = true;
		if (currentAnimation == "ready" || currentAnimation == "forward")
			return;
		//Debug.Log ("[COMPLETE]: " + state);
		//Debug.Log (stateQueue.Count);
		// IF NEXT STATE IS NOT EXISTS, SET TO READY STATE
		if (stateQueue.Count == 0 ) {
			//Debug.Log("RESET STATE TO READY WHEN ANIMATION OVER.");
			if (grounded) {
				ResetAnimation();
			} else {
				m_state = AnimState.Fall;
				SetAnimation(0, "fall", true);
			}
		}
	}

	// call when Spine animation END
	void AnimationEndListener (Spine.AnimationState state, int trackIndex) {
		
		// SET isKinematic TO FALSE, SO CHARACTER WILL EFFECT BY GRAVITY
		if (currentAnimation == "jump" || currentAnimation == "airJump") {
			rigibody.isKinematic = false;
		}

	}

	void StartEventForward (Spine.AnimationState state, int trackIndex) {
		//skeletonAnimation.state.End += StartEventForward;
		Debug.Log("BBBBBBBBBBBBBBBBBBBBBB");
	}
}
