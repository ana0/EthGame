using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Networking;
using System.Globalization;
using System.Text;

public class Ethereum : MonoBehaviour {
	//Main object for making requests to the json rpc api
	//Requests must be first be formatted properly using the jsonAssembler

	public string apiResponse;
	public Hashtable parsedJsonResponse;
	public string contractABI;
	public ArrayList parsedContractABI;

	//event listener to fire when api responds
	public delegate void apiResponded ();
	public static event apiResponded Responded;

	public IEnumerator Test() {
		//Not currently called, useful sometimes in debugging to check a completely hardcoded json string

		byte[] ba = Encoding.Default.GetBytes("baz(uint32,bool)");
		var hexString = BitConverter.ToString(ba);
		hexString = hexString.Replace("-", "");
		string jsonstring = "{\"jsonrpc\":\"2.0\",\"method\":\"web3_sha3\",\"params\":[\"" + hexString + "\"],\"id\":64}";

		var encoding = new System.Text.UTF8Encoding();

		WWWForm form = new WWWForm();
		var headers = form.headers;
		headers["Content-type"] =  "text/json";
		byte[] data = form.data;
		data = encoding.GetBytes (jsonstring);

		WWW www = new WWW ("http://localhost:8080/rpc", data, headers);

		yield return www;
		Debug.Log(www.text);
		yield break;
	}
		
	private IEnumerator Call(string jsonstring, Action<string> resultCallback) {
		//Store the system encoding so we can later turn the json into bytes
		var encoding = new System.Text.UTF8Encoding();

		//Assemble the headers and create a Unity http request object
		WWWForm form = new WWWForm();
		var headers = form.headers;
		headers["Content-type"] =  "text/json";
		//Encode the json as bytes
		byte[] data = form.data;
		data = encoding.GetBytes (jsonstring);

		WWW www = new WWW ("http://localhost:8080/rpc", data, headers);
		yield return www;

		string response = www.text;
		//Return the api response with a callback
		resultCallback(response);
		yield break;
	}

	private void setResponse(string _response){
		//This is used as a callback to return data from the IEnumerator
		//there's only ever one api response string, and it's overwritten by successive api calls
		//all public methods return it
		apiResponse = _response;
		parsedJsonResponse = JSON.JsonDecode (apiResponse) as Hashtable;
		if(Responded != null)
			Responded();
		Debug.Log (apiResponse);
	}

	public void personalUnlockAccount(string address, string password, int duration = 300) {
		//assembles args into json rpc call, and starts Call Coroutine
		//string address represents the account to be unlocked, password is the password of that account
		//duration is optional, and represents the length of time to keep the account unlocked for

		ArrayList parameters = new ArrayList { address, password, duration };
		Hashtable data = new Hashtable ();
		data ["jsonrpc"] = "2.0"; 
		data ["method"] = "personal_unlockAccount";
		data ["params"] = parameters;
		data ["id"] = 1;

		string assembledRequest = JSON.JsonEncode (data);
		Debug.Log (assembledRequest);

		StartCoroutine(Call(assembledRequest, setResponse));
	}
		
	public void ethSendTransaction(string fromAddress, string toAddress, string gas, string gasPrice, string value = "", string contractData = "") {
		//assembles args into json rpc call, and starts Call Coroutine
		//string fromAddress represents the account sending the transaction, toAddress is the destination address
		//gas is the amount of available gas, gasPrice is the price of gas, and value is the amount to be sent

		Hashtable _parameters = new Hashtable ();
		_parameters ["from"] = fromAddress;
		_parameters ["to"] = toAddress;
		_parameters ["gas"] = gas;
		_parameters ["gasPrice"] = gasPrice;
		if (value != "") {
			_parameters ["value"] = value;
		}
		if (contractData != "") {
			_parameters ["data"] = contractData;
		}
		ArrayList parameters = new ArrayList { _parameters };
		Hashtable data = new Hashtable ();
		data ["jsonrpc"] = "2.0"; 
		data ["method"] = "eth_sendTransaction";
		data ["params"] = parameters;
		data ["id"] = 1;

		string assembledRequest = JSON.JsonEncode (data);
		Debug.Log (assembledRequest);

		StartCoroutine(Call(assembledRequest, setResponse));
	}

	public void web3Sha3 (string toHash) {
		//assembles args into json rpc call, and starts Call Coroutine
		//string toHash is the data being hashed 

		//we have to convert to a hex string, because web3 only understands those
		byte[] ba = Encoding.Default.GetBytes(toHash);
		var hexString = BitConverter.ToString(ba);
		hexString = hexString.Replace("-", "");

		ArrayList parameters = new ArrayList { hexString };
		Hashtable data = new Hashtable ();
		data ["jsonrpc"] = "2.0"; 
		data ["method"] = "web3_sha3";
		data ["params"] = parameters;
		data ["id"] = 64;

		string assembledRequest = JSON.JsonEncode (data);
		Debug.Log (assembledRequest);

		StartCoroutine(Call(assembledRequest, setResponse));
	}
		
}

public class Contract {

	public string contractName;		
	public string ABI;
	public ArrayList parsedContractABI;
	public string contractAddress;
	public Hashtable callableMethods;	
	public Contract(string address, string _ABI){		
		contractAddress = address;		
		ABI = _ABI;
	}

	public void parseContractABI (string unparsedABI) {
		//create object from json of contract ABI
		parsedContractABI = JSON.JsonDecode (unparsedABI) as ArrayList;
	}

	public void extractCallableMethods () {
		//extracts the callable methods names, and their inputs from parsed ABI
		//creates method objects for all of them and stores them on a hashtable methods
		//@to-do throws errors if user enters an incorrect ABI
		Hashtable methods = new Hashtable ();
		for (int i = 0; i < parsedContractABI.Count; i++) {
			Hashtable element = parsedContractABI [i] as Hashtable;
			//check if element is a function
			if (!element.ContainsKey ("constant")) {
				continue;
			} 
			//now check what the value of constant
			//may also need to check the length of inputs
			if ((element ["constant"] is Boolean) && ((bool)element ["constant"] == false)) {
				string name = (string)element ["name"];
				ArrayList inputs = element ["inputs"] as ArrayList;
				CallableMethod method = new CallableMethod (name, contractAddress, inputs);
				methods [name] = method;
			} 
		}
		//return a hash table {name: CallableMethod}
		//could probably replace with a generic 
		callableMethods = methods;
	}

}

public class CallableMethod {				
			
	public string methodName;	
	public string address;
	public ArrayList inputs;	
	public GameObject eth;
	public Ethereum ethereum;
	public string signature;
	public string sha;
	public CallableMethod(string name, string _address, ArrayList _inputs){		
		methodName = name;		
		address = _address;
		inputs = _inputs;
		eth = GameObject.FindGameObjectWithTag ("Eth");
		ethereum = eth.GetComponent<Ethereum> ();
		buildSignature();
		sha = "";
	}

	public void buildSignature () {
		//goes through the inputs, an ArrayList, and gets all the input types
		//concatenating the method signature
		//@to-do this could be recursively implemented
		signature = methodName + "(";
		for (int i = 0; i < inputs.Count; i++) {
			Hashtable element = inputs [i] as Hashtable;
			signature = signature + (string)element ["type"] + ",";
		}
		signature = signature.TrimEnd(',') + ")";
		Debug.Log (signature);
	}

	public void getSha () {
		ethereum.web3Sha3 (signature);
		Ethereum.Responded += setSha3;
	}

	public void setSha3 () {
		//event listener for parsing the sha3 hash and extracting the bytes needed for method signature
		//will need error checking
		string result = (string)ethereum.parsedJsonResponse ["result"];
		sha = result.Substring (0, 10);
		Ethereum.Responded -= setSha3;
	}

	public void parseTransactionInput(List<string> enteredArgs, string fromAddress) {
		//as we go through the args, formatting them properly according to their type
		//they get added to this giant list
		List<string> parsedArgs = new List<string> ();
		for (int i = 0; i < inputs.Count; i++) {
			//unpack the needed data type from the input arraylist
			Hashtable arg = inputs [i] as Hashtable;
			string argtype = (string)arg["type"];
			//strip whitespace from what the user entered
			string rawArg = enteredArgs[i].Trim();
			//call the appropriate parsing method, depending on desired datatype
			if (argtype.Substring (argtype.Length - 2) == "[]") {
				//dynamic arg variable length array
				Debug.Log (argtype.Substring (argtype.Length - 2));
			} else if (argtype.Substring (argtype.Length - 1) == "]") {
				//dynamic arg fixed length array
				Debug.Log (argtype.Substring (argtype.Length - 1));
			} else if (argtype == "string") {
				//dynamic arg
				Debug.Log (argtype);
			} else if (argtype == "bytes") {
				//dynamic arg
				Debug.Log (argtype);
			} else if (argtype.Substring (0, 3) == "uin") {
				//max size is currently int32, or else TryParse will fail
				parsedArgs.Add (intParser (rawArg));
			} else if (argtype.Substring (0, 3) == "int") {
				//static length signed ints
				Debug.Log (argtype);
			} else if (argtype == "bool") {
				parsedArgs.Add (boolParser (rawArg));
			} else if (argtype.Substring (0, 3) == "ufi") {
				//unsigned fixed point ints
				Debug.Log (argtype);
			} else if (argtype.Substring (0, 3) == "fix") {
				//signed fixed point ints
				Debug.Log (argtype);
			} else if (argtype == "address") {
				parsedArgs.Add(addressParser(rawArg));
			} else if (argtype == "function") {
				Debug.Log (argtype);
			} else {
				Debug.Log ("Unknown arg type");
				Debug.Log (argtype);
			}
		}
		//build everything we've parsed into one giant string
		string assembledData = listBuilder (parsedArgs);
		//send the transaction
		ethereum.ethSendTransaction (fromAddress, address, "0x76c0", "0x9184272a000", contractData:assembledData); 
	}

	private string addressParser(string rawArg) {
		//checks that address is the right length and adds 0s at the beginning
		string parsedArg = "";
		//strips the preceeding '0x' if it's present
		if (rawArg [1] == 'x') {
			rawArg = rawArg.Substring (2);
		}
		//Should be checking for valid address here
		if (rawArg.Length == 40) {
			parsedArg = "000000000000000000000000" + rawArg;
		} else {
			Debug.Log ("Address was not the right length");
		}
		return parsedArg;
	}

	private string intParser(string rawArg) {
		//checks that the given string is a valid int and formats it correctly if so
		int checkInt;
		string parsedArg = "";
		//tryParse will fail on ints larger than int32
		if (int.TryParse (rawArg, out checkInt)) {
			//adds the right amount of 0s to the beginning
			string hexValue = checkInt.ToString("X");
			for (int i = 0; i < (64 - hexValue.Length); i++) {
				parsedArg = parsedArg + "0";
			}
			parsedArg = parsedArg + hexValue;
		} else {
			Debug.Log ("Couldn't parse int");
		}
		return parsedArg;
	}

	private string boolParser(string rawArg) {
		//Returns encoded string version of the given boolean
		string parsedArg = "";
		if (rawArg == "true") {
			parsedArg = "0000000000000000000000000000000000000000000000000000000000000001";
		} else {
			parsedArg = "0000000000000000000000000000000000000000000000000000000000000000";
		}
		return parsedArg;
	}

	private string listBuilder(List<string> data) {
		string builtData = "";
		for (int i = 0; i < data.Count; i++) {
			builtData = builtData + data [i];
		}
		builtData = sha + builtData;
		return builtData;
	}
}

