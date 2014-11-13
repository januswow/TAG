using UnityEngine;
using System.Collections;
using Spine;
using System;

[RequireComponent(typeof(SkeletonAnimation))]

public class SpineController1 : MonoBehaviour {

	// 判斷是否為swipe行為的距離值
	const int defineSwipe = 500;
	const int defineJump = 4;
	const float defineTap = 0.15f;
	const float defineGravity = 9.8f;

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
	public bool grounded = true;
	bool foreFall = false;
	bool charging = false;
	bool cancelable = true;
	bool recovering = false;

	enum AnimState {
		Ready,
		Forward,
		Backward,
		DashForward,
		DashBackward,
		Jump,
		AirJump,
		AttackUp,
		AttackDn,
		AttackFw,
		AttackBw
	}
	
	AnimState currentState;


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
	}
	
	// Update is called once per frame
	void Update () {
		// CHECK GROUNDED STATUS
		grounded = Physics2D.OverlapArea(collider.bounds.max, collider.bounds.min, layerGround);

		// INPUTS RECEIVER -------------------------------------------

		// CHECK TOUCHED
		if (Input.GetMouseButton(0)) {
			touched = true;
		} else {
			touched = false;
		}
		if (Input.GetMouseButtonDown(0)) {
			touchTimeStart = Time.time;
			touchVecStart = Input.mousePosition;
		}
		if (Input.GetMouseButtonUp(0)) {
			touchTimeEnd = Time.time;
			touchVecEnd = Input.mousePosition;

			touchDuration = touchTimeEnd - touchTimeStart;
			touchDistance = touchVecEnd - touchVecStart;

			if (touchDistance.sqrMagnitude > defineSwipe) {
				Debug.Log("SWIPE");
				touchDirection = touchDistance / touchDistance.magnitude;
				double touchAngle = Math.Atan2(touchVecEnd.y - touchVecStart.y, touchVecEnd.x - touchVecStart.x)/Math.PI*180;

				if (touchAngle < 45 && touchAngle > -45) {
					Debug.Log("ATTACK FW - SWIPE RIGHT");
					AnimStateMachine(AnimState.AttackFw);
					fxAtkHit.transform.position = transform.position + new Vector3(3,3,0);
				} else if (touchAngle > 45 && touchAngle < 135) {
					Debug.Log("ATTACK UP - SWIPE UP");
					AnimStateMachine(AnimState.AttackUp);
					fxAtkHit.transform.position = transform.position + new Vector3(3,5,0);
				} else if (touchAngle < -45 && touchAngle > -135) {
					Debug.Log("ATTACK DN - SWIPE DOWN");
					AnimStateMachine(AnimState.AttackDn);
					fxAtkHit.transform.position = transform.position + new Vector3(3,0,0);
				} else {
					Debug.Log("ATTACK BW - SWIPE LEFT");
					AnimStateMachine(AnimState.AttackBw);
					fxAtkHit.transform.position = transform.position + new Vector3(-3,2,0);
				}
			} else {
				Debug.Log("NOT SWIPE, TAP OR HOLD");
				Vector3 mousePos = (Vector3)touchVecEnd;
				mousePos.z = 10;
				Vector3 moveVector = Camera.main.ScreenToWorldPoint(mousePos) - transform.position;

				if (moveVector.y > defineJump) {
					Debug.Log("JUMP!!");
					if (grounded) {
						Debug.Log("FIRST JUMP!!");
						AnimStateMachine(AnimState.Jump);
					} else {
						Debug.Log("SECOND JUMP!!");
						AnimStateMachine(AnimState.AirJump);
					}
				} else if (moveVector.x > 0){
					if (grounded) {
						Debug.Log("FORWARD!!");
						AnimStateMachine(AnimState.Forward);
					}
				} else {
					if (grounded) {
						Debug.Log("DASHBACKWARD!!");
						AnimStateMachine(AnimState.DashBackward);
					}
				}
			}
		}
	}

	void AnimStateMachine (AnimState m_state) {
		currentState = m_state;
		// m_state IS THE NEXT STATE
		Debug.Log ("STATE MACHINE START UP");
		switch (m_state) {
		case AnimState.Ready :
			SetAnimation(0, "ready", true, false);
			break;
		case AnimState.Forward :
			Debug.Log("FORWARD IN SM");
			SetAnimation(0, "forward", true, true);
			break;
		case AnimState.Backward :
			break;
		case AnimState.DashForward :
			break;
		case AnimState.DashBackward :
			SetAnimation(0, "dashBackward", false, true);
			break;
		case AnimState.Jump :
			SetAnimation(0, "jump", false, true);
			break;
		case AnimState.AirJump :
			SetAnimation(0, "jump", false, true);
			break;
		case AnimState.AttackUp :
			Debug.Log("attackUp");
			SetAnimation(0, "attackForward", false, true);
			break;
		case AnimState.AttackDn :
			Debug.Log("attackDn");
			SetAnimation(0, "attackForward", false, true);
			break;
		case AnimState.AttackFw :
			Debug.Log("attackFw");
			SetAnimation(0, "attackForward", false, true);
			break;
		case AnimState.AttackBw :
			SetAnimation(0, "attackForward", false, true);
			break;
		}
	}

	void ResetAnimation ()
	{

	}

	void SetAnimation (int trackIndex, string name, bool loop, bool instant = true, float delay = 0) {
		// DON NOT SET ANIMATION WHEN THE ANIMATIONS ARE SAME ONE
		if (name != currentAnimation) {
			if (instant) 
			{
				Debug.Log("SET ANIMATION");
				skeletonAnimation.state.SetAnimation(trackIndex, name, loop);
			} else {
				Debug.Log("ADD ANIMATION");
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
					cancelable = false;
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
		
		// GET CURRENT ANIMATION
		currentAnimation = state.GetCurrent(trackIndex).ToString();
		cancelable = false;

		// SET GRAVITY SCALE TO ZERO FOR JUMP ACTIONS
		if (currentAnimation == "jump" || currentAnimation == "airJump") {
			Debug.Log("GRAVITY = 0");
			rigibody.gravityScale = 0;
		}
	}
	
	// call when Spine animation END
	void AnimationEndListener (Spine.AnimationState state, int trackIndex) {
		cancelable = true;

		// SET GRAVITY SCALE TO DEFAULT FOR GROUNDED
		if (currentAnimation == "jump" || currentAnimation == "airJump") {
			Debug.Log("GRAVITY = " + defineGravity.ToString());
			rigibody.gravityScale = defineGravity;
		}
		Debug.Log (stateQueue.Count);
		// IF NEXT STATE IS NOT EXISTS, SET TO READY STATE
		if (stateQueue.Count == 0 && currentState != AnimState.Ready) {
			Debug.Log("RESET STATE TO READY WHEN ANIMATION OVER.");
			AnimStateMachine(AnimState.Ready);
			//SetAnimation(0, "ready", true);
		}
	}
}
