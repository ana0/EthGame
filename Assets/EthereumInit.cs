using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Networking;

public class EthereumInit : MonoBehaviour {

	// Use this for initialization
	void Start () {
		var _params = new Params[] {};

		var json = new Jsonify();
		json.jsonrpc = "2.0";
		json.method = "eth_coinbase";
		json.@params = _params;
		json.id = 64;

		string Jsonified = JsonUtility.ToJson(json);

		var _dejson = new DeJson();

		StartCoroutine(Call(Jsonified, setRect));

//		print (_dejson.text);
	}

	IEnumerator Test() {
		string jsonstring = "{\"jsonrpc\":\"2.0\",\"method\":\"eth_coinbase\",\"params\":[],\"id\":64}";
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
	public struct Jsonify {
		public string jsonrpc;
		public string method;
		public Params[] @params;
		public int id;
	}

	[System.Serializable]
	public struct Params {
		public string from;
		public string to;
		public string gas;
		public string gasPrice;
		public string value;
		public string data;
		public string nonce;
	}

	[System.Serializable]
	public class DeJson {
		public string text;
	}

//	public class CoroutineWithData {
//		public Coroutine coroutine { get; private set; }
//		public object result;
//		private IEnumerator target;
//		public CoroutineWithData(MonoBehaviour owner, IEnumerator target) {
//			this.target = target;
//			this.coroutine = owner.StartCoroutine(Run());
//		}
//
//		private IEnumerator Run() {
//			while(target.MoveNext()) {
//				result = target.Current;
//				yield return result;
//			}
//		}
//	}

	IEnumerator Call(string jsonstring, Action<string> resultCallback) {
		var encoding = new System.Text.UTF8Encoding();

		WWWForm form = new WWWForm();
		var headers = form.headers;
		headers["Content-type"] =  "text/json";
		byte[] data = form.data;
		data = encoding.GetBytes (jsonstring);

		WWW www = new WWW ("http://localhost:8080/rpc", data, headers);
		yield return www;
		string newRect = www.text;
		resultCallback(newRect);

		yield break;
		//		resultCallback = www.text;
		//		yield return result.text;

	}
		

	public string response;

	public void setRect(string _response){
		response = _response;
	}


	// Update is called once per frame
	void Update () {
//		print (response);
	}
}




