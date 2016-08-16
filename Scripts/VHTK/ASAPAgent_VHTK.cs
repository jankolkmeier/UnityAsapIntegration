using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ASAP {
	
	public class ASAPAgent_VHTK : ASAPAgent {

		public AnimationClip neutralFacePose;

		// Use this for initialization
		void Start () {
			animator = GetComponent<Animator> ();
			poseHandler = new HumanPoseHandler(animator.avatar, transform);
			Initialize ();
		}
		
		// Update is called once per frame
		void Update () {
		
		}

		public override void Initialize() {
			if (retarget != null) {
				bones = GetBoneList(retarget.transform);
			} else if (rootBone != null) {
				bones = GetBoneList(rootBone);
			} else {
				bones = GetBoneList(transform);
			}
			VJoint[] vJoints = GenerateVJoints();

			List<IFaceTarget> faceTargets = new List<IFaceTarget>();

			faceTargets.Add(new VHTKFaceTarget("test"));
			//...

			agentSpec = new AgentSpec(id, vJoints, faceTargets.ToArray());
			FindObjectOfType<ASAPManager>().OnAgentInitialized(this);
		}


		public override void SetAgentState(AgentState agentState) {
			for (int b = 0; b < agentState.rotations.Length; b++) {
				//bones[b].localPosition = agentState.positions[b];
				bones[b].localRotation = agentState.rotations[b];
			}

			if (retarget != null) { 
				retarget.StorePose();
				HumanPose pose = retarget.GetPose();
				poseHandler.SetHumanPose(ref pose);
			}

			/*
			float[] zeroes = new float[];
			*/

			for (int f = 0; f < agentState.faceTargetValues.Length; f++) {
				if (Mathf.Approximately(agentState.faceTargetValues[f], 0.0f)) continue;

				if (typeof(VHTKFaceTarget) == agentSpec.faceTargets[f].GetType()) {
//					VHTKFaceTarget vhtft = ((VHTKFaceTarget)agentSpec.faceTargets[f]);

				}

			}
		}

	}


	public class VHTKFaceTarget : IFaceTarget {
		public float value;
		public string name;

		public VHTKFaceTarget(string name) {
			this.value = 0.0f;
			this.name = name;
		}

		public void SetValue(float v) {
			value = v;
		}

		public float GetValue() {
			return value;
		}

		public string GetName() {
			return name;
		}
	}

}
