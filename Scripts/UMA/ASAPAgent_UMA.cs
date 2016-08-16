using UnityEngine;
using System.Collections.Generic;
using UMA.PoseTools;
using UMA;

namespace ASAP { 

    public class ASAPAgent_UMA : ASAPAgent {

        ExpressionPlayer ep;

        float[] expressionControlValues;

        void Awake() {
        }

        void Start () {
	    }
	
	    void Update () {
        }

        public void UMAConfigure(UMAData umaData) {
            animator = umaData.animator;
            ep = umaData.GetComponent<ExpressionPlayer>();
            expressionControlValues = new float[ExpressionPlayer.PoseCount];
            poseHandler = new HumanPoseHandler(animator.avatar, transform);
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
			faceTargets.Add(new ExpressionPlayerFaceTarget("Surprise", new ExpressionControlMapping(new string[] { "midBrowUp_Down", "rightBrowUp_Down", "leftBrowUp_Down", "leftEyeOpen_Close", "rightEyeOpen_Close" }, new float[] { 1.0f, 1.0f, 1.0f, 0.6f, 0.6f })));
			faceTargets.Add(new ExpressionPlayerFaceTarget("Aggressive", new ExpressionControlMapping(new string[] { "midBrowUp_Down", "leftLowerLipUp_Down", "rightLowerLipUp_Down", "leftUpperLipUp_Down", "rightUpperLipUp_Down", "jawOpen_Close" }, new float[] { -1.0f, -0.3f, -0.3f, 0.4f, 0.4f, 0.1f })));

            foreach (string target in ExpressionTargetEditor.ExpressionTargets) {
				faceTargets.Add(new ExpressionPlayerFaceTarget(target, ExpressionTargetEditor.LoadMapping(target)));
            }

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

            float[] zeroes = new float[ExpressionPlayer.PoseCount];
            expressionControlValues = zeroes;
            // keep a "touched" array of faceControls that were touched?

			// only handle the ECM values.

            for (int f = 0; f < agentState.faceTargetValues.Length; f++) {
                if (Mathf.Approximately(agentState.faceTargetValues[f], 0.0f)) continue;

				if (typeof(ExpressionPlayerFaceTarget) == agentSpec.faceTargets[f].GetType()) {
					ExpressionPlayerFaceTarget epft = ((ExpressionPlayerFaceTarget)agentSpec.faceTargets[f]);
					for (int c = 0; c < epft.expressionControlMapping.indexes.Length; c++) {
						int idx = epft.expressionControlMapping.indexes[c];
						expressionControlValues[idx] += epft.expressionControlMapping.values[c] * agentState.faceTargetValues[f];
						// TODO: Average/dampen/....
					}
				}

            }
            ep.Values = expressionControlValues;
        }
	}

	public class ExpressionControlMapping {

		public float[] values;
		public int[] indexes;

		public ExpressionControlMapping(string[] controls, float[] values) {
			this.values = values;
			this.indexes = new int[values.Length];
			for (int i=0; i< values.Length; i++) {
				indexes[i] = System.Array.IndexOf(ExpressionPlayer.PoseNames, controls[i]);
			}
		}
	}

	public class ExpressionPlayerFaceTarget : IFaceTarget {
		public ExpressionControlMapping expressionControlMapping;
		public float value;
		public string name;

		public ExpressionPlayerFaceTarget(string name, ExpressionControlMapping expressionControlMapping) {
			this.value = 0.0f;
			this.name = name;
			this.expressionControlMapping = expressionControlMapping;
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
//faceTargets.Add(new FaceTarget("Surprise", new ExpressionControlMapping(new string[] { "midBrowUp_Down", "rightBrowUp_Down", "leftBrowUp_Down", "leftEyeOpen_Close", "rightEyeOpen_Close" }, new float[] { 1.0f, 1.0f, 1.0f, 0.6f, 0.6f })));
//faceTargets.Add(new FaceTarget("Aggressive", new ExpressionControlMapping(new string[] { "midBrowUp_Down", "leftLowerLipUp_Down", "rightLowerLipUp_Down", "leftUpperLipUp_Down", "rightUpperLipUp_Down", "jawOpen_Close" }, new float[] { -1.0f, -0.3f, -0.3f, 0.4f, 0.4f, 0.1f })));
