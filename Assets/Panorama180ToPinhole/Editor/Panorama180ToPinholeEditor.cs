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
         * パスの変更のフォルダ選択ダイアログボックスを表示.
         */
        private string m_selectPath (string currentPath) {
            string retPath = EditorUtility.SaveFolderPanel("Select Path", currentPath, "");
            return (retPath == "") ? currentPath : retPath;
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
            var CameraPresetType = serializedObject.FindProperty("CameraPresetType");
            var CameraFOVH = serializedObject.FindProperty("CameraFOVH");
            var CameraFOVV = serializedObject.FindProperty("CameraFOVV");

            var CaptureParam_foldout = serializedObject.FindProperty("CaptureParam_foldout");
            var CaptureBackgroundTextureSize = serializedObject.FindProperty("CaptureBackgroundTextureSize");
            var CaptureCameraFOV = serializedObject.FindProperty("CaptureCameraFOV");
            var CaptureCameraTiltH = serializedObject.FindProperty("CaptureCameraTiltH");
            var CaptureCameraTiltV = serializedObject.FindProperty("CaptureCameraTiltV");

            var OutputParam_foldout = serializedObject.FindProperty("OutputParam_foldout");
            var OutputTextureSize = serializedObject.FindProperty("OutputTextureSize");
            var OutputCaptureFPS = serializedObject.FindProperty("OutputCaptureFPS");
            var OutputFiles = serializedObject.FindProperty("OutputFiles");
            var OutputPath = serializedObject.FindProperty("OutputPath");
            var OutputSpecifyRange = serializedObject.FindProperty("OutputSpecifyRange");
            var OutputStartTimeSec = serializedObject.FindProperty("OutputStartTimeSec");
            var OutputEndTimeSec = serializedObject.FindProperty("OutputEndTimeSec");

            GUI.enabled = true;

            PanoramaVideoClip.objectReferenceValue = (VideoClip)EditorGUILayout.ObjectField("Video Clip", PanoramaVideoClip.objectReferenceValue, typeof(VideoClip), false);
            StopVideo.boolValue = EditorGUILayout.Toggle("Stop Video", StopVideo.boolValue);

            CameraParam_foldout.boolValue = EditorGUILayout.Foldout(CameraParam_foldout.boolValue, "Camera");
            if (CameraParam_foldout.boolValue) {
                CameraEyesType.intValue = EditorGUILayout.Popup("Eyes Type", CameraEyesType.intValue, new string[]{"Single Eye", "Two Eyes (Side By Side)"});
                CameraLensType.intValue = EditorGUILayout.Popup("Lens Type", CameraLensType.intValue, new string[]{"Equirectangular", "Fish Eye"});
                CameraPresetType.intValue = EditorGUILayout.Popup("Camera Preset", CameraPresetType.intValue,
                        new string[]{"None", "Custom", "GoPro 6-7 : 4 x 3 : Wide", "GoPro 6-7 : 16 x 9 : Wide",
                                 "GoPro 12 : 4 x 3 : Wide : HyperSmooth On", "GoPro 12 : 4 x 3 : Wide : HyperSmooth Off",
                                 "GoPro 12 : 16 x 9 : Wide : HyperSmooth On", "GoPro 12 : 16 x 9 : Wide : HyperSmooth Off",
                                 "GoPro 12 : 8 x 7 : Wide : HyperSmooth On", "GoPro 12 : 8 x 7 : Wide : HyperSmooth Off"
                                 });

                GUI.enabled = (CameraPresetType.intValue == 1);
                if (CameraPresetType.intValue >= 2) {
                    switch ((Panorama180ToPinhole.CameraFOVPresetType)CameraPresetType.intValue) {
                        case Panorama180ToPinhole.CameraFOVPresetType.GoPro7_4x3_wide:
                            CameraFOVH.floatValue = 122.6f;
                            CameraFOVV.floatValue = 94.4f;
                            break;
                        case Panorama180ToPinhole.CameraFOVPresetType.GoPro7_16x9_wide:
                            CameraFOVH.floatValue = 118.2f;
                            CameraFOVV.floatValue = 69.5f;
                            break;
                        case Panorama180ToPinhole.CameraFOVPresetType.GoPro12_4x3_wide_HyperSmooth_on:
                            CameraFOVH.floatValue = 113.0f;
                            CameraFOVV.floatValue = 87.0f;
                            break;
                        case Panorama180ToPinhole.CameraFOVPresetType.GoPro12_4x3_wide_HyperSmooth_off:
                            CameraFOVH.floatValue = 121.0f;
                            CameraFOVV.floatValue = 93.0f;
                            break;
                        case Panorama180ToPinhole.CameraFOVPresetType.GoPro12_16x9_wide_HyperSmooth_on:
                            CameraFOVH.floatValue = 109.0f;
                            CameraFOVV.floatValue = 63.0f;
                            break;
                        case Panorama180ToPinhole.CameraFOVPresetType.GoPro12_16x9_wide_HyperSmooth_off:
                            CameraFOVH.floatValue = 118.0f;
                            CameraFOVV.floatValue = 69.0f;
                            break;
                        case Panorama180ToPinhole.CameraFOVPresetType.GoPro12_8x7_wide_HyperSmooth_on:
                            CameraFOVH.floatValue = 113.0f;
                            CameraFOVV.floatValue = 100.0f;
                            break;
                        case Panorama180ToPinhole.CameraFOVPresetType.GoPro12_8x7_wide_HyperSmooth_off:
                            CameraFOVH.floatValue = 122.0f;
                            CameraFOVV.floatValue = 108.0f;
                            break;
                    }
                }
                CameraFOVH.floatValue = EditorGUILayout.Slider("Camera FOV(H)", CameraFOVH.floatValue, 0.01f, 180.0f);
                CameraFOVV.floatValue = EditorGUILayout.Slider("Camera FOV(V)", CameraFOVV.floatValue, 0.01f, 180.0f);
                GUI.enabled = true;
            }

            CaptureParam_foldout.boolValue = EditorGUILayout.Foldout(CaptureParam_foldout.boolValue, "Capture");
            if (CaptureParam_foldout.boolValue) {
                CaptureBackgroundTextureSize.intValue = EditorGUILayout.Popup("Background Texture Size", CaptureBackgroundTextureSize.intValue, new string[]{"1024", "2048", "4096", "8192"});
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

                OutputSpecifyRange.boolValue = EditorGUILayout.Toggle("SpecifyRange", OutputSpecifyRange.boolValue);
                GUI.enabled = OutputFiles.boolValue && OutputSpecifyRange.boolValue;
                OutputStartTimeSec.floatValue = EditorGUILayout.FloatField("Start Time (sec)", OutputStartTimeSec.floatValue);
                OutputEndTimeSec.floatValue = EditorGUILayout.FloatField("End Time (sec)", OutputEndTimeSec.floatValue);

                GUI.enabled = OutputFiles.boolValue;

                GUILayout.BeginHorizontal();
                OutputPath.stringValue = EditorGUILayout.TextField("Output Path", OutputPath.stringValue);
                if (GUILayout.Button(new GUIContent("Select ...", "Display folder selection dialog box"), GUILayout.Width(70))) {
                    OutputPath.stringValue = m_selectPath(OutputPath.stringValue);
                    // "EndLayoutGroup: BeginLayoutGroup must be called first. "が出るのを避ける.
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }
                GUILayout.EndHorizontal();

                GUI.enabled = true;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

}

