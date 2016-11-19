using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UMA.PoseTools;
using UMA;
using ASAP;

public class AsapSlotScript : MonoBehaviour {

    bool isConfigured = false;

    public void OnCharacterBegun(UMAData umaData) {
        isConfigured = false;
    }


    public void OnDnaApplied(UMAData umaData) {}

    public void OnCharacterCompleted(UMAData umaData) {
        ASAPAgent_UMA asapAgent = umaData.gameObject.GetComponentInChildren<ASAPAgent_UMA>();
        if (asapAgent == null) {
            asapAgent = umaData.gameObject.AddComponent<ASAPAgent_UMA>();
            // need HumanoidRoot?/
        }
        if (!isConfigured) {
            isConfigured = true;
            asapAgent.UMAConfigure(umaData);
        }
    }
}