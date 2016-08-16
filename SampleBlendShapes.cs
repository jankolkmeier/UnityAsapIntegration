using UnityEngine;
using System.Collections;

public class SampleBlendShapes : MonoBehaviour {

	public SkinnedMeshRenderer skinnedMeshRenderer;

	// Use this for initialization
	void Start () {
		skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer> ();
		Mesh m = skinnedMeshRenderer.sharedMesh;
		if (m.blendShapeCount == 0) Debug.Log("No blend shapes on " +transform.name);
		for (int i= 0; i < m.blendShapeCount; i++) {
			string s = m.GetBlendShapeName(i);
			Debug.Log(transform.name+" blend shape: " + i + " " + s);
		}
	}
	
	// Update is called once per frame
	void Update () {
	}
}
