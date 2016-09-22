using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}

	public GameObject target;
	public float distance;

	void FixedUpdate() {

		transform.position = new Vector3(target.transform.position.x, target.transform.position.y, target.transform.position.z - distance);

	}

}
