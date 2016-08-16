using UnityEngine;
using System.Collections;

public class MecanimRetargetingSource : MonoBehaviour {
    
    Animator animator;
    HumanPoseHandler poseHandler;
    HumanPose pose;

    void Awake() {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInParent<Animator>();
        poseHandler = new HumanPoseHandler(animator.avatar, transform.parent);
    }

    public HumanPose GetPose() {
        return pose;
    }

    public void StorePose() {
        poseHandler.GetHumanPose(ref pose);
    }
}
