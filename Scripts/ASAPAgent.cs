using UnityEngine;
using System.Collections.Generic;
using System;

namespace ASAP { 
    public class ASAPAgent : MonoBehaviour, IASAPAgent {

        public string id;
        public MecanimRetargetingSource retarget;

        [HideInInspector]
        public Transform rootBone;
        [HideInInspector]
        public Animator animator;
        [HideInInspector]
        protected HumanPoseHandler poseHandler;

        protected Transform[] bones;
        protected AgentSpec agentSpec;

        void Start () {
            animator = GetComponent<Animator>();
            poseHandler = new HumanPoseHandler(animator.avatar, transform);
            Initialize();
	    }
	
	    void Update () {
	
	    }

        protected Transform[] GetBoneList(Transform root) {
            List<Transform> transforms = new List<Transform>();
            AppendChildren(root, transforms);
            return transforms.ToArray();
        }

        protected VJoint[] GenerateVJoints() {
            VJoint[] res = new VJoint[bones.Length];
            Dictionary<string, VJoint> lut = new Dictionary<string, VJoint>();

            for (int b = 0; b < bones.Length; b++) {
                VJoint parent = null;
                if (b > 0) parent = lut[bones[b].parent.name];
                res[b] = new VJoint(bones[b].name, bones[b].localPosition, bones[b].localRotation, parent);
                lut.Add(bones[b].name, res[b]);
            }

            return res;
        }

        protected void AppendChildren(Transform root, List<Transform> transforms) {
            transforms.Add(root);
            foreach (Transform child in root) {
                AppendChildren(child, transforms);
            }
        }

        protected int CountChildren(Transform root) {
            int res = 1;
            foreach (Transform child in root) {
                res += CountChildren(child);
            }
            return res;
        }

        public virtual void SetAgentState(AgentState agentState) {
            //Debug.Log("Would Set AGent Sate...");
            for (int b = 0; b < agentState.rotations.Length; b++) {
                //bones[b].localPosition = agentState.positions[b];
                bones[b].localRotation = agentState.rotations[b];
            }

            if (retarget != null) {
                retarget.StorePose();
                HumanPose pose = retarget.GetPose();
                poseHandler.SetHumanPose(ref pose);
            }
        }
        

        public virtual AgentSpec GetAgentSpec() {
            return this.agentSpec;
        }

        public virtual void Initialize() {
            if (retarget != null) {
                bones = GetBoneList(retarget.transform);
            } else if (rootBone != null) {
                bones = GetBoneList(rootBone);
            } else {
                bones = GetBoneList(transform);
            }

            bones = GetBoneList(transform);
            VJoint[] vJoints = GenerateVJoints();
			IFaceTarget[] faceTargets = new IFaceTarget[0] { };
            this.agentSpec = new AgentSpec(id, vJoints, faceTargets);
            Debug.Log("Agent initialized, id=" + this.agentSpec.id + " Bones: " + this.agentSpec.skeleton.Length + " faceControls: " + this.agentSpec.faceTargets.Length);
            FindObjectOfType<ASAPManager>().OnAgentInitialized(this);
        }
    }
}
