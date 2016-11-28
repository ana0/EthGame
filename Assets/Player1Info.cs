using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;

public class Player1Info : MonoBehaviour {

	//Ethereum components
	public string address;
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
	public bool isEnteringInput;
	public Text message;
	//movement/physics components
	public float speed;
	Vector3 movement;
	Rigidbody playerRigidbody;

	/*
	 * 
	 * Game Loop
	 * 
 	 */

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
		//No way to begin the game with an input prompt active
		isEnteringInput = false;

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
					prompt ("Enter Amount . . .", amountToSendPrompt, getValueandSendTx);
				} else {
					StartCoroutine (unlockAndSend ());
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

	/*
	 * 
	 * Physics
	 * 
 	 */

	public void Move (float h, float v) {
		//Set movement vector from keyboard input
		movement.Set (h, 0f, v);
		movement = movement * speed * Time.deltaTime;
		//Add movement vector to the player's current transform
		playerRigidbody.MovePosition (transform.position + movement);
	}

	/*
	 * 
	 * Ui helpers
	 * 
 	 */

	public void prompt (string placeholderText, GameObject inputPrompt, UnityAction<string> endEditCallback) {
		isEnteringInput = true;
		inputPrompt.SetActive (true);
		var inputLayer = inputPrompt.transform.GetChild(0);
		InputField entryField = inputLayer.GetComponent<InputField> ();
		entryField.placeholder.GetComponent<Text>().text = placeholderText;
		//Add event listener to close window on offclick/enter
		entryField.onEndEdit.RemoveAllListeners();
		entryField.onEndEdit.AddListener (endEditCallback);
	}

	public void clearEntryField(GameObject inputPrompt) {
		//clears the input prompt for reuse
		isEnteringInput = false;
		var canvasLayer = inputPrompt.transform.GetChild(0);
		InputField entryField = canvasLayer.GetComponent<InputField> ();
		entryField.text = "";
	}

	public void populateDropdown(Dropdown dropdown, Hashtable options, UnityAction<int> valueChangedCallback) {
		dropdown.onValueChanged.RemoveAllListeners ();
		dropdown.options.Clear ();
		foreach (DictionaryEntry pair in options) {
			dropdown.options.Add(new Dropdown.OptionData((string)pair.Key));
		}
		dropdown.onValueChanged.AddListener (valueChangedCallback);
	}

	/*
	 * 
	 * Password management and event listeners for standard transaction
	 * 
 	 */
		
	public void getValueandSendTx (string value) {
		//Event listener, gets value when sending a standard tx
		//@to-do will throw an error when passed a non-parseable string or a long
		int _value = int.Parse (value);
		Debug.Log (_value);
		//Convert number to hex
		amountToSend = "0x" + _value.ToString ("x");
		ethereum.ethSendTransaction (address, addressToReceive, "0x76c0", "0x9184272a000", value:amountToSend);
		amountToSendPrompt.SetActive (false);
		//clear amount to send field
		clearEntryField(amountToSendPrompt);
	}

	public bool doneUnlocking() {
		//Wrapper for the isAccountUnlocked switch 
		//So that the boolean can be checked as a function by yield waitUntil
		if (accountIsUnlocked) {
			return true;
		}
		return false;
	}

	private IEnumerator unlockAndSend() {
		//prompts for password, and then waits until account is unlocked
		prompt ("Enter Password . . .", passwordPrompt, getPasswordandUnlock);
		yield return new WaitUntil(doneUnlocking);

		prompt ("Enter Amount . . .", amountToSendPrompt, getValueandSendTx);
		yield break;
	}
		
	public void getPasswordandUnlock (string password) {;
		//Event listener attempts to unlock the account using the entered password
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
		//begins account unlock timer
		bool result = false;
		if (ethereum.parsedJsonResponse ["result"] is bool) {
			result = (bool)ethereum.parsedJsonResponse ["result"];
		} 
		if (result) {
			message.text = "";
			setPasswordTimer (10.0f);
			accountIsUnlocked = true;
		} else {
			message.text = "";
		}
		Debug.Log(ethereum.parsedJsonResponse ["result"]);
		Ethereum.Responded -= wasUnlockSuccessful;
	}

	/*
	 * 
	 * Watch-contract flow and contract interaction 
	 * 
 	 */
		
	public void watchContract () {
		prompt ("Enter Contract Name . . .", contractPrompt, beginWatchedContract);
	}

	public void beginWatchedContract (string contractName) {
		//first stage of watching a contract, collects the contract name
		contractPrompt.SetActive (false);
		//begin a temporary entry in watched contracts for storing contract info as we collect it
		//to be cleared in final stage of watching contract
		watchedContracts ["temp"] = contractName;
		clearEntryField(contractPrompt);
		prompt ("Enter Address . . .", contractPrompt, getContractAddress);
	}
		
	public void getContractAddress (string contractAddress) {
		//second stage of watching a contract, collects contract address
		contractPrompt.SetActive (false);
		//unpack the string stored in the first stage of watching the contract
		string contractName = (string)watchedContracts["temp"];
		//construct a string array of contract name and contract address
		//and re-store it at the temp spot
		string[] continueContract = new string[] {contractName, contractAddress};
		watchedContracts ["temp"] = continueContract;
		clearEntryField(contractPrompt);	
		//open UI element for contract ABI entry
		prompt ("Enter Contract ABI . . .", contractPrompt, finalizeWatchedContract);
	}
		
	public void finalizeWatchedContract (string ABI) {
		//final stage of watching a contract, collects ABI and constructs contract object
		contractPrompt.SetActive (false);
		//unpack the temp string array held in watchedContracts and delete it
		string[] contractInfo = watchedContracts ["temp"] as string[];
		watchedContracts.Remove ("temp");
		//create a new contract object with that info
		Contract contract = new Contract (contractInfo [1], ABI);
		contract.parseContractABI (ABI);
		contract.extractCallableMethods ();
		//"finalized" items in watched Contracts have the format {contractName: contractObject}
		watchedContracts [contractInfo [0]] = contract;
		clearEntryField(contractPrompt);
		//show and populate contract interaction dropdowns
		populateDropdown(contractDropdown, watchedContracts, setSelectedContract);
		setSelectedContract (0);
		populateDropdown (methodDropdown, selectedContract.callableMethods, setSelectedMethod);
		setSelectedMethod (0);
		methodDropdownParent.SetActive (true);
		sendMethodTx.SetActive (true);
	}

	public void setSelectedContract(int index) {
		//Sets the selectedContract variable, and gathers the methods associated with it
		string selectedName = contractDropdown.options [index].text;
		selectedContract = watchedContracts [selectedName] as Contract;
		//must also refresh selected methods
		populateDropdown (methodDropdown, selectedContract.callableMethods, setSelectedMethod);
		setSelectedMethod (0);
		//Refresh text
		contractDropdown.captionText.text = "Contracts";
	}

	public void setSelectedMethod(int index) {
		//Sets the selectedMethod variable 
		string selectedName = methodDropdown.options [index].text;
		selectedMethod = selectedContract.callableMethods [selectedName] as CallableMethod;
		//Refresh Text
		methodDropdown.captionText.text = "Methods";
	}

	public void callMethod() {
		//attached to call Method button, triggers input collection coroutine
		StartCoroutine(collectArgs());
	}

	public bool doneEntering() {
		//wrapper for the isEnteringInput switch 
		//So that the boolean can be checked as a function by yield waitUntil
		if (isEnteringInput) {
			return false;
		}
		return true;
	}

	private IEnumerator collectArgs() {
		//loops and collect input, prompting for the type and appending them to a list
		if (selectedMethod.sha == "") {
			selectedMethod.getSha ();
		}
		for (int i = 0; i < selectedMethod.inputs.Count; i++) {
			Hashtable arg = selectedMethod.inputs [i] as Hashtable;
			string argtype = (string)arg ["type"];
			prompt ("Enter " + argtype, contractPrompt, setMethodArg);
			yield return new WaitUntil(doneEntering);
		}
		//Pass gathered args over to the method object for encoding
		if (!accountIsUnlocked) {
			prompt ("Enter Password . . .", passwordPrompt, getPasswordandUnlock);
			yield return new WaitUntil (doneUnlocking);
		}
		selectedMethod.parseTransactionInput (methodArgs, address);
		//Clear list in case we call another method later
		methodArgs.Clear ();
		yield break;
	}

	public void setMethodArg(string arg) {
		//event listener for collectArgs Coroutine, appends to arg list and closes prompt
		clearEntryField (contractPrompt);
		contractPrompt.SetActive (false);
		methodArgs.Add (arg);
	}
} 



