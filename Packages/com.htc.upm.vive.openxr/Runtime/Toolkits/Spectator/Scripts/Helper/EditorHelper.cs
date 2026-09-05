// Copyright HTC Corporation All Rights Reserved.

#if UNITY_EDITOR

using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace VIVE.OpenXR.Toolkits.Spectator.Helper
{
    public static class EditorHelper
    {
        // Microsoft definition
        // https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.metadata.conventions.backingfieldconvention?view=efcore-7.0#definition
        private const string BackingFieldConventionPrefix = "<";
        private const string BackingFieldConventionEndString = "k__BackingField";

        public static void ShowDefaultInspector(SerializedObject obj)
        {
            EditorGUI.BeginChangeCheck();
            obj.UpdateIfRequiredOrScript();

            SerializedProperty iterator = obj.GetIterator();
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                {
                    string originalLabelText = iterator.name;
                    if (originalLabelText.EndsWith(BackingFieldConventionEndString))
                    {
                        string fixLabelText = Regex.Replace(
                            originalLabelText.Substring(
                                1,
                                originalLabelText.Length - 1 - BackingFieldConventionPrefix.Length -
                                BackingFieldConventionEndString.Length),
                            "([a-z])([A-Z])",
                            "$1 $2");

                        EditorGUILayout.PropertyField(
                            property: iterator,
                            label: new GUIContent(fixLabelText),
                            includeChildren: true);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                }
            }

            obj.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();
        }

        public static string PropertyName(string propertyName)
        {
            // Microsoft definition
            // https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.metadata.conventions.backingfieldconvention?view=efcore-7.0#definition
            return $"<{propertyName}>k__BackingField";
        }
    }
}
#endif