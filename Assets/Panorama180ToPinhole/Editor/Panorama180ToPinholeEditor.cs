using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Video;
using System;
using System.IO;

namespace Panorama180ToPinhole
{
    [CustomEditor(typeof(Panorama180ToPinhole))]
    public class Panorama180ToPinholeEditor : Editor {
        private Panorama180ToPinhole panorama180ToPinhole = null;

        public Panorama180ToPinholeEditor () {
        }

        public void OnEnable () {
            panorama180ToPinhole = (Panorama180ToPinhole)target;
        }

        /**
         * InspectorのカスタムGUI表示.
         */
        public override void OnInspectorGUI () {
            serializedObject.Update();

            var PanoramaVideoClip = serializedObject.FindProperty("PanoramaVideoClip");
            var StopVideo = serializedObject.FindProperty("StopVideo");

            var CameraParam_foldout = serializedObject.FindProperty("CameraParam_foldout");
            var CameraEyesType = serializedObject.FindProperty("CameraEyesType");
            var CameraLensType = serializedObject.FindProperty("CameraLensType");
            var CameraFOVH = serializedObject.FindProperty("CameraFOVH");
            var CameraFOVV = serializedObject.FindProperty("CameraFOVV");
            var CameraLensZScale = serializedObject.FindProperty("CameraLensZScale");

            var CaptureParam_foldout = serializedObject.FindProperty("CaptureParam_foldout");
            var CaptureCameraFOV = serializedObject.FindProperty("CaptureCameraFOV");
            var CaptureCameraTiltH = serializedObject.FindProperty("CaptureCameraTiltH");
            var CaptureCameraTiltV = serializedObject.FindProperty("CaptureCameraTiltV");

            var OutputParam_foldout = serializedObject.FindProperty("OutputParam_foldout");
            var OutputTextureSize = serializedObject.FindProperty("OutputTextureSize");
            var OutputCaptureFPS = serializedObject.FindProperty("OutputCaptureFPS");
            var OutputFiles = serializedObject.FindProperty("OutputFiles");
            var OutputPath = serializedObject.FindProperty("OutputPath");

            GUI.enabled = true;

            PanoramaVideoClip.objectReferenceValue = (VideoClip)EditorGUILayout.ObjectField("Video Clip", PanoramaVideoClip.objectReferenceValue, typeof(VideoClip), false);
            StopVideo.boolValue = EditorGUILayout.Toggle("Stop Video", StopVideo.boolValue);

            CameraParam_foldout.boolValue = EditorGUILayout.Foldout(CameraParam_foldout.boolValue, "Camera");
            if (CameraParam_foldout.boolValue) {
                CameraEyesType.intValue = EditorGUILayout.Popup("Eyes Type", CameraEyesType.intValue, new string[]{"Single Eye", "Two Eyes (Side By Side)"});
                CameraLensType.intValue = EditorGUILayout.Popup("Lens Type", CameraLensType.intValue, new string[]{"Equirectangular", "Fish Eye"});
            }

            CaptureParam_foldout.boolValue = EditorGUILayout.Foldout(CaptureParam_foldout.boolValue, "Capture");
            if (CaptureParam_foldout.boolValue) {
                CaptureCameraFOV.floatValue = EditorGUILayout.Slider("Camera FOV(H)", CaptureCameraFOV.floatValue, 10.0f, 180.0f);
                CaptureCameraTiltH.floatValue = EditorGUILayout.Slider("Camera Tilt(H)", CaptureCameraTiltH.floatValue, 0.0f, 90.0f);
                CaptureCameraTiltV.floatValue = EditorGUILayout.Slider("Camera Tilt(V)", CaptureCameraTiltV.floatValue, 0.0f, 90.0f);
            }

            OutputParam_foldout.boolValue = EditorGUILayout.Foldout(OutputParam_foldout.boolValue, "Output");
            if (OutputParam_foldout.boolValue) {
                OutputFiles.boolValue = EditorGUILayout.Toggle("Output Files", OutputFiles.boolValue);
                GUI.enabled = OutputFiles.boolValue;

                OutputTextureSize.vector2IntValue = EditorGUILayout.Vector2IntField("Texture Size", OutputTextureSize.vector2IntValue);
                OutputCaptureFPS.floatValue = EditorGUILayout.Slider("Capture fps", OutputCaptureFPS.floatValue, 0.01f, 30.0f);
                OutputPath.stringValue = EditorGUILayout.TextField("Output Path", OutputPath.stringValue);

                GUI.enabled = true;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

}

