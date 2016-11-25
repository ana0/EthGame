using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
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
	public Contract selectedContract;
	public CallableMethod selectedMethod;
	public List<string> methodArgs;
	public Hashtable watchedContracts = new Hashtable ();
	//UI Components
	public GameObject passwordPrompt;
	//@to-do amount to send prompt is redundant
	public GameObject amountToSendPrompt;
	public GameObject contractPrompt;
	//@to-do change contractDropdown to gameObject for consistency
	public GameObject sendMethodTx;
	public Dropdown contractDropdown;
	public GameObject methodDropdownParent;
	public Dropdown methodDropdown;

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
		//Setup UI components
		contractDropdown.captionText.text = "Contracts";
		methodDropdown = methodDropdownParent.GetComponent<Dropdown> ();
		methodDropdown.captionText.text = "Methods";
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
					prompt ("Enter Amount . . .", amountToSendPrompt, getValue);
				} else {
					prompt ("Enter Password . . .", passwordPrompt, getPasswordThenSend);
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
		
	public void getValue (string value) {
		//Will need error checking here
		int _value = int.Parse (value);
		Debug.Log (_value);
		//Convert number to hex
		amountToSend = "0x" + _value.ToString ("x");
		ethereum.ethSendTransaction (address, addressToReceive, "0x76c0", "0x9184272a000", amountToSend);
		amountToSendPrompt.SetActive (false);
		//clear amount to send field
		clearEntryField(amountToSendPrompt);
	}
		
	public void getPasswordThenSend (string password) {;
		//Will need to parse this response and check for success
		ethereum.personalUnlockAccount (address, password, 10);
		Ethereum.Responded += wasUnlockSuccessful;
		passwordPrompt.SetActive (false);
		//clear password field
		clearEntryField(passwordPrompt);
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
			prompt ("Enter Amount . . .", amountToSendPrompt, getValue);
		}
		Debug.Log(ethereum.parsedJsonResponse ["result"]);
		Ethereum.Responded -= wasUnlockSuccessful;
	}
		
	public void watchContract () {
		prompt ("Enter Contract Name . . .", contractPrompt, beginWatchedContract);
	}

	public void beginWatchedContract (string contractName) {
		contractPrompt.SetActive (false);
		watchedContracts ["temp"] = contractName;
		//clear the entry field in case we want to use it again
		clearEntryField(contractPrompt);
		prompt ("Enter Address . . .", contractPrompt, getContractAddress);
	}
		
	public void getContractAddress (string contractAddress) {
		contractPrompt.SetActive (false);
		//being storing the contract as "watched" - in the future we'll need to be validating this address
		string contractName = (string)watchedContracts["temp"];
		string[] continueContract = new string[] {contractName, contractAddress};
		watchedContracts ["temp"] = continueContract;
		//clear the entry field in case we want to use it again
		clearEntryField(contractPrompt);	
		//open UI element for contract ABI entry
		prompt ("Enter Contract ABI . . .", contractPrompt, finalizeWatchedContract);
	}
		
	public void finalizeWatchedContract (string ABI) {
		contractPrompt.SetActive (false);
		string[] contractInfo = watchedContracts ["temp"] as string[];
		watchedContracts.Remove ("temp");
		Contract contract = new Contract (contractInfo [1], ABI);
		contract.parseContractABI (ABI);
		contract.extractCallableMethods ();
		watchedContracts [contractInfo [0]] = contract;
		//clear the entry field in case we want to use it again
		clearEntryField(contractPrompt);
		populateContractDropdown ();
	}
		
	public void prompt (string placeholderText, GameObject inputPrompt, UnityAction<string> endEditCallback) {
		inputPrompt.SetActive (true);
		var inputLayer = inputPrompt.transform.GetChild(0);
		InputField entryField = inputLayer.GetComponent<InputField> ();
		entryField.placeholder.GetComponent<Text>().text = placeholderText;
		//Add event listener to close window on offclick/enter
		entryField.onEndEdit.RemoveAllListeners();
		entryField.onEndEdit.AddListener (endEditCallback);
	}

	public void clearEntryField(GameObject inputPrompt) {
		var canvasLayer = inputPrompt.transform.GetChild(0);
		InputField entryField = canvasLayer.GetComponent<InputField> ();
		entryField.text = "";
	}

	public void populateContractDropdown() {
	//@to-do make general function and reuse in below function w showavailable
		foreach (DictionaryEntry pair in watchedContracts) {
			contractDropdown.options.Add(new Dropdown.OptionData((string)pair.Key));
			contractDropdown.onValueChanged.AddListener (showAvailableMethods);
			//Debug.Log (contractDropdown.options [1].text);
		}
	}

	//@to-do rename this
	public void showAvailableMethods(int index) {
		string selectedName = contractDropdown.options [index].text;
		selectedContract = watchedContracts [selectedName] as Contract;
		//add methods to dropdown
		methodDropdown.options.Clear();
		foreach (DictionaryEntry pair in selectedContract.callableMethods) {
			methodDropdown.options.Add (new Dropdown.OptionData ((string)pair.Key));
			CallableMethod callable = pair.Value as CallableMethod;
			Debug.Log (callable.sha);
			methodDropdown.onValueChanged.AddListener (setSelectedMethod);
		}
		methodDropdownParent.SetActive (true);
	}

	public void setSelectedMethod(int index) {
		sendMethodTx.SetActive (true);
		string selectedName = methodDropdown.options [index].text;
		selectedMethod = selectedContract.callableMethods [selectedName] as CallableMethod;

	}

	public void callMethod() {
		for (int i = 9; i < selectedMethod.inputs; i++) {
			prompt ("Enter first arg . . .", contractPrompt, setMethodArg);
			clearEntryField (contractPrompt);
		}
	}

	public void setMethodArg(string arg) {
		methodArgs.Add (arg);
	}
} 



