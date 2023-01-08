using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Apple.Core
{
    [CustomEditor(typeof(AppleBuildProfile))]
    public class AppleBuildProfileEditor : Editor
    {
        public const float VerticalUIPadding      = 5.0f;
        public const string BuildPlayerWindowType = "UnityEditor.BuildPlayerWindow,UnityEditor";

        private static Dictionary<Editor, bool>             _editorFoldouts = new Dictionary<Editor, bool>();
        private static Dictionary<ScriptableObject, Editor> _editors        = new Dictionary<ScriptableObject, Editor>();

        class UIStrings
        {
            public const string UnityBuildConfigSectionLabelText          = "Unity Build Configuration";
            public const string UnityActiveBuildTargetLabelText           = "Current build target";
            public const string UnityBuildSettingsButtonLabelText         = "Unity Build Settings...";
            public const string AutomationSettingsSectionLabelText        = "Automation Settings";
            public const string AutomateInfoPlistToggleLabelText          = "Automate info.plist";
            public const string AppUsesNonExemptEncryptionToggleLabelText = "ITSAppUsesNonExemptEncryption";
            public const string DefaultInfoPlistFieldLabelText            = "Default info.plist";
            public const string DefaultMinimumMacOSVersionText            = "10.15.0";
            public const string MinimumOSVersionFieldLabelText            = "Minimum OS Version";
            public const string AutomateEntitlementsToggleLabelText       = "Automate Entitlements";
            public const string DefaultEntitlementsFieldLabelText         = "Default Entitlements";
            public const string iOSBuildTargetName                        = "iOS";
            public const string tvOSBuildTargetName                       = "tvOS";
            public const string macOSBuildTargetName                      = "macOS";
        }

        /// <summary>
        /// Called when drawing associated tab of the Project Settings window. See: AppleBuildSettingsProvider.cs
        /// Also called to draw within the Inspector window when a user selects an AppleBuildProfile asset
        /// </summary>
        public override void OnInspectorGUI()
        {
            var appleBuildProfile = target as AppleBuildProfile;

            serializedObject.Update();

#region Draw Build Summary
            
            GUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(UIStrings.UnityBuildConfigSectionLabelText, EditorStyles.boldLabel);
            GUILayout.EndHorizontal();

            string buildTargetName;
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.iOS:
                    buildTargetName = UIStrings.iOSBuildTargetName;
                    break;
                case BuildTarget.tvOS:
                    buildTargetName = UIStrings.tvOSBuildTargetName;
                    break;
                case BuildTarget.StandaloneOSX:
                    buildTargetName = UIStrings.macOSBuildTargetName;
                    break;
                default:
                    buildTargetName = string.Empty;
                    break;
            }

            GUILayout.Label($"{UIStrings.UnityActiveBuildTargetLabelText} {buildTargetName}");

            if (GUILayout.Button(UIStrings.UnityBuildSettingsButtonLabelText))
            {
                EditorWindow.GetWindow(Type.GetType(BuildPlayerWindowType));
            }

            GUILayout.EndVertical();

#endregion // Draw Build Summary

            GUILayout.Space(VerticalUIPadding);

#region Draw Build Profile Properties

            GUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(UIStrings.AutomationSettingsSectionLabelText, EditorStyles.boldLabel);
            GUILayout.EndHorizontal();

            // Info Plist
            appleBuildProfile.AutomateInfoPlist = EditorGUILayout.Toggle(UIStrings.AutomateInfoPlistToggleLabelText, appleBuildProfile.AutomateInfoPlist);

            EditorGUI.indentLevel++;

            if (appleBuildProfile.AutomateInfoPlist)
            {
                appleBuildProfile.AppUsesNonExemptEncryption = EditorGUILayout.Toggle(UIStrings.AppUsesNonExemptEncryptionToggleLabelText, appleBuildProfile.AppUsesNonExemptEncryption);
                appleBuildProfile.DefaultInfoPlist = EditorGUILayout.ObjectField(UIStrings.DefaultInfoPlistFieldLabelText, appleBuildProfile.DefaultInfoPlist, typeof(UnityEngine.Object), false);

                if (appleBuildProfile.MinimumOSVersion == string.Empty)
                {
                    switch (EditorUserBuildSettings.activeBuildTarget)
                    {
                        case BuildTarget.iOS:
                            appleBuildProfile.MinimumOSVersion = PlayerSettings.iOS.targetOSVersionString;
                            break;
                        case BuildTarget.tvOS:
                            appleBuildProfile.MinimumOSVersion = PlayerSettings.tvOS.targetOSVersionString;
                            break;
                        case BuildTarget.StandaloneOSX:
                            appleBuildProfile.MinimumOSVersion = UIStrings.DefaultMinimumMacOSVersionText;
                            break;
                    }
                }

                appleBuildProfile.MinimumOSVersion = EditorGUILayout.TextField(UIStrings.MinimumOSVersionFieldLabelText, appleBuildProfile.MinimumOSVersion);
            }

            EditorGUI.indentLevel--;

            // Entitlements
            appleBuildProfile.AutomateEntitlements = EditorGUILayout.Toggle(UIStrings.AutomateEntitlementsToggleLabelText, appleBuildProfile.AutomateEntitlements);

            EditorGUI.indentLevel++;

            if (appleBuildProfile.AutomateEntitlements)
            {
                appleBuildProfile.DefaultEntitlements = EditorGUILayout.ObjectField(UIStrings.DefaultEntitlementsFieldLabelText, appleBuildProfile.DefaultEntitlements, typeof(UnityEngine.Object), false);
            }

            EditorGUI.indentLevel--;

            GUILayout.EndVertical();

#endregion // Draw Build Profile Properties

            GUILayout.Space(VerticalUIPadding);

#region Draw Apple Build Steps

            GUILayout.BeginVertical();


            List<string> buildStepNames = appleBuildProfile.buildSteps.Keys.ToList();
            buildStepNames.Sort();

            foreach (var name in buildStepNames)
            {
                var buildStep = appleBuildProfile.buildSteps[name];

                if (!_editors.ContainsKey(buildStep))
                {
                    _editors.Add(buildStep, CreateEditor(buildStep));
                }

                var currEditor = _editors[buildStep];

                if (!_editorFoldouts.ContainsKey(currEditor))
                {
                    _editorFoldouts.Add(currEditor, false);
                }

                var showFoldout = _editorFoldouts[currEditor];

                GUILayout.BeginHorizontal(EditorStyles.toolbar);

                // TODO: Should really be a disclosure and not this eye icon
                var arrow = showFoldout ? EditorGUIUtility.IconContent("d_scenevis_visible_hover") : EditorGUIUtility.IconContent("d_scenevis_hidden_hover");

                if (GUILayout.Button(arrow, EditorStyles.toolbarButton, GUILayout.Width(24)))
                {
                    showFoldout = !showFoldout;
                    _editorFoldouts[currEditor] = showFoldout;
                }

                GUILayout.Label(buildStep.DisplayName, EditorStyles.boldLabel);

                GUILayout.EndHorizontal();

                if (showFoldout)
                {
                    EditorGUI.indentLevel++;
                    GUILayout.BeginVertical(EditorStyles.helpBox);

                    currEditor.OnInspectorGUI();

                    GUILayout.EndVertical();
                    GUILayout.Space(VerticalUIPadding);
                    EditorGUI.indentLevel--;
                }
            }

            GUILayout.EndVertical();

#endregion // Draw Apple Build Steps

            serializedObject.ApplyModifiedProperties();
        }
    }
}
