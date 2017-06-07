using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ASAP {

    [System.Serializable]
    public class AsapMessage : System.Object {
        public string msgType;
        public string agentId; // not used in all messages, but useful to
        // parse before routing the message further
    }

    [System.Serializable]
    public class AgentSpec : AsapMessage {
        public int nBones;
        public int nFaceTargets;
        public BoneSpec[] bones;
        public string[] faceTargets;

        [System.NonSerialized] public VJoint[] skeleton;
        [System.NonSerialized] public IFaceTarget[] faceTargetsControls;

        public AgentSpec(string id, VJoint[] skeleton, IFaceTarget[] faceTargetControls) {
            this.skeleton = skeleton;
            this.faceTargetsControls = faceTargetControls;

            this.agentId = id;
            this.msgType = AUPROT.MSGTYPE_AGENTSPEC;

            List<string> faceTargetsList = new List<string>();
            for (int f = 0; f < faceTargetControls.Length; f++) {
                faceTargetsList.Add(faceTargetControls[f].GetName());
            }
            faceTargets = faceTargetsList.ToArray();
            nFaceTargets = faceTargets.Length;

            List<BoneSpec> bonesList = new List<BoneSpec>();
            for (int b = 0; b < skeleton.Length; b++) {
                string parentName = "";
                if (skeleton[b].parent != null) {
                    parentName = skeleton[b].parent.id;
                }

                bonesList.Add(new BoneSpec() {
                    boneId = skeleton[b].id,
                    hAnimName = skeleton[b].hAnimName,
                    parentId = parentName,
                    transform = skeleton[b].GetTransformArray()
                });
            }
            bones = bonesList.ToArray();
            nBones = bones.Length;
        }
    }

    [System.Serializable]
    public class AgentSpecRequest : AsapMessage {
        public string source;
    }

    [System.Serializable]
    public class AgentState : AsapMessage {
        public int nBones;
        public int nFaceTargets;
        public BoneTransform[] boneValues;
        public float[] faceTargetValues;

        //
        public string binaryBoneValues;
        public string binaryFaceTargetValues;
    }

    [System.Serializable]
    public class WorldObjectUpdate : AsapMessage {
        public int nObjects;
        public ObjectUpdate[] objects;
    }

    [System.Serializable]
    public class BoneSpec {
        public string boneId;
        public string parentId;
        public string hAnimName;
        public float[] transform;
    }

    [System.Serializable]
    public class ObjectUpdate {
        public string objectId;
        public float[] transform;
    }

    [System.Serializable]
    public class BoneTransform {
        public float[] t;

        public BoneTransform(BinaryReader br) {
            float x = br.ReadSingle();
            float y = br.ReadSingle();
            float z = br.ReadSingle();
            float qx = br.ReadSingle();
            float qy = br.ReadSingle();
            float qz = br.ReadSingle();
            float qw = br.ReadSingle();
            // Just like when parsed in json, the transform parsed from
            // binary does not have the handednes cos converted yet
            t = new float[] { x, y, z, qx, qy, qz, qw };
        }

        // For debugging purpose, to simulate input from ASAP
        // Yes, we inveret the unity cos just so it can get inverted again in the pipeline
        public BoneTransform(Transform transform) {
            float x = -transform.localPosition.x;
            float y = transform.localPosition.y;
            float z = transform.localPosition.z;
            float qx = -transform.localRotation.x;
            float qy = transform.localRotation.y;
            float qz = transform.localRotation.z;
            float qw = -transform.localRotation.w;
            t = new float[] { x, y, z, qx, qy, qz, qw };
        }
    }

    public interface IFaceTarget {
        void SetValue(float f);
        float GetValue();
        string GetName();
    }


    public class AnimationClipFaceTarget : IFaceTarget {
        AnimationClip clip;

        // Uses single/last/??? frame in animation clip
        // to blend the animated bones to that position

        public void SetValue(float v) {}

        public float GetValue() {
            return 0.0f;
        }

        public string GetName() {
            return "";
        }

    }

    public class MorphFaceTarget : IFaceTarget {
        //

        public void SetValue(float v) {}

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

        public void SetValue(float v) {}

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
        public string hAnimName;
        public VJoint parent;
        public Vector3 position;
        public Quaternion rotation;

        public VJoint(string name) : this(name, "", null, Vector3.zero, Quaternion.identity) {}
        public VJoint(string name, Vector3 position, Quaternion rotation) : this(name, "", null, position, rotation) {}
        public VJoint(string name, Vector3 position, Quaternion rotation, VJoint parent) : this(name, "", parent, position, rotation) {}
        public VJoint(string name, string hAnimName) : this(name, hAnimName, null, Vector3.zero, Quaternion.identity) {}
        public VJoint(string name, string hAnimName, Vector3 position, Quaternion rotation) : this(name, hAnimName, null, position, rotation) {}
        public VJoint(string name, string hAnimName, Vector3 position, Quaternion rotation, VJoint parent) : this(name, hAnimName, parent, position, rotation) {}

        public VJoint(string name, string hAnimName, VJoint parent, Vector3 position, Quaternion rotation) {
            this.parent = parent;
            this.id = name;
            if (hAnimName == "") {
                this.hAnimName = "_no_HAnim_" + name;
            } else {
                this.hAnimName = hAnimName;
            }
            this.position = position;
            this.rotation = rotation;
        }

        public float[] GetTransformArray() {
            return new float[] {
                -position.x, position.y, position.z,
                -rotation.x, rotation.y, rotation.z, -rotation.w
            };
        }

    }

    public interface IASAPAgent {
        void Initialize();
        void ApplyAgentState();
    }


    public static class AUPROT {
        public const string PROP_MSGTYPE = "msgType";
        public const string PROP_AGENTID = "agentId";
        public const string PROP_SOURCE = "source";
        public const string PROP_N_BONES = "nBones";
        public const string PROP_N_FACETARGETS = "nFaceTargets";
        public const string PROP_N_OBJECTS = "nObjects";
        public const string PROP_BONES = "bones";
        public const string PROP_BONE_VALUES = "boneValues";
        public const string PROP_BINARY_BONE_VALUES = "binaryBoneValues";
        public const string PROP_FACETARGETS = "faceTargets";
        public const string PROP_FACETARGET_VALUES = "faceTargetValues";
        public const string PROP_BINARY_FACETARGET_VALUES = "binaryFaceTargetsValues";
        public const string PROP_OBJECTS = "objects";
        public const string PROP_OBJECTS_BINARY = "objectsBinary";

        public const string PROP_BONE_ID = "boneId";
        public const string PROP_BONE_PARENTID = "parentId";
        public const string PROP_BONE_HANIMNAME = "hAnimName";
        public const string PROP_TRANSFORM = "transform";
        public const string PROP_OBJECT_ID = "objectId";

        public const string MSGTYPE_AGENTSPECREQUEST = "AgentSpecRequest";
        public const string MSGTYPE_AGENTSPEC = "AgentSpec";
        public const string MSGTYPE_AGENTSTATE = "AgentState";
        public const string MSGTYPE_WORLDOBJECTUPDATE = "WorldObjectUpdate";
    }
}