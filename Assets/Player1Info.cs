using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;

public class Player1Info : MonoBehaviour {

	//Ethereum components
	public string address = "0x74e7680630aAa2cBFf07e91069E426C2A46f065b";
	public GameObject eth;
	public Ethereum ethereum;
	public bool accountIsUnlocked;
	public float accountUnlockTimer;
	public GameObject playerToReceive;
	public string addressToReceive;
	public string amountToSend;
	public Hashtable watchedContracts = new Hashtable ();
	//UI Components
	public GameObject passwordPrompt;
	public GameObject amountToSendPrompt;
	public GameObject getContractAddressPrompt;
	public GameObject getContractABIPrompt;
	public GameObject messageDisplay;
	public Text message;
	//movement/physics components
	public float speed;
	Vector3 movement;
	Rigidbody playerRigidbody;

	void Awake () {
		//Set up a rigidbody so the player doesn't fall through the world plane, etc
		playerRigidbody = GetComponent <Rigidbody> ();
		//Find and store a reference to the ethereum GameObject
		eth = GameObject.FindGameObjectWithTag ("Eth");
		ethereum = eth.GetComponent<Ethereum> ();
		//We're assuming the player hasn't already unlocked their account via Geth or some such
		accountIsUnlocked = false;
		//Going to be using this UI component fairly often
		//message = messageDisplay.GetComponent<Text> ();
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
		ethereum.ethSendTransaction (address, addressToReceive, "0x76c0", "0x9184272a000", amountToSend);
		amountToSendPrompt.SetActive (false);
		//clear amount to send field
		var amountEntryLayer = amountToSendPrompt.transform.GetChild(0);
		InputField amountEntryField = amountEntryLayer.GetComponent<InputField> ();
		amountEntryField.text = "";
	}

	public void unlockAccountAndSend () {
		//@todo rename this
		//Show UI prompt for password
		passwordPrompt.SetActive (true);
		var passwordLayer = passwordPrompt.transform.GetChild(0);
		InputField passwordEntryField = passwordLayer.GetComponent<InputField> ();
		//Add event listener to close window on offclick/enter
		passwordEntryField.onEndEdit.AddListener (getPasswordThenSend);
	}

	public void getPasswordThenSend (string password) {;
		//Will need to parse this response and check for success
		ethereum.personalUnlockAccount (address, password, 10);
		Ethereum.Responded += wasUnlockSuccessful;
		passwordPrompt.SetActive (false);
		//clear password field
		var passwordLayer = passwordPrompt.transform.GetChild(0);
		InputField passwordEntryField = passwordLayer.GetComponent<InputField> ();
		passwordEntryField.text = "";
		//display "unlocking" message
		message.text = "Unlocking . . .";
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

	public void wasUnlockSuccessful () {
		//used as delegate by event listener - check that account unlock was successfuk
		//begins account unlock timer, and prompts for amount to send
		bool result = false;
		if (ethereum.parsedJsonResponse ["result"] is bool) {
			result = (bool)ethereum.parsedJsonResponse ["result"];
		} 
		if (result) {
			message.text = "";
			setPasswordTimer (10.0f);
			accountIsUnlocked = true;
			getAmountToSend ();
		}
		Debug.Log(ethereum.parsedJsonResponse ["result"]);
		Ethereum.Responded -= wasUnlockSuccessful;
	}

	public void watchContract () {
		getContractAddressPrompt.SetActive (true);
		var contractAddressLayer = getContractAddressPrompt.transform.GetChild(0);
		InputField addressEntryField = contractAddressLayer.GetComponent<InputField> ();
		//Add event listener to close window on offclick/enter
		addressEntryField.onEndEdit.AddListener (getContractAddress);
	}

	public void getContractAddress (string contractAddress) {
		getContractAddressPrompt.SetActive (false);
		//being storing the contract as "watched" - in the future we'll need to be validating this address
		watchedContracts ["temp"] = contractAddress;
		//clear the entry field in case we want to use it again
		var contractAddressLayer = getContractAddressPrompt.transform.GetChild(0);
		InputField addressEntryField = contractAddressLayer.GetComponent<InputField> ();
		addressEntryField.text = "";	
		//open second UI element for contract ABI entry
		promptForContractABI ();
	}

	public void promptForContractABI () {
		getContractABIPrompt.SetActive (true);
		var contractABILayer = getContractABIPrompt.transform.GetChild(0);
		InputField ABIEntryField = contractABILayer.GetComponent<InputField> ();
		//Add event listener to close window on offclick/enter
		ABIEntryField.onEndEdit.AddListener (getABI);
	}

	public void getABI (string ABI) {
		//Will need error checking here
		string contractAddress = (string) watchedContracts["temp"];
		watchedContracts.Remove ("temp");
		watchedContracts [contractAddress] = ethereum.parseContractABI (ABI);
		Debug.Log (ethereum.parseContractABI (ABI));
		getContractABIPrompt.SetActive (false);
		//clear amount to send field
		var contractABILayer = getContractABIPrompt.transform.GetChild(0);
		InputField ABIEntryField = contractABILayer.GetComponent<InputField> ();
		ABIEntryField.text = "";
	}

}



