using UnityEngine;
using System.Collections;

public class SampleAnimation : MonoBehaviour {

	public AnimationClip clip;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if (clip == null) return;
		clip.SampleAnimation(gameObject, 1.0f);
	}
}
