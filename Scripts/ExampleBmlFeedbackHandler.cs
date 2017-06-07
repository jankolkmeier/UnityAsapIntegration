using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASAP {
	// This is a quite useless example. We use BML Feedback messages to determine when 
	// BML blocks with the id="speechbml***" start and stop.
	// Something like this could be used as input for a statemachine that controls listener/speaker behavior.
	/*                                                                  vvvvvvv
			<bml xmlns="http://www.bml-initiative.org/bml/bml-1.0" id="speechbml1">
				<speech id="speech1" start="2">
					<text>Hello! This is a basic BML test for the realizer bridge!</text>
				</speech>
			</bml>
	*/
	public class ExampleBmlFeedbackHandler : MonoBehaviour {

		BMLFeedback bmlFeedback;

		Dictionary<string, float> activeBehaviorStack;

		void Start() {
			bmlFeedback = FindObjectOfType<BMLFeedback>();
			if (bmlFeedback == null) {
				Debug.LogError("Could not find BMLFeedback component in scene!");
			} else {
				bmlFeedback.BlockProgressEventHandler += new BlockProgressCallback(OnBlockProgress);
				bmlFeedback.PredictionFeedbackEventHandler += new PredictionFeedbackCallback(OnPredictionFeedback);
				bmlFeedback.SyncPointProgressEventHandler += new SyncPointProgressCallback(OnSyncPointProgress);
				bmlFeedback.WarningFeedbackEventHandler += new WarningFeedbackCallback(OnWarningFeedback);
			}

			activeBehaviorStack = new Dictionary<string, float>();
		}

		void OnBlockProgress(BlockProgress blockProgress) {
			if (blockProgress.status == "DONE" && blockProgress.id.EndsWith(":end")) {
				RemoveBehavior(blockProgress.id.Substring(0, blockProgress.id.Length-4));
			}
		}

		void OnPredictionFeedback(PredictionFeedback predictionFeedback) {
			if (predictionFeedback.bml.status == "PENDING") {
				AddBehavior(predictionFeedback.bml.id);
			} else if (predictionFeedback.bml.status == "LURKING") {
				//Debug.LogWarning("Have LURKING behavour. Removing from stack as LURKING often means it fails.");
				RemoveBehavior(predictionFeedback.bml.id);
			}
		}

		void OnSyncPointProgress(SyncPointProgress syncPointProgress) {
			// ...
		}

		void OnWarningFeedback(WarningFeedback warningFeedback) {
			Debug.LogWarning("Got warning. Trying removing associated behavior from stack:\n"+ warningFeedback.Value);
			RemoveBehavior(warningFeedback.id);
		}


		void AddBehavior(string id) {
			if (!id.StartsWith("speechbml") || activeBehaviorStack.ContainsKey(id)) return;
			if (activeBehaviorStack.Count == 0) {
				// Send event that we're starting talking.
				Debug.Log("speechbml block starts");
			}

			activeBehaviorStack.Add(id, Time.time);
		}

		void RemoveBehavior(string id) {
			if (!id.StartsWith("speechbml") || !activeBehaviorStack.ContainsKey(id)) return;
			activeBehaviorStack.Remove(id);

			if (activeBehaviorStack.Count == 0) {
				// Send event that we're done talking.
				Debug.Log("speechbml block ends");
			}
		}
	}
}