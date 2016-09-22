using UnityEngine;
using System.Collections;

public class Player1Info : MonoBehaviour {

	public static string address = "0x74e7680630aAa2cBFf07e91069E426C2A46f065b";
	public GameObject eth;

	public float speed;
	Vector3 movement;
	Rigidbody playerRigidbody;

	void Start () {
		playerRigidbody = GetComponent <Rigidbody> ();

	}

	void FixedUpdate () {
		float h = Input.GetAxisRaw ("Horizontal");
		float v = Input.GetAxisRaw ("Vertical");
		Move (h, v);
	}

	public void Move (float h, float v) {

		movement.Set (h, 0f, v);

		movement = movement * speed * Time.deltaTime;
		playerRigidbody.MovePosition (transform.position + movement);
	}
}



