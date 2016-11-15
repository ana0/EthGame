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
		
//	public string personalUnlockAccount(string address, string password, int duration = 300) {
//		//assembles args into json rpc call, and starts Call Coroutine
//		//string address represents the account to be unlocked, password is the password of that account
//		//duration is optional, and represents the length of time to keep the account unlocked for
//
//		ArrayList parameters = new ArrayList { address, password, duration };
//		JsonAssembler jsonAssembler = new JsonAssembler("personal_unlockAccount", parameters, false);
//		string assembledRequest = jsonAssembler.buildJson();
//		Debug.Log (assembledRequest);
//
//		StartCoroutine(Call(assembledRequest, setResponse));
//		return apiResponse;
//	}

	public string personalUnlockAccount(string address, string password, int duration = 300) {
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

		return apiResponse;
	}

	public string ethSendTransaction(string fromAddress, string toAddress, string gas, string gasPrice, string value) {
		//assembles args into json rpc call, and starts Call Coroutine
		//string fromAddress represents the account sending the transaction, toAddress is the destination address
		//gas is the amount of available gas, gasPrice is the price of gas, and value is the amount to be sent

		ArrayList parameters = new ArrayList { fromAddress, toAddress, gas, gasPrice, value };
		JsonAssembler jsonAssembler = new JsonAssembler("eth_sendTransaction", parameters, true);
		string assembledRequest = jsonAssembler.buildJson();
		Debug.Log (assembledRequest);

		StartCoroutine(Call(assembledRequest, setResponse));
		return apiResponse;
	}
}

public class JsonAssembler {
	//Object for assembling the json before making requests
	//Should be created with three parameters
	//methodName is the name of the rpc method
	//parameters are the parameters taken by the rpc method
	//isEthMethod should be true if the method is one of the eth methods that uses named parameters
	//otherwise, methodName should be from the Geth management console and parameters are raw strings

	public string methodName;
	public ArrayList parameters;
	public bool isEthMethod;
	public JsonAssembler(string method, ArrayList _params, bool _isethmethod){
		methodName = method;
		parameters = _params;
		isEthMethod = _isethmethod;
	}

	private Jsonifyprefix jsonpre = new Jsonifyprefix ();
	private Jsonifypostfix jsonpost = new Jsonifypostfix ();
	private Params _params = new Params ();

	[System.Serializable]
	public struct Jsonifyprefix {
		public string jsonrpc;
		public string method;
	}

	[System.Serializable]
	public struct Jsonifypostfix {
		public int id;
	}
		
	[System.Serializable]
	public struct Params {
		public string from;
		public string to;
		public string gas;
		public string gasPrice;
		public string value;
		//Not implemented yet, due to Unity's built in json not handling null values
		//public string data;
		//public string nonce;
	}

	public string buildJson() {
		//build the inputs into json, using different strategies 
		//all of the ugliness lives here :scream:
		jsonpre.jsonrpc = "2.0";
		jsonpre.method = methodName;
		jsonpost.id = 1;
		string @params;
		if (isEthMethod) {
			//if one of the eth methods, params args are named and we can use Unity's JsonUtility to assemble them
			//@ symbol here is because params is a keyword
			buildParamsStruct(parameters);
			@params = "\"params\":[" + JsonUtility.ToJson(_params) + "]";
		} else {
			//if not an eth method, params is an ArrayList and we're just concatenating elements
			@params = concatParamaters(parameters);
		}
		string prefix = JsonUtility.ToJson (jsonpre);
		string postfix = JsonUtility.ToJson (jsonpost);

		//JsonUtility can't deal with arrays :(
		string jsonstring = prefix.Substring (0, prefix.Length - 1) + "," + @params + "," + postfix.Substring (1, postfix.Length - 1);
		return jsonstring;
	}

	private string concatParamaters(ArrayList parametersList) {
		//concatenate params for rpc methods without named parameters
		//researching and switching to an external json serializer is a big to-do because Unity's built-ins are pretty limited
		string paramsString = "\"params\":[" ;
		for (int i = 0; i < parametersList.Count; i++) {
			if (parametersList [i] is string) {
				paramsString = paramsString + "\"" + parametersList [i] + "\",";
			} else {
				//only other type we're going to be running into is an int with the currently implemented rpc calls
				paramsString = paramsString + parametersList[i].ToString() + ",";
			}
		} 
		paramsString = paramsString.Substring (0, paramsString.Length - 1) + "]";
		return paramsString;
	}

	private void buildParamsStruct(ArrayList parametersList) {
		//builds the _params struct, at the moment assumes an order of the ArrayList it's receiving
		//a Dictionary might be a better type here than an ArrayList, and/or checking for length/type
		_params.from = (string) parametersList [0];
		_params.to = (string) parametersList [1];
		_params.gas = (string) parametersList [2];
		_params.gasPrice = (string) parametersList [3];
		_params.value = (string) parametersList [4];
	}
}

public class JsonDisassemler {
	//to-do for parsing the responses from Json RPC
	//will be used for return transaction hashs, errors, etc
	public string jsonRpcResponse;
	public JsonDisassemler(string rpcResponse) {
		jsonRpcResponse = rpcResponse;
	}
}


