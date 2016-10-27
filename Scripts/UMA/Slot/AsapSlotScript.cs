using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UMA.PoseTools;
using UMA;
using ASAP;

public class AsapSlotScript : MonoBehaviour {

//    bool isConfigured = false;

    public void OnCharacterBegun(UMAData umaData) {}


    public void OnDnaApplied(UMAData umaData) {}

    public void OnCharacterCompleted(UMAData umaData) {
        //if (isConfigured) return; // TODO: we might consider notifiying asap?
        ASAPAgent_UMA asapAgent = umaData.gameObject.GetComponentInChildren<ASAPAgent_UMA>();
        if (asapAgent == null) {
            asapAgent = umaData.gameObject.AddComponent<ASAPAgent_UMA>();
            // need HumanoidRoot?/
        }
        asapAgent.UMAConfigure(umaData);
        //isConfigured = true;
    }
}