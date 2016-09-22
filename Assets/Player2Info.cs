using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;

public class Player2Info : MonoBehaviour {

	public static string address = "0xFAcbfE417eFA0d8e5869c34AFbE2AD3b2AdF22C9";
	public GameObject eth;
	public static string v;

	public GameObject prompt;

	void Start () {
		GameObject eth = GameObject.FindGameObjectWithTag ("Eth");
	}

	void Update () {

	}

	public void GetValue(string value) {
		int _value = int.Parse (value);
		var ethCaller = eth.GetComponent<EthereumInit> ();
		ethCaller._params.value = "0x" + _value.ToString ("x");
		print (_value.ToString ("x"));
		prompt.SetActive (false);

		ethCaller._params.to = Player2Info.address;
		ethCaller._params.from = Player1Info.address;
		ethCaller._params.gas = "0x76c0";
		ethCaller._params.gasPrice = "0x9184272a000";
		//ethCaller._params.value = "";

		string @params = "\"params\":[" + JsonUtility.ToJson(ethCaller._params) + "]";

		string a = JsonUtility.ToJson (ethCaller.jsonpre);
		string b = JsonUtility.ToJson (ethCaller.jsonpost);

		string jsonstring = a.Substring (0, a.Length - 1) + "," + @params + "," + b.Substring (1, b.Length - 1);

		print (jsonstring);

		StartCoroutine(ethCaller.Call(jsonstring, ethCaller.setResponse));
	}


	void OnMouseOver() {
		if (Input.GetMouseButtonDown (0)) {
			var ethCaller = eth.GetComponent<EthereumInit> ();

			prompt.SetActive (true);
			var input = prompt.transform.GetChild(0);
			var ok = input.GetComponent<InputField> ();
			ok.onEndEdit.AddListener (GetValue);
		}
	}
}
