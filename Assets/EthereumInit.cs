using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class EthereumInit : MonoBehaviour {

	// Use this for initialization
	void Start () {
//		NetworkTransport.Init();
//		ConnectionConfig config = new ConnectionConfig();
//		int myReliableChannelId  = config.AddChannel(QosType.Reliable);
//		int myUnreliableChannelId = config.AddChannel(QosType.Unreliable);
//		HostTopology topology = new HostTopology(config, 10);
//		int hostId = NetworkTransport.AddHost(topology, 8888);
//
//		connectionId = NetworkTransport.Connect(hostId, "192.16.7.21", 8888, 0, out error);
		StartCoroutine(Test());

	}
	IEnumerator Test() {
		string jsonstring = "{\"jsonrpc\":\"2.0\",\"method\":\"eth_coinbase\",\"params\":[],\"id\":64}";
		var encoding = new System.Text.UTF8Encoding();

		WWWForm form = new WWWForm();
		var headers = form.headers;
		headers["Content-type"] =  "text/json";
		byte[] data = form.data;
		data = encoding.GetBytes (jsonstring);
		//form.AddField ("{'jsonrpc':\"2.0\",\"method\":\"eth_coinbase\",\"params\":[],\"id\":64}'", "'{\"jsonrpc\":\"2.0\",\"method\":\"eth_coinbase\",\"params\":[],\"id\":64}'");

		WWW www = new WWW ("http://localhost:8080/rpc", data, headers);
		yield return www;
		print (form.headers);
		print (form.data);
		print (www.text);
	}

	// Update is called once per frame
	void Update () {
	
	}
}

