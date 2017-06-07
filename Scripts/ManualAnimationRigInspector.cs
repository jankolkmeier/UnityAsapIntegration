#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Xml;

namespace ASAP {

    [CustomEditor(typeof(ManualAnimationRig), true)]
    public class ManualAnimationRigInspector : Editor {

        private ManualAnimationRig animationRig;
        private AnimationClip[] animationClips;
        private AnimationClip blankPose;
        private AnimationClip restPose;

        private int currentClipIndex = 0;
        private Animation animation;

        /*
        public bool enableControl = false;
            if (enableControl && !ManualAnimation) ManualAnimation = true;
        if (!enableControl && ManualAnimation) ManualAnimation = false;
        enableControl = ManualAnimation;*/

        public void OnEnable() {
            List<AnimationClip> _animationClips = new List<AnimationClip>();
            animationRig = target as ManualAnimationRig;
            string[] animationLocations = {"Assets/UnityAsapIntegration/Animations"};
            string[] clipGUIDs = AssetDatabase.FindAssets("t:AnimationClip", animationLocations);

            foreach (string guid in clipGUIDs) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = (AnimationClip)AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip));
                _animationClips.Add(clip);
            }

            animationClips = _animationClips.ToArray();

            animation = animationRig.vjointRoot.parent.gameObject.GetComponent<Animation>();
            if (animation == null) {
                //DestroyImmediate(animation);
                //Debug.Log("Destroyed...");
                animation = animationRig.vjointRoot.parent.gameObject.AddComponent<Animation>();
            }

            /*
            foreach (int i = 0; i < animation.GetClipCount(); if++) {
                animation.RemoveClip(animation.GetClip()
            }*/

            /*
            foreach (AnimationClip clip in animationClips) {
                //clip.legacy = true;
                animation.AddClip(clip, clip.name);
            }*/
        }

        public override void OnInspectorGUI() {
            if (animationRig == null || animation == null) return;
            animationRig.ManualAnimation = GUILayout.Toggle(animationRig.ManualAnimation, "ManualAnimation", GUILayout.Width(130));
            int newCurrentClipIndex = EditorGUILayout.Popup(currentClipIndex, (from clip in animationClips select clip.name).ToArray());
            if (newCurrentClipIndex != currentClipIndex || animation.clip == null) {
                foreach (AnimationClip clip in animationClips) {
                    if (animation.GetClip(clip.name) != null) { 
                        animation.RemoveClip(clip);
                    }
                }

                animation.AddClip(animationClips[newCurrentClipIndex], animationClips[newCurrentClipIndex].name);
                animation.clip = animationClips[newCurrentClipIndex];
                Selection.activeGameObject = animationRig.gameObject;
                EditorApplication.ExecuteMenuItem("Window/Animation");
                //if (blankPose != null) blankPose.SampleAnimation(animationRig.vjointRoot.parent.gameObject, 0.0f);
                //if (restPose != null) restPose.SampleAnimation(animationRig.vjointRoot.parent.gameObject, 0.0f);
                animationRig.ResetToBlankPose();
                if (animation.clip != null) animation.clip.SampleAnimation(animationRig.vjointRoot.parent.gameObject, 0.0f);
            }

            currentClipIndex = newCurrentClipIndex;

            // GUILayout.Label(AnimationUtility.);

            if (GUILayout.Button("Export")) {
                ExportAnimation(animationClips[currentClipIndex], 30);
            }
        }

        // Export animation... current clip?



        private void ExportAnimation(AnimationClip clip, float frameRate) {
            if (animationRig == null) return;

            Dictionary<string, bool> animatedBones = new Dictionary<string, bool>();
            Dictionary<string, float> syncPoints = new Dictionary<string, float>();

            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings (clip)) {
                //AnimationCurve curve = AnimationUtility.GetEditorCurve (clip, binding);

                string[] pathElems = binding.path.Split('/');
                string hAnimName = pathElems[pathElems.Length - 1];
                if (binding.propertyName.StartsWith("m_LocalRotation")) {
                    if (!animatedBones.ContainsKey(hAnimName))
                        animatedBones.Add(hAnimName, false);
                }

                if (binding.propertyName.StartsWith("m_LocalPosition")) {
                    if (!animatedBones.ContainsKey(hAnimName)) {
                        animatedBones.Add(hAnimName, true);
                    }
                    else animatedBones[hAnimName] = true;
                }
            }

            foreach (AnimationEvent ae in AnimationUtility.GetAnimationEvents(clip)) {
                if (ae.functionName.StartsWith("Sync_")) {
                    string syncType = ae.functionName.Substring(5);
                    if (syncType == "custom") syncType = ae.stringParameter;
                    syncPoints.Add(syncType, ae.time/clip.length);
                    Debug.Log(ae.functionName+" "+ syncType + " "+ae.time);
                }
            }

            string parts = "";
            List<Transform> partReferences = new List<Transform>();

            // Get a nice ordered list of the bones:
            foreach (BoneSpec boneSpec in animationRig.controlledAgent.agentSpec.bones) {
                foreach (KeyValuePair<string,bool> animatedBone in animatedBones) {
                    if (animatedBone.Key == boneSpec.hAnimName) {
                        parts += boneSpec.hAnimName + " ";
                        Transform boneObject = animationRig.FindDeepChild(animationRig.vjointRoot.parent, boneSpec.hAnimName);
                        partReferences.Add(boneObject);
                        break;
                    }
                }
            }
            parts = parts.Trim();

            string encoding = "R";
            string rotationEncoding = "quaternions";

            // if root has translation: we use T1R
            if (animatedBones.ContainsKey("HumanoidRoot") && animatedBones["HumanoidRoot"]) {
                encoding = "T1R";
                // Could also use "TR" if there is a non-root bone with translation...
                // but we don't support those non-root translations in animations atm anyway...
            }


            /*
            int frameLength = 0;
            if (encoding == "R") frameLength = 4 * partReferences.Count;
            if (encoding == "T1R") frameLength = 3 + 4 * partReferences.Count;
            if (encoding == "TR") frameLength = 7 * partReferences.Count;
            */
            List<float[]> frames = new List<float[]>();

            float delta = 1 / frameRate;
            for (int frame = 0; frame < Math.Max(1, clip.length * frameRate); frame++) {
                float t = delta * frame;
                clip.SampleAnimation(animationRig.vjointRoot.parent.gameObject, t);
                //yield return new WaitForSeconds(delta);
                List<float> elems = new List<float>();

                elems.Add(t);
                foreach (Transform partReference in  partReferences) {
                    if (encoding == "TR" || (encoding == "T1R" && partReference.name == "HumanoidRoot")) {
                        elems.AddRange(ExtractAsapVectorPosition(partReference));
                        elems.AddRange(ExtractAsapQuaternionRotation(partReference));
                    } else if (encoding == "R" || encoding == "T1R") {
                        elems.AddRange(ExtractAsapQuaternionRotation(partReference));
                    }
                }
                //if (frameLength != elems.Count) Debug.LogError("Number of values in frame ("+elems.Count+") not equal expected ("+frameLength+")");
                frames.Add(elems.ToArray());
            }
            Debug.Log("Exporting gesture " + clip.name + ". Duration: " + clip.length + " (" + frames.Count + " frames total)");
            WriteXML(clip, parts, rotationEncoding, encoding, frames, syncPoints, ManualAnimationRig.ExportMode.ProcAnimationGesture);
        }

        // returns a float[4] quaternion ready for use in ASAP...
        private float[] ExtractAsapQuaternionRotation(Transform t) {
            return new [] {
                -t.localRotation.w,
                -t.localRotation.x,
                 t.localRotation.y,
                 t.localRotation.z
            };
        }

        private float[] ExtractAsapVectorPosition(Transform t) {
            return new [] {
                -t.localPosition.x,
                 t.localPosition.y,
                 t.localPosition.z
            };
        }

        // TODO: differnt types of output (restpose, keyframe, handshape, ...)
        private void WriteXML(AnimationClip clip, string parts, string rotationEncoding, string encoding, List<float[]> frames, Dictionary<string,float> syncPoints, ManualAnimationRig.ExportMode mode) {
            MemoryStream ms = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.OmitXmlDeclaration = true;

            using (XmlWriter w = XmlWriter.Create(ms, settings)) {
                w.WriteStartDocument();
                w.WriteStartElement("bml", "http://www.bml-initiative.org/bml/bml-1.0"); // <bml ...>
                w.WriteAttributeString("id", "exportedBml1");
                w.WriteAttributeString("xmlns:bmlt", "http://hmi.ewi.utwente.nl/bmlt");

                if (mode == ManualAnimationRig.ExportMode.Keyframes) {
                    w.WriteStartElement("bmlt:keyframe"); // <bmlt:keyframe ...>
                    w.WriteAttributeString("id", "keyframes1");
                    w.WriteStartElement("SkeletonInterpolator"); // <SkeletonInterpolator ...>
                    w.WriteAttributeString("parts", parts);
                    w.WriteAttributeString("rotationEncoding", rotationEncoding);
                    w.WriteAttributeString("encoding", encoding); // ... xmlns=""
                    foreach (float[] frame in frames) {
                        w.WriteString("\n      ");
                        w.WriteString(string.Join(" ", frame.Select(f => f.ToString("0.0##")).ToArray()));
                    }
                    w.WriteString("\n    ");
                    w.WriteEndElement(); // </SkeletonInterpolator>
                    w.WriteEndElement(); // </bmlt:keyframe>
                } else if (mode == ManualAnimationRig.ExportMode.ProcAnimationGesture) {
                    w.WriteStartElement("bmlt:procanimationgesture"); // <bmlt:procanimationgesture ...>
                    w.WriteAttributeString("id", "procgesture1");
                    w.WriteStartElement("ProcAnimation");  // < ProcAnimation ...>
                    w.WriteAttributeString("prefDuration", clip.length.ToString("0.0##"));
                    w.WriteAttributeString("minDuration", clip.length.ToString("0.0##"));
                    w.WriteAttributeString("maxDuration", clip.length.ToString("0.0##"));
                    w.WriteStartElement("SkeletonInterpolator"); // <SkeletonInterpolator ...>
                    w.WriteAttributeString("parts", parts);
                    w.WriteAttributeString("rotationEncoding", rotationEncoding);
                    w.WriteAttributeString("encoding", encoding); // ... xmlns=""

                    foreach (float[] frame in frames) {
                        w.WriteString("\n      ");
                        w.WriteString(string.Join(" ", frame.Select(f => f.ToString("0.0##")).ToArray()));
                    }
                    w.WriteString("\n    ");
                    w.WriteEndElement(); // </SkeletonInterpolator>

                    foreach (KeyValuePair<string,float> syncPoint in syncPoints.OrderBy(pair => pair.Value)) {
                        w.WriteStartElement("KeyPosition");
                        w.WriteAttributeString("id", syncPoint.Key);
                        w.WriteAttributeString("weight", "1");
                        w.WriteAttributeString("time", syncPoint.Value.ToString("0.0##"));
                        w.WriteEndElement();
                    }

                    w.WriteEndElement(); // </ProcAnimation>
                    w.WriteEndElement(); // </bmlt:procanimationgesture>
                }

                w.WriteEndElement(); // </bml>
                w.WriteEndDocument();
            }
            StreamReader sr = new StreamReader(ms);
            ms.Seek(0, SeekOrigin.Begin);
            Debug.Log(sr.ReadToEnd());
            sr.Dispose();
        }


    }

}

/*

<bml xmlns="http://www.bml-initiative.org/bml/bml-1.0"  id="bml1" xmlns:bmlt="http://hmi.ewi.utwente.nl/bmlt">
<bmlt:keyframe id="keyframes1">
<SkeletonInterpolator xmlns="" rotationEncoding="quaternions" parts="HumanoidRoot vl5 l_hip l_knee r_hip r_knee" encoding="T1R">
0.0 0.0 0.854 -0.028 1.0 0.0 0.0 0.0 -1.0 0.0 0.0 0.0 -1.0 0.0 0.0 0.0 -1.0 0.0 0.0 0.0 -1.0 0.0 0.0 0.0 -1.0 0.0 0.0 0.0
0.033 0.0 0.869 -0.028 0.998 0.0 -0.058 0.0 -1.0 0.0 0.0 0.0 -1.0 0.009 -0.017 0.003 -1.0 -0.018 0.002 0.002 -1.0 0.011 0.014 0.0 -1.0 -0.026 0.0 0.0
0.067 0.0 0.907 -0.028 0.977 0.0 -0.213 0.0 -1.0 0.0 0.0 0.0 -0.998 0.032 -0.061 0.01 -0.998 -0.065 0.006 0.006 -0.998 0.039 0.052 0.002 -0.996 -0.094 0.0 0.0
0.1 0.0 0.961 -0.028 0.903 0.0 -0.43 0.0 -1.0 0.0 0.0 0.0 -0.99 0.066 -0.121 0.016 -0.991 -0.13 0.011 0.011 -0.992 0.079 0.103 0.008 -0.982 -0.189 0.0 0.0
0.133 0.0 1.021 -0.028 0.75 0.0 -0.661 0.0 -1.0 0.0 0.0 0.0 -0.977 0.105 -0.186 0.017 -0.979 -0.202 0.015 0.015 -0.979 0.122 0.16 0.02 -0.956 -0.292 0.0 0.0
0.167 0.0 1.079 -0.028 0.522 0.0 -0.853 0.0 -1.0 0.0 0.0 0.0 -0.959 0.142 -0.246 0.014 -0.962 -0.271 0.018 0.019 -0.963 0.162 0.214 0.036 -0.921 -0.389 0.0 0.0
0.2 0.0 1.126 -0.028 0.25 0.0 -0.968 0.0 -1.0 0.0 0.0 0.0 -0.94 0.172 -0.293 0.007 -0.945 -0.326 0.02 0.02 -0.946 0.193 0.256 0.052 -0.886 -0.464 0.0 0.0
0.233 0.0 1.153 -0.028 -0.016 0.0 -1.0 0.0 -1.0 0.0 0.0 0.0 -0.928 0.189 -0.32 0.002 -0.934 -0.357 0.02 0.021 -0.934 0.211 0.28 0.063 -0.862 -0.507 0.0 0.0
0.267 0.0 1.153 -0.028 -0.241 0.0 -0.97 0.0 -1.0 0.0 0.0 0.0 -0.928 0.189 -0.32 0.002 -0.934 -0.357 0.02 0.021 -0.934 0.211 0.28 0.063 -0.862 -0.507 0.0 0.0
0.3 0.0 1.126 -0.028 -0.475 0.0 -0.88 0.0 -1.0 0.0 0.0 0.0 -0.94 0.172 -0.293 0.007 -0.945 -0.326 0.02 0.02 -0.946 0.193 0.256 0.052 -0.886 -0.464 0.0 0.0
0.333 0.0 1.079 -0.028 -0.689 0.0 -0.724 0.0 -1.0 0.0 0.0 0.0 -0.959 0.142 -0.246 0.014 -0.962 -0.271 0.018 0.019 -0.963 0.162 0.214 0.036 -0.921 -0.389 0.0 0.0
0.367 0.0 1.021 -0.028 -0.852 0.0 -0.524 0.0 -1.0 0.0 0.0 0.0 -0.977 0.105 -0.186 0.017 -0.979 -0.202 0.015 0.015 -0.979 0.122 0.16 0.02 -0.956 -0.292 0.0 0.0
0.4 0.0 0.961 -0.028 -0.948 0.0 -0.318 0.0 -1.0 0.0 0.0 0.0 -0.99 0.066 -0.121 0.016 -0.991 -0.13 0.011 0.011 -0.992 0.079 0.103 0.008 -0.982 -0.189 0.0 0.0
0.433 0.0 0.907 -0.028 -0.989 0.0 -0.146 0.0 -1.0 0.0 0.0 0.0 -0.998 0.032 -0.061 0.01 -0.998 -0.065 0.006 0.006 -0.998 0.039 0.052 0.002 -0.996 -0.094 0.0 0.0
0.467 0.0 0.869 -0.028 -0.999 0.0 -0.045 0.0 -1.0 0.0 0.0 0.0 -1.0 0.009 -0.017 0.003 -1.0 -0.018 0.002 0.002 -1.0 0.011 0.014 0.0 -1.0 -0.026 0.0 0.0
</SkeletonInterpolator>
</bmlt:keyframe>
</bml>


<bml xmlns="http://www.bml-initiative.org/bml/bml-1.0"  id="bml1" xmlns:bmlt="http://hmi.ewi.utwente.nl/bmlt">
    <bmlt:procanimationgesture id="ani">
        <ProcAnimation prefDuration="0.5" minDuration="0.2" maxDuration="2">
            <SkeletonInterpolator parts="r_shoulder r_elbow r_wrist" rotationEncoding="quaternions" encoding="R">
              0.0 -0.942 0.147 -0.189 0.235 -0.746 0.638 0.177 -0.068 0.97 -0.002 -0.141 0.197
            </SkeletonInterpolator>
            <KeyPosition id="start" weight="1" time="0.0"/>
            <KeyPosition id="ready" weight="1" time="0.2"/>
            <KeyPosition id="strokeStart" weight="1" time="0.3"/>
            <KeyPosition id="stroke" weight="1" time="0.5"/>
            <KeyPosition id="strokeEnd" weight="1" time="0.6"/>
            <KeyPosition id="relax" weight="1" time="0.8"/>
            <KeyPosition id="end" weight="1" time="1"/>
        </ProcAnimation>
    </bmlt:procanimationgesture>
</bml>

*/
#endif
