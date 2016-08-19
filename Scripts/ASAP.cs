using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LitJsonUnity;

namespace ASAP {

    public enum MessageType : byte {
        AGENT_SPEC = 0x01,
        AGENT_REQ = 0x02,
        AGENT_STATE = 0x03
        // Messages for controling/customizing uma?
        // save/change recipes or something?
        // physics spec?
        // animation data/clips?
    }
    
    public enum AgentSourceLocation {
        SCENE,
        RECIPE,
        PREFAB
    }

    public class AgentRequest {
        public string id;
        public AgentSource source;

        AgentRequest(string id, AgentSource source) {
            this.id = id;
            this.source = source;
        }

        public static AgentRequest Parse(BinaryReader br) {
            string id = ReadWriteHelper.ReadASCIIString(br);
            AgentSource source = new AgentSource(ReadWriteHelper.ReadASCIIString(br));
            return new AgentRequest(id, source);
        }


        public override bool Equals(System.Object obj) {
            if (obj == null) return false;
            AgentRequest p = obj as AgentRequest;
            if ((System.Object)p == null) return false;
            return (id == p.id);
        }

        public bool Equals(AgentRequest p) {
            if ((object)p == null) return false;
            return (id == p.id);
        }

        public override int GetHashCode() {
            return id.GetHashCode();
        }
    }

    public static class ReadWriteHelper {

        public static void WriteASCIIString(BinaryWriter bw, string s) {
            for (int c = 0; c < s.Length; c++) {
                bw.Write(System.Convert.ToByte(s[c]));
            }
            bw.Write((byte) 0x00);
        }

        public static string ReadASCIIString(BinaryReader br) {
            return Encoding.ASCII.GetString(ReadUntilZero(br));
        }

        public static byte[] ReadUntilZero(BinaryReader br) {
            MemoryStream fs = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(fs);

            byte b;
            while (true) {
                b = br.ReadByte();
                if (b != 0x00) { 
                    bw.Write(b);
                } else {
                    break;
                }
            }
            bw.Flush();
            return fs.ToArray();
        }

        public static Quaternion ReadQuaternion(BinaryReader br) {
            float qw = br.ReadSingle();
            float qx = br.ReadSingle();
            float qy = br.ReadSingle();
            float qz = br.ReadSingle();
            return new Quaternion(-qx, qy, qz, -qw);
        }

        public static Vector3 ReadVector(BinaryReader br) {
            float x = br.ReadSingle();
            float y = br.ReadSingle();
            float z = br.ReadSingle();
            return new Vector3(-x, y, z);
        }
    }

    public class AgentSource {
        public string handle;
        public AgentSourceLocation location;

        public AgentSource(string handle) {
            this.handle = handle;
            if (handle.Equals("/scene")) {
                this.location = AgentSourceLocation.SCENE;
            } else {
                Debug.LogWarning("Couldn't parse AgentSourceLocation handle: "+handle);
            }
            // TODO Implement finding prefabs/recipes/objects in scene
        }
    }

    public class AgentState {
        public string id;
        public Vector3[] positions;
        public Quaternion[] rotations;
        public float[] faceTargetValues;

        AgentState(string id, Vector3[] positions, Quaternion[] rotations, float[] faceTargetValues) {
            this.id = id;
            this.positions = positions;
            this.rotations = rotations;
            this.faceTargetValues = faceTargetValues;
        }

        public static AgentState Parse(string id, BinaryReader br) {
            int numBones = br.ReadInt32();
            Vector3[] positions = new Vector3[numBones];
			Quaternion[] rotations = new Quaternion[numBones];
			Debug.Log ("numBones: " + numBones);
            for (int b = 0; b < numBones; b++) {
                positions[b] = ReadWriteHelper.ReadVector(br);
                rotations[b] = ReadWriteHelper.ReadQuaternion(br);
            }

            int numFaceTargets = br.ReadInt32();
			Debug.Log ("numFaceTargets: " + numFaceTargets);
            float[] faceTargets = new float[numFaceTargets];
            for (int f = 0; f < numFaceTargets; f++) {
                faceTargets[f] = br.ReadSingle();
            }

            return new AgentState(id, positions, rotations, faceTargets);
        }

        public void ParseUpdate(BinaryReader br) {
            int numBones = br.ReadInt32();
            // check that numBones equals this.positions.Length?
            for (int b = 0; b < numBones; b++) {
                positions[b] = ReadWriteHelper.ReadVector(br);
                rotations[b] = ReadWriteHelper.ReadQuaternion(br);
                //Debug.Log("B: "+ReadWriteHelper.ReadASCIIString(br));
            }

            int numFaceTargets = br.ReadInt32();
            for (int f = 0; f < numFaceTargets; f++) {
                faceTargetValues[f] = br.ReadSingle();
            }
        }
    }

	public class WorldUpdate {

		public WorldUpdate() {
		
		}

		public static string WriteUpdate(Dictionary<string,VJoint> worldObjects) {
			StringBuilder sb = new StringBuilder();
			JsonWriter jWriter = new JsonWriter(sb);
			jWriter.WriteObjectStart();
				jWriter.WritePropertyName("worldUpdate");
				jWriter.WriteObjectStart();
					jWriter.WritePropertyName("objects");
					jWriter.WriteObjectStart();
					foreach (KeyValuePair<string,VJoint> kvp in worldObjects) {
						jWriter.WritePropertyName(kvp.Key);
						jWriter.Write(System.Convert.ToBase64String (kvp.Value.GetTransformBytes()));
					}
					jWriter.WriteObjectEnd();
				jWriter.WriteObjectEnd();
			jWriter.WriteObjectEnd();
			return sb.ToString ();
		}
	}

    public class AgentSpec {

        public string id;
        public VJoint[] skeleton;
		public IFaceTarget[] faceTargets;

		public AgentSpec(string id,  VJoint[] skeleton, IFaceTarget[] faceTargets) {
            this.id = id;
            this.skeleton = skeleton;
            this.faceTargets = faceTargets;
        }

        public BinaryWriter WriteBytes(BinaryWriter bw) {
            bw.Write((byte)MessageType.AGENT_SPEC);

            ReadWriteHelper.WriteASCIIString(bw, id);

            bw.Write(skeleton.Length);
            foreach (VJoint bone in skeleton) {
                bone.WriteBytes(bw);
            }

            bw.Write((System.Int32)faceTargets.Length);
            foreach (IFaceTarget faceTarget in faceTargets) {
				ReadWriteHelper.WriteASCIIString(bw, faceTarget.GetName());
            }
            return bw;
        }
    }
		

	public interface IFaceTarget {
		void SetValue(float f);
		float GetValue();
		string GetName();
	}


	public class AnimatoinClipFaceTarget : IFaceTarget {
		AnimationClip clip;

		// Uses single/last/??? frame in animation clip
		// to blend the animated bones to that position

		public void SetValue(float v) {
		}

		public float GetValue() {
			return 0.0f;
		}

		public string GetName() {
			return "";
		}

	}

	public class MorphFaceTarget : IFaceTarget {
		//

		public void SetValue(float v) {
		}

		public float GetValue() {
			return 0.0f;
		}

		public string GetName() {
			return "";
		}

	}

	public class BonePoseFaceTarget : IFaceTarget {
		// Where to get t-pose from?
		// Blend multiple bones to a given target configuration
		// (local rot/pos)
		Quaternion[] targetRotations;
		Vector3[] targetPositions;
		Transform[] bones;

		public void SetValue(float v) {
		}

		public float GetValue() {
			return 0.0f;
		}

		public string GetName() {
			return "";
		}

	}


	/* 
	 * Representing the ASAP VJoint 
	 */
    public class VJoint {
        public string id;
        public VJoint parent;
        public Vector3 position;
        public Quaternion rotation;

        public VJoint(string name) : this(name, null, Vector3.zero, Quaternion.identity) { }

        public VJoint(string name, VJoint parent) : this(name, parent, Vector3.zero, Quaternion.identity) { }

        public VJoint(string name, Vector3 position, Quaternion rotation) : this(name, null, position, rotation) { }

        public VJoint(string name, Vector3 position, Quaternion rotation, VJoint parent) : this(name, parent, position, rotation) { }

        public VJoint(string name, VJoint parent, Vector3 position, Quaternion rotation) {
            this.parent = parent;
            this.id = name;
            this.position = position;
            this.rotation = rotation;
        }

		public byte[] GetTransformBytes() {
			using (MemoryStream stream = new MemoryStream ())
			using (BinaryWriter bw = new BinaryWriter(stream)) {
				bw.Write((float)-position.x);  // FLOAT  x
				bw.Write((float)position.y);  // FLOAT  y
				bw.Write((float)position.z);  // FLOAT  z
				bw.Write((float)-rotation.w);  // FLOAT  qw
				bw.Write((float)-rotation.x);  // FLOAT  qx
				bw.Write((float)rotation.y);  // FLOAT  qy
				bw.Write((float)rotation.z);  // FLOAT  qz
				bw.Flush();
				return stream.ToArray();
			}
		}

        public BinaryWriter WriteBytes(BinaryWriter bw) {
            ReadWriteHelper.WriteASCIIString(bw, id);
            if (parent == null) ReadWriteHelper.WriteASCIIString(bw, "");
            else ReadWriteHelper.WriteASCIIString(bw, parent.id);
            bw.Write((float)-position.x);  // FLOAT  x
            bw.Write((float)position.y);  // FLOAT  y
            bw.Write((float)position.z);  // FLOAT  z
            bw.Write((float)-rotation.w);  // FLOAT  qw
            bw.Write((float)-rotation.x);  // FLOAT  qx
            bw.Write((float)rotation.y);  // FLOAT  qy
            bw.Write((float)rotation.z);  // FLOAT  qz
            return bw;
        }

    }

    public interface IASAPAgent {
        AgentSpec GetAgentSpec();
        void Initialize();
        void SetAgentState(AgentState agentState);
    }

}
