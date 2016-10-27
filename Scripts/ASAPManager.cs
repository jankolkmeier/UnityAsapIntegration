using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ASAP {

    public class ASAPManager : MonoBehaviour {

        public int worldUpdateFrequency = 30;
        float nextWorldUpdate = 0.0f;
        IMiddleware middleware;


        Dictionary<string, ASAPAgent> agents;
        Dictionary<string, AgentSpecRequest> agentRequests;
        Dictionary<string, VJoint> worldObjects;

        void Awake() {
            agents = new Dictionary<string, ASAPAgent>();
            agentRequests = new Dictionary<string, AgentSpecRequest>();
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
            nextWorldUpdate = Time.time + 1.0f / worldUpdateFrequency;

            ObjectUpdate[] objectUpdates = new ObjectUpdate[worldObjects.Count];
            int objectIdx = 0;
            foreach (KeyValuePair<string, VJoint> kvp in worldObjects) {
                objectUpdates[objectIdx] = new ObjectUpdate {
                    objectId = kvp.Key,
                    transform = kvp.Value.GetTransformArray()
                };
                objectIdx++;
            }

            middleware.SendMessage(JsonUtility.ToJson(new WorldObjectUpdate {
                msgType = AUPROT.MSGTYPE_WORLDOBJECTUPDATE,
                nObjects = worldObjects.Count,
                objects = objectUpdates
            }));
        }

        void LateUpdate() {
            foreach (string id in agents.Keys) {
                if (agents[id].agentState != null) {
                    agents[id].ApplyAgentState();
                }
            }
        }

        void ReadMessages() {
            string rawMsg = middleware.ReadMessage(); // Raw JSON string from middleware
            if (rawMsg.Length == 0) return; // will return "" if there is nothing new
            //Debug.Log("Raw: "+rawMsg);

            // Try to parse properties "msgType" & "agentId" if in message
            AsapMessage asapMessage = JsonUtility.FromJson<AsapMessage>(rawMsg);


            switch (asapMessage.msgType) {
                case AUPROT.MSGTYPE_AGENTSPECREQUEST: // AgentSpecRequest type msg comming from ASAP
                    AgentSpecRequest agentSpecRequest = JsonUtility.FromJson<AgentSpecRequest>(rawMsg);


                    if (!agentRequests.ContainsKey(agentSpecRequest.agentId)) {
                        agentRequests.Add(agentSpecRequest.agentId, agentSpecRequest);
                        Debug.Log("Added agent request: " + agentSpecRequest.agentId);
                        nextWorldUpdate = Time.time + 3.0f; // Delay world updates while setting up new agent...
                    } else {
                        Debug.LogWarning("Already preparing agentSpec for ID " + agentSpecRequest.agentId);
                    }
                    break;
                case AUPROT.MSGTYPE_AGENTSTATE:
                    if (agents.ContainsKey(asapMessage.agentId)) {
                        if (agents[asapMessage.agentId].agentState == null)
                            agents[asapMessage.agentId].agentState = new AgentState();
                        JsonUtility.FromJsonOverwrite(rawMsg, agents[asapMessage.agentId].agentState);
                    } else {
                        Debug.LogWarning("Can't update state for unknown agent: " + asapMessage.agentId);
                    }
                    break;
                default:
                    break;
            }
        }

        void HandleRequests() {
            List<string> removals = new List<string>();
            foreach (KeyValuePair<string, AgentSpecRequest> kv in agentRequests) {
                if (kv.Value.source == "/scene" && agents.ContainsKey(kv.Key)) {
                    middleware.SendMessage(JsonUtility.ToJson(agents[kv.Key].agentSpec));
                    Debug.Log("Sent agent spec for id=" + kv.Key);
                    removals.Add(kv.Key);
                } else {
                    Debug.LogWarning("Initializing agents only possible from /scene!");
                }
                // RANDOM / RANDOM SET ?
            }

            foreach (string removeKey in removals) {
                agentRequests.Remove(removeKey);
            }
        }

        public void OnAgentInitialized(ASAPAgent agent) {
            if (agents.ContainsKey(agent.agentSpec.agentId)) {
                Debug.LogWarning("Agent with id " + agent.agentSpec.agentId + " already known. Ignored.");
            } else {
                agents.Add(agent.agentSpec.agentId, agent);
            }
        }

        public void OnWorldObjectInitialized(VJoint worldObject) {
            if (worldObjects.ContainsKey(worldObject.id)) {
                Debug.LogWarning("WorldObject with id " + worldObject.id + " already known. Ignored.");
            } else {
                worldObjects.Add(worldObject.id, worldObject);
            }
        }

        void OnApplicationQuit() {
            middleware.Close();
        }

    }

}