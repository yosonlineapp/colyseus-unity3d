using UnityEngine;
using UnityEngine.UI;

using System.Collections;
using System.Collections.Generic;
using System;
using Colyseus;

using GameDevWare.Serialization;

public class ColyseusClient : MonoBehaviour {

	// UI Buttons are attached through Unity Inspector
	public Button m_ConnectButton, m_JoinButton, m_ReJoinButton, m_SendMessageButton, m_LeaveButton;
	public InputField m_EndpointField;
	public Text m_IdText, m_SessionIdText;

	public string roomName = "demo";

	protected Client client;
	protected Room room;

	// map of players
	protected Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();

	// Use this for initialization
	IEnumerator Start () {
		/* Demo UI */
		m_ConnectButton.onClick.AddListener(ConnectToServer);

		m_JoinButton.onClick.AddListener(JoinRoom);
		m_ReJoinButton.onClick.AddListener(ReJoinRoom);
		m_SendMessageButton.onClick.AddListener(SendMessage);
		m_LeaveButton.onClick.AddListener(LeaveRoom);

		/* Always call Recv if Colyseus connection is open */
		while (true)
		{
			if (client != null)
			{
				client.Recv();
			}
			yield return 0;
		}
	}

	void ConnectToServer ()
	{
		/*
		 * Get Colyseus endpoint from InputField
		 */
		string endpoint = m_EndpointField.text;

		Debug.Log("Connecting to " + endpoint);

		/*
		 * Connect into Colyeus Server
		 */
		client = new Client(endpoint);
		client.OnOpen += (object sender, EventArgs e) => {
			/* Update Demo UI */
			m_IdText.text = "id: " + client.id;
		};
		client.OnError += (sender, e) => Debug.LogError(e.message);
		client.OnClose += (object sender, EventArgs e) => Debug.Log("CONNECTION CLOSED");
		StartCoroutine(client.Connect());
	}

	void JoinRoom ()
	{
		room = client.Join(roomName, new Dictionary<string, object>()
		{
			{ "create", true }
		});

		room.OnReadyToConnect += (sender, e) => {
			Debug.Log("Ready to connect to room!");
			StartCoroutine(room.Connect());
		};
		room.OnError += (sender, e) => Debug.LogError(e.message);
		room.OnJoin += (sender, e) => {
			Debug.Log("Joined room successfully.");
			m_SessionIdText.text = "sessionId: " + room.sessionId;

			PlayerPrefs.SetString("sessionId", room.sessionId);
			PlayerPrefs.Save();
		};
		room.OnStateChange += OnStateChangeHandler;

		room.Listen("players/:id", this.OnPlayerChange);
		room.Listen("players/:id/:axis", this.OnPlayerMove);
		room.Listen("messages/:number", this.OnMessageAdded);
		room.Listen(this.OnChangeFallback);

		room.OnMessage += OnMessage;
	}

	void ReJoinRoom ()
	{
		string sessionId = PlayerPrefs.GetString("sessionId");
		if (string.IsNullOrEmpty(sessionId))
		{
			Debug.Log("Cannot ReJoin without having a sessionId");
			return;
		}

		room = client.ReJoin(roomName, sessionId);

		room.OnReadyToConnect += (sender, e) => {
			Debug.Log("Ready to connect to room!");
			StartCoroutine(room.Connect());
		};
		room.OnError += (sender, e) => Debug.LogError(e.message);
		room.OnJoin += (sender, e) => {
			Debug.Log("Joined room successfully.");
			m_SessionIdText.text = "sessionId: " + room.sessionId;
		};
		room.OnStateChange += OnStateChangeHandler;

		room.Listen("players/:id", this.OnPlayerChange);
		room.Listen("players/:id/:axis", this.OnPlayerMove);
		room.Listen("messages/:number", this.OnMessageAdded);
		room.Listen(this.OnChangeFallback);

		room.OnMessage += OnMessage;
	}

	void LeaveRoom()
	{
		room.Leave(false);

		// Destroy player entities
		foreach (KeyValuePair<string, GameObject> entry in this.players)
		{
			Destroy(entry.Value);
		}

		this.players.Clear();
	}

	void SendMessage()
	{
		if (room != null)
		{
			room.Send("move_right");
		}
		else
		{
			Debug.Log("Room is not connected!");
		}
	}

	void OnMessage (object sender, MessageEventArgs e)
	{
		var message = (IndexedDictionary<string, object>) e.message;
//		Debug.Log(data);
	}

	void OnStateChangeHandler (object sender, RoomUpdateEventArgs e)
	{
		// Setup room first state
		if (e.isFirstState) {
			IndexedDictionary<string, object> players = (IndexedDictionary<string, object>) e.state ["players"];
		}
	}

	void OnPlayerChange (DataChange change)
	{
		Debug.Log ("OnPlayerChange");
		Debug.Log (change.operation);
		Debug.Log (change.path["id"]);
//		Debug.Log (change.value);

		if (change.operation == "add") {
			IndexedDictionary<string, object> value = (IndexedDictionary<string, object>) change.value;

			GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);

			cube.transform.position = new Vector3 (Convert.ToSingle(value ["x"]), Convert.ToSingle(value ["y"]), 0);

			// add "player" to map of players by id.
			players.Add (change.path ["id"], cube);

		} else if (change.operation == "remove") {
			// remove player from scene
			GameObject cube;
			players.TryGetValue (change.path ["id"], out cube);
			Destroy (cube);

			players.Remove (change.path ["id"]);
		}
	}

	void OnPlayerMove (DataChange change)
	{
//		Debug.Log ("OnPlayerMove");
//		Debug.Log ("playerId: " + change.path["id"] + ", Axis: " + change.path["axis"]);
//		Debug.Log (change.value);

		GameObject cube;
		players.TryGetValue (change.path ["id"], out cube);

		cube.transform.Translate (new Vector3 (Convert.ToSingle(change.value), 0, 0));
	}

	void OnPlayerRemoved (DataChange change)
	{
//		Debug.Log ("OnPlayerRemoved");
//		Debug.Log (change.path);
//		Debug.Log (change.value);
	}

	void OnMessageAdded (DataChange change)
	{
//		Debug.Log ("OnMessageAdded");
//		Debug.Log (change.path["number"]);
//		Debug.Log (change.value);
	}

	void OnChangeFallback (PatchObject change)
	{
//		Debug.Log ("OnChangeFallback");
//		Debug.Log (change.operation);
//		Debug.Log (change.path);
//		Debug.Log (change.value);
	}

	void OnApplicationQuit()
	{
		// Make sure client will disconnect from the server
		if (room != null)
		{
			room.Leave();
		}

		if (client != null)
		{
			client.Close();
		}
	}
}
