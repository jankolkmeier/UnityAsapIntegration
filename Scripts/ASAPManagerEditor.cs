#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(ASAPManagerEditor))]
public class ASAPManagerEditor : Editor {

    public override void OnInspectorGUI() {
        //ASAPManagerEditor rt = (ASAPManagerEditor)target;
        //DrawDefaultInspector();
    }
}
#endif