using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UMA.PoseTools;
using UMA;
using ASAP;

public class AsapSlotScript : MonoBehaviour {

//    bool isConfigured = false;

    public void OnCharacterBegun(UMAData umaData) {
    }


    public void OnDnaApplied(UMAData umaData) {
    }

    public void OnCharacterCompleted(UMAData umaData) {
        //if (isConfigured) return; // TODO: we might consider notifiying asap?
        ASAPAgent_UMA asapAgent = umaData.gameObject.GetComponentInChildren<ASAPAgent_UMA>();
        if (asapAgent == null) {
            asapAgent = umaData.gameObject.AddComponent<ASAPAgent_UMA>();
            // need HumanoidRoot?/
        }

        asapAgent.UMAConfigure(umaData);
        asapAgent.Initialize();
//        isConfigured = true;

        //var retargetTarget = umaData.GetComponent<Retargetting>();
        //if (retargetTarget == null)
        //    retargetTarget = umaData.gameObject.AddComponent<Retargetting>();

        //retargetTarget.Source = FindObjectOfType<ASAPMiddelware>().retargetSrc;


        //retargetTarget.configureAtRuntime = true;
        /*
        var basicAsap = umaData.GetComponent<BasicAsap>();
        if (basicAsap == null)
            umaData.gameObject.AddComponent<BasicAsap>();

        var mecanimLipSync = umaData.GetComponent<MecanimLipSync>();
        if (mecanimLipSync == null)
            umaData.gameObject.AddComponent<MecanimLipSync>();

        var spawnSpeak = umaData.GetComponent<SpawnSpeak>();
        if (spawnSpeak == null)
            umaData.gameObject.AddComponent<SpawnSpeak>();
            */

    }

}
