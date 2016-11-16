using UnityEngine;
using System.Collections;
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

	public delegate void apiResponded ();
	public static event apiResponded Responded;

	public IEnumerator Test() {
		//Not currently called, useful sometimes in debugging to check a completely hardcoded json string
		string jsonstring = "{\"jsonrpc\":\"2.0\",\"method\":\"personal_unlockAccount\",\"params\":[\"0x74e7680630aAa2cBFf07e91069E426C2A46f065b\", \"test\",],\"id\":1}";

		var encoding = new System.Text.UTF8Encoding();

		WWWForm form = new WWWForm();
		var headers = form.headers;
		headers["Content-type"] =  "text/json";
		byte[] data = form.data;
		data = encoding.GetBytes (jsonstring);

		WWW www = new WWW ("http://localhost:8080/rpc", data, headers);

		yield return www;
		Debug.Log(www.text);
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
		
	public void ethSendTransaction(string fromAddress, string toAddress, string gas, string gasPrice, string value) {
		//assembles args into json rpc call, and starts Call Coroutine
		//string fromAddress represents the account sending the transaction, toAddress is the destination address
		//gas is the amount of available gas, gasPrice is the price of gas, and value is the amount to be sent

		Hashtable _parameters = new Hashtable ();
		_parameters ["from"] = fromAddress;
		_parameters ["to"] = toAddress;
		_parameters ["gas"] = gas;
		_parameters ["gasPrice"] = gasPrice;
		_parameters ["value"] = value;
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

	public ArrayList parseContractABI (string ABI) {
		parsedContractABI = JSON.JsonDecode (ABI) as ArrayList;
		return parsedContractABI;
	}

}

