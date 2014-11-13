using UnityEngine;
using System.Collections;
using Spine;
/*
[RequireComponent(typeof(SkeletonAnimation))]
public class SkeletonRootMotion : MonoBehaviour {
	
	
	SkeletonAnimation skeletonAnimation;
	int rootBoneIndex = -1;
	AnimationCurve rootMotionCurve;
	
	void OnEnable(){
		if(skeletonAnimation == null)
			skeletonAnimation  = GetComponent<SkeletonAnimation>();
		
		skeletonAnimation.UpdateState += ApplyRootMotion;
		skeletonAnimation.UpdateBones += UpdateBones;
	}
	
	void OnDisable(){
		skeletonAnimation.UpdateState -= ApplyRootMotion;
		skeletonAnimation.UpdateBones -= UpdateBones;
	}
	
	void Start(){
		rootBoneIndex = skeletonAnimation.skeleton.FindBoneIndex( skeletonAnimation.skeleton.RootBone.Data.Name );
		skeletonAnimation.state.Start += HandleStart;
	}
	
	void HandleStart (Spine.AnimationState state, int trackIndex)
	{
		//must use first track for now
		if(trackIndex != 0)
			return;
		
		rootMotionCurve = null;
		
		Spine.Animation anim = state.GetCurrent(trackIndex).Animation;
		
		//find the root bone's translate curve
		foreach(Timeline t in anim.Timelines){
			if(t.GetType() != typeof(TranslateTimeline))
				continue;
			
			TranslateTimeline tt = (TranslateTimeline)t;
			if(tt.boneIndex == rootBoneIndex){
				
				//sample the root curve's X value
				//TODO:  cache this data?  Maybe implement RootMotionTimeline instead and keep it in SkeletonData
				rootMotionCurve = new AnimationCurve();
				
				float time = 0;
				float increment = 1f/30f;
				int frameCount = Mathf.FloorToInt(anim.Duration / increment);
				
				for(int i = 0; i <= frameCount; i++){
					float x = GetXAtTime(tt, time);
					rootMotionCurve.AddKey(time, x);
					time += increment;
				}
				
				break;
			}
		}
	}
	
	//borrowed from TranslateTimeline.Apply method
	float GetXAtTime(TranslateTimeline timeline, float time){	
		
		float[] frames = timeline.frames;
		if (time < frames[0]) return frames[1]; // Time is before first frame.
		
		Bone bone = skeletonAnimation.skeleton.RootBone;
		//foreach(float f in frames) Debug.Log(f);
		//Debug.Log(frames[frames.Length - 2]);
//		Debug.Log(bone.data.x);
//		Debug.Log(bone.x);
//		Debug.Log("------");
		if (time >= frames[frames.Length - 3]) { // Time is after last frame.
			return (bone.data.x + frames[frames.Length - 2] - bone.x);
		}
		
		// Interpolate between the last frame and the current frame.
		int frameIndex = Spine.Animation.binarySearch(frames, time, 3);
		float lastFrameX = frames[frameIndex - 2];
		float frameTime = frames[frameIndex];
		float percent = 1 - (time - frameTime) / (frames[frameIndex + -3] - frameTime);
		percent = timeline.GetCurvePercent(frameIndex / 3 - 1, percent < 0 ? 0 : (percent > 1 ? 1 : percent));
		
		return (bone.data.x + lastFrameX + (frames[frameIndex + 1] - lastFrameX) * percent - bone.x);
	}
	
	
	
	void ApplyRootMotion(SkeletonAnimation skelAnim){
		if(rootMotionCurve == null)
			return;
		
		TrackEntry t = skelAnim.state.GetCurrent(0);
		
		if(t == null)
			return;
		
		int loopCount = (int)(t.Time / t.EndTime);
		int lastLoopCount = (int)(t.LastTime / t.EndTime);
		//disregard the unwanted
		if(lastLoopCount < 0) lastLoopCount = 0;
		
		float currentTime = t.Time - (t.EndTime * loopCount);
		float lastTime = t.LastTime - (t.EndTime * lastLoopCount);
		
		float delta = 0;
		
		float a = rootMotionCurve.Evaluate(lastTime);
		float b = rootMotionCurve.Evaluate(currentTime);
		
		//detect if loop occurred and offset
		if(loopCount > lastLoopCount){
			float e = rootMotionCurve.Evaluate(t.EndTime);
			float s = rootMotionCurve.Evaluate(0);
			
			delta = (e-a) + (b-s);
		}
		else{
			delta = b - a;
		}
		
		if(skelAnim.skeleton.FlipX)
			delta *= -1;
		
		
		//TODO:  implement Rigidbody2D and Rigidbody hooks here
		transform.Translate(delta,0,0);
	}
	
	void UpdateBones(SkeletonAnimation skelAnim){
		//reset the root bone's x component to stick to the origin
		skelAnim.skeleton.RootBone.X = 0;
	}
}
//*/
///*
public class SkeletonRootMotion : MonoBehaviour {
	
	
	SkeletonAnimation skeletonAnimation;
	int rootBoneIndex = -1;
	// animation curves for copy position
	AnimationCurve rootMotionCurve;
	AnimationCurve rootMotionCurveY;
	
	void OnEnable(){
		if(skeletonAnimation == null)
			skeletonAnimation  = GetComponent<SkeletonAnimation>();

		// add events
		skeletonAnimation.UpdateState += ApplyRootMotion;
		skeletonAnimation.UpdateBones += UpdateBones;
	}
	
	void OnDisable(){
		// remove events
		skeletonAnimation.UpdateState -= ApplyRootMotion;
		skeletonAnimation.UpdateBones -= UpdateBones;
	}
	
	void Start(){
		rootBoneIndex = skeletonAnimation.skeleton.FindBoneIndex( skeletonAnimation.skeleton.RootBone.Data.Name );
		skeletonAnimation.state.Start += HandleStart;
	}
	
	void HandleStart (Spine.AnimationState state, int trackIndex)
	{
		//must use first track for now
		if(trackIndex != 0)
			return;
		
		rootMotionCurve = null;
		rootMotionCurveY = null;

		// get current animation
		Spine.Animation anim = state.GetCurrent(trackIndex).Animation;
		
		//find the root bone's translate curve
		foreach(Timeline t in anim.Timelines){
			if(t.GetType() != typeof(TranslateTimeline))
				continue;
			
			TranslateTimeline tt = (TranslateTimeline)t;
			if(tt.boneIndex == rootBoneIndex){
				
				//sample the root curve's X value
				//TODO:  cache this data?  Maybe implement RootMotionTimeline instead and keep it in SkeletonData
				rootMotionCurve = new AnimationCurve();
				rootMotionCurveY = new AnimationCurve();

				float time = 0;
				float increment = 1f/30f;
				int frameCount = Mathf.FloorToInt(anim.Duration / increment);
				
				for(int i = 0; i <= frameCount; i++){
					Vector2 v = GetXYAtTime(tt, time);
					rootMotionCurve.AddKey(time, v.x);
					rootMotionCurveY.AddKey(time, v.y);
					time += increment;
				}
				
				break;
			}
		}
	}
	
	//borrowed from TranslateTimeline.Apply method
	Vector2 GetXYAtTime(TranslateTimeline timeline, float time){
		float[] frames = timeline.frames;
		if (time < frames[0]) return (new Vector2(frames[1], frames[2])); // Time is before first frame.
		
		Bone bone = skeletonAnimation.skeleton.RootBone;
		if (time >= frames[frames.Length - 3]) { // Time is after last frame.
			return (new Vector2(bone.data.x + frames[frames.Length - 2] - bone.x, bone.data.y + frames[frames.Length - 1] - bone.y));
		}
		
		// Interpolate between the last frame and the current frame.
		int frameIndex = Spine.Animation.binarySearch(frames, time, 3);
		float lastFrameX = frames[frameIndex - 2];
		float lastFrameY = frames[frameIndex - 1];
		float frameTime = frames[frameIndex];
		float percent = 1 - (time - frameTime) / (frames[frameIndex + -3] - frameTime);
		percent = timeline.GetCurvePercent(frameIndex / 3 - 1, percent < 0 ? 0 : (percent > 1 ? 1 : percent));
		
		return (new Vector2(bone.data.x + lastFrameX + (frames[frameIndex + 1] - lastFrameX) * percent - bone.x, bone.data.y + lastFrameY + (frames[frameIndex + 2] - lastFrameY) * percent - bone.y));
	}
	
	void ApplyRootMotion(SkeletonAnimation skelAnim){
		if(rootMotionCurve == null || rootMotionCurveY == null)
			return;
		
		TrackEntry t = skelAnim.state.GetCurrent(0);
		
		if(t == null)
			return;
		
		int loopCount = (int)(t.Time / t.EndTime);
		int lastLoopCount = (int)(t.LastTime / t.EndTime);
		//disregard the unwanted
		if(lastLoopCount < 0) lastLoopCount = 0;
		
		float currentTime = t.Time - (t.EndTime * loopCount);
		float lastTime = t.LastTime - (t.EndTime * lastLoopCount);
		
		float delta = 0;
		float deltaY = 0;
		
		float a = rootMotionCurve.Evaluate(lastTime);
		float aY = rootMotionCurveY.Evaluate(lastTime);
		float b = rootMotionCurve.Evaluate(currentTime);
		float bY = rootMotionCurveY.Evaluate(currentTime);

		//detect if loop occurred and offset
		if(loopCount > lastLoopCount){
			float e = rootMotionCurve.Evaluate(t.EndTime);
			float eY = rootMotionCurveY.Evaluate(t.EndTime);
			float s = rootMotionCurve.Evaluate(0);
			float sY = rootMotionCurveY.Evaluate(0);
			
			delta = (e-a) + (b-s);
			deltaY = (eY-aY) + (bY-sY);
		}
		else{
			delta = b - a;
			deltaY = bY - aY;
		}
		
		if(skelAnim.skeleton.FlipX)
		{
			delta *= -1;
			deltaY *= -1;
		}
		

		//TODO:  implement Rigidbody2D and Rigidbody hooks here
		transform.Translate(delta,deltaY,0);
		Debug.DrawLine(new Vector3(transform.position.x+2, 2, 0), new Vector3(transform.position.x+2, 2+deltaY, 0), Color.red, .2f);
	}
	
	void UpdateBones(SkeletonAnimation skelAnim){
		//reset the root bone's x component to stick to the origin
		skelAnim.skeleton.RootBone.X = 0;
		skelAnim.skeleton.RootBone.Y = 0;
	}
}
//*/