#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UMA.PoseTools;
using UMA;

namespace ASAP {
    [CustomEditor(typeof(ExpressionTargetEditor), true)]
    public class ExpressionTargetEditorInspector : Editor {

        ExpressionTargetEditor editor;
        bool showAddTarget;
        bool showDeleteTarget;
        int altLoad = 0;

        string newName;
        string newDescription;

        public void OnEnable() {
            editor = target as ExpressionTargetEditor;
            showAddTarget = false;
            showDeleteTarget = false;
        }


        public void AddTargetWindow() {}

        public void DeleteTargetWindow() {}

        public override void OnInspectorGUI() {
            if (editor == null || editor.ep == null) return;


            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            int currentSelected = EditorGUILayout.Popup(editor.currentlyEditing, ExpressionTargetEditor.ExpressionTargets);
            EditorGUILayout.HelpBox(ExpressionTargetEditor.ExpressionTargetDescriptions[editor.currentlyEditing], UnityEditor.MessageType.Info);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Prev")) {
                currentSelected = editor.currentlyEditing - 1;
                if (currentSelected < 0) currentSelected = ExpressionTargetEditor.ExpressionTargets.Length - 1;
            }
            if (GUILayout.Button("Next")) {
                currentSelected = editor.currentlyEditing + 1;
                if (currentSelected >= ExpressionTargetEditor.ExpressionTargets.Length) currentSelected = 0;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (!showAddTarget && !showDeleteTarget) {
                if (GUILayout.Button("Add")) {
                    showAddTarget = true;
                    newName = "";
                    newDescription = "";
                }
                if (GUILayout.Button("Delete")) {
                    showDeleteTarget = true;
                }
                if (GUILayout.Button("SplitToL/R")) {
                    string current = ExpressionTargetEditor.ExpressionTargets[editor.currentlyEditing];
                    editor.Rename(current + "_L");
                    editor.Duplicate(current + "_R");
                }
            } else {
                if (GUILayout.Button("Cancel")) {
                    showAddTarget = false;
                    showDeleteTarget = false;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(10.0f);

            if (showAddTarget) {
                newName = EditorGUILayout.TextField("Name", newName);
                newDescription = EditorGUILayout.TextField("Description", newDescription);
                if (GUILayout.Button("Add")) {
                    editor.AddTarget(newName, newDescription);
                    showAddTarget = false;
                }
            }

            if (showDeleteTarget) {
                if (GUILayout.Button("Confirm")) {
                    editor.RemoveCurrentTarget();
                    showDeleteTarget = false;
                }
            }


            GUILayout.Space(10.0f);

            if (currentSelected != editor.currentlyEditing) {
                editor.Save();
                editor.currentlyEditing = currentSelected;
                editor.Load();
            }


            for (int i = 0; i < ExpressionPlayer.PoseCount; i++) {
                GUILayout.BeginHorizontal();
                editor.currentUseControl[i] = GUILayout.Toggle(editor.currentUseControl[i], ExpressionPlayer.PoseNames[i], GUILayout.Width(130));
                float minVal = -1.0f;
                if (System.Array.IndexOf(editor.nonDefaultRanges, ExpressionPlayer.PoseNames[i]) > -1) {
                    minVal = 0.0f;
                }
                float currentTarget = GUILayout.HorizontalSlider(editor.currentTargetValues[i], minVal, 1.0f);
                if (!Mathf.Approximately(currentTarget, editor.currentTargetValues[i])) {
                    editor.currentTargetValues[i] = currentTarget;
                    editor.currentUseControl[i] = true;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10.0f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Defaults")) {
                editor.Defaults();
            }
            if (GUILayout.Button("Undo Changes")) {
                editor.Reset();
            }
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            altLoad = EditorGUILayout.Popup(altLoad, ExpressionTargetEditor.ExpressionTargets);
            if (GUILayout.Button("Load Values")) {
                editor.LoadValues(altLoad);
            }
            GUILayout.EndHorizontal();
        }
    }
}

#endif