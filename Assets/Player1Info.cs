using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;

public class Player1Info : MonoBehaviour {

	//Ethereum components
	public string address = "0x74e7680630aAa2cBFf07e91069E426C2A46f065b";
	public GameObject eth;
	public EthereumInit ethereumCaller;
	public bool accountIsUnlocked;
	public float accountUnlockTimer;
	public GameObject playerToReceive;
	public string addressToReceive;
	public string amountToSend;
	//UI Components
	public GameObject passwordPrompt;
	public GameObject amountToSendPrompt;
	//movement/physics components
	public float speed;
	Vector3 movement;
	Rigidbody playerRigidbody;

	void Awake () {
		//Set up a rigidbody so the player doesn't fall through the world plane, etc
		playerRigidbody = GetComponent <Rigidbody> ();
		//Find and store a reference to the ethereum GameObject
		eth = GameObject.FindGameObjectWithTag ("Eth");
		ethereumCaller = eth.GetComponent<EthereumInit> ();
		//We're assuming the player hasn't already unlocked their account via Geth or some such
		accountIsUnlocked = false;
	}

	void Update () { 
		if (Input.GetMouseButtonDown (0)) {
			//Raycast fires from the camera to the mouse position, detects whatever is hit
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast (ray, out hit, 100)) {
				//The player object that's hit is a child of the transform returned by the raycast
				playerToReceive = hit.transform.gameObject;
				addressToReceive = playerToReceive.GetComponent<Player2Info> ().address;
				if (accountIsUnlocked) {
					getAmountToSend ();
				} else {
					unlockAccountAndSend ();
				}
			}
		}
		passwordTimerManager ();
	}

	void FixedUpdate () {
		//Get keyboard input and move player
		//We're using FixedUpdate here because this is physics, and therefore needs to be
		//able to rely on a constant framerate
		float h = Input.GetAxisRaw ("Horizontal");
		float v = Input.GetAxisRaw ("Vertical");
		Move (h, v);
	}

	public void Move (float h, float v) {
		//Set movement vector from keyboard input
		movement.Set (h, 0f, v);
		movement = movement * speed * Time.deltaTime;
		//Add movement vector to the player's current transform
		playerRigidbody.MovePosition (transform.position + movement);
	}

	public void getAmountToSend () {
		//Show UI prompt to enter the amount to send
		amountToSendPrompt.SetActive (true);
		var amountEntryLayer = amountToSendPrompt.transform.GetChild(0);
		InputField amountEntryField = amountEntryLayer.GetComponent<InputField> ();
		//Add event listener to close window on offclick/enter
		amountEntryField.onEndEdit.AddListener (getValue);
	}

	public void getValue (string value) {
		//Will need error checking here
		int _value = int.Parse (value);
		Debug.Log (_value);
		//Convert number to hex
		amountToSend = "0x" + _value.ToString ("x");
		ethereumCaller.ethSendTransaction (address, addressToReceive, "0x76c0", "0x9184272a000", amountToSend);
		amountToSendPrompt.SetActive (false);
		//clear amount to send field
		var amountEntryLayer = amountToSendPrompt.transform.GetChild(0);
		InputField amountEntryField = amountEntryLayer.GetComponent<InputField> ();
		amountEntryField.text = "";
	}

	public void unlockAccountAndSend () {
		//Show UI prompt for password
		passwordPrompt.SetActive (true);
		var passwordLayer = passwordPrompt.transform.GetChild(0);
		InputField passwordEntryField = passwordLayer.GetComponent<InputField> ();
		//Add event listener to close window on offclick/enter
		passwordEntryField.onEndEdit.AddListener (getPasswordThenSend);
	}

	public void getPasswordThenSend (string password) {;
		//Will need to parse this response and check for success
		ethereumCaller.personalUnlockAccount (address, password, 10);
		accountIsUnlocked = true;
		passwordPrompt.SetActive (false);
		setPasswordTimer (10.0f);
		//clear password field
		var passwordLayer = passwordPrompt.transform.GetChild(0);
		InputField passwordEntryField = passwordLayer.GetComponent<InputField> ();
		passwordEntryField.text = "";
		//call next step - get the amount to be sent
		getAmountToSend ();
	}

	public void setPasswordTimer (float duration) {
		accountUnlockTimer = duration;
	}

	public void passwordTimerManager () {
		//If unlock account has been called recently, decrement the timer, 
		//Otherwise set accountIsUnlocked flag to false
		if (accountUnlockTimer > 0.0f) {
			accountUnlockTimer -= Time.deltaTime;
		} else {
			accountUnlockTimer = 0.0f;
			accountIsUnlocked = false;
		}
	}
}



