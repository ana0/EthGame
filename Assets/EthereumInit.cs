using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Networking;

public class EthereumInit : MonoBehaviour {

	public Jsonifypre jsonpre = new Jsonifypre ();
	public Jsonifypost jsonpost = new Jsonifypost ();

	public Params _params = new Params ();

//	public ParamsContructor[] _paramsconst = new ParamsContructor[] {};

	// Use this for initialization
	void Start () {
//		var _params = new Params[] {};

//		var json = new Jsonify();
		jsonpre.jsonrpc = "2.0";
		jsonpre.method = "eth_sendTransaction";
//		json.@params = "";
		jsonpost.id = 1;

	}

	IEnumerator Test() {
		//string jsonstring = "{\"jsonrpc\":\"2.0\",\"method\":\"eth_coinbase\",\"params\":[],\"id\":64}";
		string jsonstring = "{\"jsonrpc\":\"2.0\",\"method\":\"eth_sendTransaction\",\"params\":[{\"from\":\"0xe2Ed3337810Faa653c0E4441279D3f835817F6fD\",\"to\":\"307863363042656241383934353364303941334634416139314132364263353437343034644238336233\",\"gas\":\"0x76c0\",\"gasPrice\":\"0x9184272a000\",\"value\":\"\"}],\"id\":1}";
		var encoding = new System.Text.UTF8Encoding();

		WWWForm form = new WWWForm();
		var headers = form.headers;
		headers["Content-type"] =  "text/json";
		byte[] data = form.data;
		data = encoding.GetBytes (jsonstring);

		WWW www = new WWW ("http://localhost:8080/rpc", data, headers);

		yield return www;
	}

	[System.Serializable]
	public struct Jsonifypre {
		public string jsonrpc;
		public string method;
//		public string @params;
//		public int id;
	}

	[System.Serializable]
	public struct Jsonifypost {
		public int id;
	}

	[System.Serializable]
	public struct Params {
		public string from;
		public string to;
		public string gas;
		public string gasPrice;
		public string value;
//		public string data;
//		public string nonce;
	}

	[System.Serializable]
	public class deJsonify {
		public string text;
	}

	public IEnumerator Call(string jsonstring, Action<string> resultCallback) {
		var encoding = new System.Text.UTF8Encoding();

		WWWForm form = new WWWForm();
		var headers = form.headers;
		headers["Content-type"] =  "text/json";
		byte[] data = form.data;
		data = encoding.GetBytes (jsonstring);

		WWW www = new WWW ("http://localhost:8080/rpc", data, headers);
		yield return www;

		string newRect = www.text;
		print (www.text);
		resultCallback(newRect);
		yield break;
	}
		
	public string response;

	public void setResponse(string _response){
		response = _response;
	}

	// Update is called once per frame
	void Update () {
		//print (Jsonified);
	}
}




