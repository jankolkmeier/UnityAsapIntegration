using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LitJsonUnity;
using System.Text;

namespace ASAP {

    public class ASAPManager : MonoBehaviour {
        
		public int worldUpdateFrequency = 30;
		float nextWorldUpdate = 0.0f;
		IMiddleware middleware;


        Dictionary<string, ASAPAgent> agents;
        Dictionary<string, AgentState> agentStates;
		Dictionary<string, AgentRequest> agentRequests;
		Dictionary<string, VJoint> worldObjects;

        void Awake() {
            agents = new Dictionary<string, ASAPAgent>();
            agentStates = new Dictionary<string, AgentState>();
			agentRequests = new Dictionary<string, AgentRequest>();
			worldObjects = new Dictionary<string, VJoint>();
            middleware = new STOMPMiddleware("stomp:tcp://127.0.0.1:61613", "topic://UnityAgentControl", "topic://UnityAgentFeedback", "admin", "password");
        }

        void Update() {
            ReadMessages();
            HandleRequests();

			if (Time.time > nextWorldUpdate) {
				UpdateWorld();
			}
        }

		void UpdateWorld() {
			middleware.SendMessage(WorldUpdate.WriteUpdate(worldObjects));
			nextWorldUpdate = Time.time + 1.0f / worldUpdateFrequency;
		}

        void LateUpdate() {
            foreach (string id in agentStates.Keys) {
                if (agents.ContainsKey(id)) {
                    agents[id].SetAgentState(agentStates[id]);
                }
            }
        }

        void ReadMessages() {
			string rawMsg = middleware.ReadMessage();
			if (rawMsg.Length == 0) return;
			JsonData msg = JsonMapper.ToObject(rawMsg); // TODO: catch error?
			if (msg ["binaryMessage"] != null) {
				byte[] binaryMessage = System.Convert.FromBase64String(msg["binaryMessage"]["content"].ToString());
				using (BinaryReader br = new BinaryReader (new MemoryStream (binaryMessage))) {
					byte msgType = br.ReadByte ();
					switch (msgType) {
					case (byte) MessageType.AGENT_REQ:
						AgentRequest request = AgentRequest.Parse (br);
						if (!agentRequests.ContainsKey (request.id)) {
							agentRequests.Add (request.id, request);
							Debug.Log ("Added agent request: " + request.id);
							nextWorldUpdate = Time.time + 3.0f;
						} else {
							Debug.LogWarning ("Double Request for same ID....");
						}
						break;
					case (byte) MessageType.AGENT_STATE:
						string id = ReadWriteHelper.ReadASCIIString (br);
                            //Debug.Log("Agent state for "+id);
						if (agentStates.ContainsKey (id)) {
							agentStates [id].ParseUpdate (br);
						} else {
							agentStates.Add (id, AgentState.Parse (id, br));
						}
						break;
					default:
						break;
					}
				}
			}
        }

        void HandleRequests() {
            List<string> removals = new List<string>();
            foreach (KeyValuePair<string, AgentRequest> kv in agentRequests) {
                if (kv.Value.source.location == AgentSourceLocation.SCENE && agents.ContainsKey(kv.Key)) {
                    using (var stream = new MemoryStream()) {
                        using (var writer = new BinaryWriter(stream)) {
                            agents[kv.Key].GetAgentSpec().WriteBytes(writer);
                        }
                        Debug.Log("Sent agent spec for id="+kv.Key);

						StringBuilder sb = new StringBuilder();
						JsonWriter jWriter = new JsonWriter(sb);

						jWriter.WriteObjectStart();
						jWriter.WritePropertyName("binaryMessage");
						jWriter.WriteObjectStart();
						jWriter.WritePropertyName("content");
						jWriter.Write(System.Convert.ToBase64String (stream.ToArray ()));
						jWriter.WriteObjectEnd();
						jWriter.WriteObjectEnd();
						string msg = sb.ToString ();
						middleware.SendMessage(msg);
                    }
                    removals.Add(kv.Key);
                } else if (kv.Value.source.location == AgentSourceLocation.PREFAB) {
                    // Init Prefab (expect it to pop up later here)
                    Debug.LogWarning("Loading from PREFAB not supported yet.");
                } else if (kv.Value.source.location == AgentSourceLocation.RECIPE) {
                    // Init using UMA (expect it to pop up later here)
                    // needs everything setup... (slots & stuff)
                    Debug.LogWarning("Loading from RECIPE not supported yet.");
                }
                // RANDOM / RANDOM SET ?
            }

            foreach (string removeKey in removals) {
                agentRequests.Remove(removeKey);
            }
        }

		public void OnAgentInitialized(ASAPAgent agent) {
			if (agents.ContainsKey (agent.GetAgentSpec ().id)) {
				Debug.LogWarning ("Agent with id " + agent.GetAgentSpec ().id + " already known. Ignored.");
			} else {
				agents.Add (agent.GetAgentSpec ().id, agent);
			}
        }

		public void OnWorldObjectInitialized(VJoint worldObject) {
			if (worldObjects.ContainsKey (worldObject.id)) {
				Debug.LogWarning ("WorldObject with id "+worldObject.id+" already known. Ignored.");
			} else {
				worldObjects.Add (worldObject.id, worldObject);
			}
		}

        void OnApplicationQuit() {
            middleware.Close();
        }
    }

}
