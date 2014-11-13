using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

	int cameraHeight = 2;
	int cameraHorOffset = 1;
	int cameraDepth = -5;

	public GameObject player;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		Camera.main.transform.position = new Vector3(player.transform.position.x + cameraHorOffset, cameraHeight, cameraDepth);
	}
}
