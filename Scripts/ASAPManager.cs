using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ASAP {

    public class ASAPManager : MonoBehaviour {

        public string middlewareLocation = "127.0.0.1";
        public int worldUpdateFrequency = 15;
        float nextWorldUpdate = 0.0f;
        IMiddleware middleware;


        Dictionary<string, ASAPAgent> agents;
        Dictionary<string, AgentSpecRequest> agentRequests;
        Dictionary<string, VJoint> worldObjects;

        void Awake() {
            agents = new Dictionary<string, ASAPAgent>();
            agentRequests = new Dictionary<string, AgentSpecRequest>();
            worldObjects = new Dictionary<string, VJoint>();
            middleware = new STOMPMiddleware("stomp:tcp://"+middlewareLocation+":61613", "topic://UnityAgentControl", "topic://UnityAgentFeedback", "admin", "password");
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

        // Maybe parsing/etc. could be done in the communication thread better?
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
                        Debug.Log("Added agent request: " + agentSpecRequest.source + ":"+agentSpecRequest.agentId);
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

                        if (agents[asapMessage.agentId].agentState.binaryBoneValues.Length > 0) {
                            Debug.LogWarning("Parsing binaryBoneValues untested");
                            byte[] binaryMessage = System.Convert.FromBase64String(
                                agents[asapMessage.agentId].agentState.binaryBoneValues);
                            agents[asapMessage.agentId].agentState.boneValues =
                                new BoneTransform[agents[asapMessage.agentId].agentState.nBones];
                            using (BinaryReader br = new BinaryReader(new MemoryStream(binaryMessage))) {
                                for (int b = 0; b < agents[asapMessage.agentId].agentState.nBones; b++) {
                                    agents[asapMessage.agentId].agentState.boneValues[b] = new BoneTransform(br);
                                }
                            }
                        }

                        if (agents[asapMessage.agentId].agentState.binaryFaceTargetValues.Length > 0) {
                            Debug.LogWarning("Parsing binaryFaceTargetValues untested");
                            byte[] binaryMessage = System.Convert.FromBase64String(
                                agents[asapMessage.agentId].agentState.binaryFaceTargetValues);
                            agents[asapMessage.agentId].agentState.faceTargetValues =
                                new float[agents[asapMessage.agentId].agentState.nFaceTargets];
                            using (BinaryReader br = new BinaryReader(new MemoryStream(binaryMessage))) {
                                for (int f = 0; f < agents[asapMessage.agentId].agentState.nFaceTargets; f++) {
                                    agents[asapMessage.agentId].agentState.faceTargetValues[f] =
                                        br.ReadSingle();
                                }
                            }
                        }
                    } else {
                        Debug.LogWarning("Can't update state for unknown agent: " + asapMessage.agentId);
                    }
                    break;
                default:
                    break;
            }
        }

        void HandleRequests() {
            foreach (KeyValuePair<string, AgentSpecRequest> kv in agentRequests) {

                if (!agents.ContainsKey(kv.Key)) {
                    Debug.Log("agentId unknown: " + kv.Key);
                }

                if (kv.Value.source == "/scene") {
                    middleware.SendMessage(JsonUtility.ToJson(agents[kv.Key].agentSpec));
                    Debug.Log("Sent agent spec for id=" + kv.Key);
                } else {
                    Debug.LogWarning("Initializing agents only possible from /scene!");
                }
            }

            agentRequests.Clear();
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