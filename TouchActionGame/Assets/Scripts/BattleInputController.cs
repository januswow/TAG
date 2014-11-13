using UnityEngine;
using System.Collections;
using Spine;
using System;

[RequireComponent(typeof(SkeletonAnimation))]

public class BattleInputController : MonoBehaviour {

	// 判斷是否為swipe行為的距離值
	const int defineSwipe = 500;
	const int defineJump = 4;
	const float defineTap = 0.15f;

	public ParticleSystem fx;
	public ParticleSystem fxFootstep;
	public ParticleSystem fxCharge;

	LayerMask layerGround = new LayerMask();

	BoxCollider2D collider;

	SkeletonAnimation skeletonAnimation;
	string currentAnimation = "";

	//Spine.AnimationState myState = new Spine.AnimationState(skeletonDataAsset.GetAnimationStateData());

	// for touch inputs
	bool touched = false;
	float touchTimeStart= 0;
	float touchTimeEnd= 0;
	float touchDuration = 0;
	Vector2 touchVecStart = Vector2.zero;
	Vector2 touchVecEnd = Vector2.zero;
	Vector2 touchDistance = Vector2.zero;

	// for player controll
	public bool grounded = true;
	bool forceFall = false;
	bool charging = false;
	bool cancelable = true;
	bool recovering = false;

	float nonCancelableTime;

//	enum ANIMATION_STATE {
//		string animationName,
//	}

	// for debug
	public GUIText debug_Event;
	public GUIText debug_AniName;

	void Awake() {
		skeletonAnimation = GetComponent<SkeletonAnimation>();

		layerGround = LayerMask.GetMask("Ground");
		collider = transform.GetComponent<BoxCollider2D>();
	}

	// Use this for initialization
	void Start () {
		skeletonAnimation.state.Event += Event;
		skeletonAnimation.state.Start += AnimationStartListener;
		skeletonAnimation.state.End += AnimationEndListener;
	}
	
	// Update is called once per frame
	void Update () {

		grounded = Physics2D.OverlapArea(collider.bounds.max, collider.bounds.min, layerGround);

		// mouse left button down
		if (Input.GetMouseButtonDown(0)) {
			//Debug.Log("** LMB DOWN");

			touched = true;
			touchTimeStart = Time.time;
			touchVecStart = Input.mousePosition;
		}

		// mouse left button up
		if (Input.GetMouseButtonUp(0)) {
			//Debug.Log("** LMB UP");

			touched = false;
			touchTimeEnd = Time.time;
			touchVecEnd = Input.mousePosition;

			// calculation
			touchDuration = touchTimeEnd - touchTimeStart;
			touchDistance = touchVecEnd - touchVecStart;

			// check if touch distance is long enough
			if (touchDistance.sqrMagnitude > 500) {
				//Debug.Log("SWIPE!!");
				Vector2 touchDirection = touchDistance / touchDistance.magnitude;
				//Debug.Log(touchDirection);
				//Debug.Log(Math.Atan((touchVecEnd.y - touchVecStart.y) / (touchVecEnd.x - touchVecStart.x)) * 180 / Math.PI);
				double touchAngle = Math.Atan2(touchVecEnd.y - touchVecStart.y, touchVecEnd.x - touchVecStart.x)/Math.PI*180;
				Debug.Log(touchAngle);
				if (touchAngle < 45 && touchAngle > -45) {
					//Debug.Log("SWIPE RIGHT");
					PlayerBattleSystem("attackF");
					fx.transform.position = transform.position + new Vector3(3,3,0);
				} else if (touchAngle > 45 && touchAngle < 135) {
					//Debug.Log("SWIPE UP");
					PlayerBattleSystem("attackU");
					fx.transform.position = transform.position + new Vector3(3,5,0);
				} else if (touchAngle < -45 && touchAngle > -135) {
					//Debug.Log("SWIPE DOWN");
					fx.transform.position = transform.position + new Vector3(3,0,0);
					PlayerBattleSystem("attackD");
				} else {
					//Debug.Log("SWIPE LEFT");
					fx.transform.position = transform.position + new Vector3(-3,2,0);
					PlayerBattleSystem("attackB");
				}
			} else {
				// not swipe, here can be tap or hold
				if (touchDuration < defineTap) {
					// tap - move
					//Debug.Log("TAP!!");
					PlayerLocomotion();
				} else {
					// hold - charge
					//Debug.Log("HOLD!!");
				}
			}
		}
	}



	void PlayerBattleSystem (string name) {
		fx.Play();
		if (name == "attackF") {
			SetAnimation(0, "attackForward", false, true);
			ResetAnimation();
		}
	}

	void SetAnimation (int trackIndex, string name, bool loop, bool instant = false) {
		if (name != currentAnimation) {
			skeletonAnimation.state.SetAnimation(trackIndex, name, loop);
//			if (instant) {
//				skeletonAnimation.state.SetAnimation(trackIndex, name, loop);
//			} else {
//				skeletonAnimation.state.AddAnimation(trackIndex, name, loop, 0);
//			}
		}
	}

	void ResetAnimation () {
		skeletonAnimation.state.AddAnimation(0, "ready", true, 0);
	}

	public void Event (Spine.AnimationState state, int trackIndex, Spine.Event e) {
		// DEBUG event display
		debug_Event.text = e.ToString();
		debug_Event.fontSize = debug_Event.fontSize != 20 ? 20 : 15;
		//Debug.Log(trackIndex + " " + state.GetCurrent(trackIndex) + ": event " + e + ", " + e.Int);

		// EVENT LISTENER
		switch (e.ToString()) {
			case "footstep" :
				fxFootstep.Play();
				break;
			case "charge" :
				if (charging) {
					// charge end
					fxCharge.Stop();
					charging = false;
					cancelable = false;
				} else {
					// start charging
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
		// DEBUG animation name
		debug_AniName.text = state.GetCurrent(0).ToString();

		// get current animation
		currentAnimation = state.GetCurrent(trackIndex).ToString();
		cancelable = false;
		nonCancelableTime = Time.time + state.GetCurrent(trackIndex).time;
	}

	// call when Spine animation END
	void AnimationEndListener (Spine.AnimationState state, int trackIndex) {
		cancelable = true;
	}

	void PlayerLocomotion()
	{
		Vector3 mousePos = (Vector3)touchVecEnd;
		mousePos.z = 10;
		Vector3 moveVector = Camera.main.ScreenToWorldPoint(mousePos) - transform.position;
		
		//Debug.Log(moveVector);
		
		if (moveVector.y > defineJump) {
			// jump
			//Debug.Log("JUMP!!");
			if (grounded) {
				// is first jump
				SetAnimation(0, "jump", false, true);
				skeletonAnimation.state.AddAnimation(0, "fall", false, 0);
				skeletonAnimation.state.AddAnimation(0, "ready", true, .4f);
			} else {
				// is second jump
				if (!forceFall) {
					SetAnimation(0, "jump", false, true);
					skeletonAnimation.state.AddAnimation(0, "fall", false, 0);
					skeletonAnimation.state.AddAnimation(0, "ready", true, .4f);
				}
			}
		} else if (moveVector.x > 0){
			// move forward
			//Debug.Log("FORWARD!!");
			if (grounded) {
				SetAnimation(0, "forward", true);
			}
			//skeletonAnimation.state.AddAnimation(0, "ready", true, 1);
		} else {
			// move backward
			//Debug.Log("BACKWARD!!");
			if (grounded) {
				SetAnimation(0, "dashBackward", false, true);
				ResetAnimation();
			}
			//animator.SetTrigger("dashBackward");
		}
	}
}