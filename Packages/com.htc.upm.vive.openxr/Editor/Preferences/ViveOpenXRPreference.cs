// Copyright HTC Corporation All Rights Reserved.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.XR.Management.Metadata;

namespace VIVE.OpenXR.Editor
{
	[InitializeOnLoad]
	public static class ViveOpenXRPreference
	{
		#region Log
		static StringBuilder m_sb = null;
		static StringBuilder sb {
			get {
				if (m_sb == null) { m_sb = new StringBuilder(); }
				return m_sb;
			}
		}
		const string LOG_TAG = "VIVE.OpenXR.Editor.ViveOpenXRPreference";
		static void DEBUG(StringBuilder msg) { Debug.LogFormat("{0} {1}", LOG_TAG, msg); }
		static void ERROR(StringBuilder msg) { Debug.LogErrorFormat("{0} {1}", LOG_TAG, msg); }
		#endregion

		static ViveOpenXRPreference()
		{
			EditorApplication.update += OnUpdate;
		}

		#region Scripting Symbols
		internal struct ScriptingDefinedSettings
		{
			public string[] scriptingDefinedSymbols;
			public BuildTargetGroup[] targetGroups;

			public ScriptingDefinedSettings(string[] symbols, BuildTargetGroup[] groups)
			{
				scriptingDefinedSymbols = symbols;
				targetGroups = groups;
			}
		}

		const string DEFINE_USE_VRM_0_x = "USE_VRM_0_x";
		static readonly ScriptingDefinedSettings m_ScriptDefineSettingVrm0 = new ScriptingDefinedSettings(
			new string[] { DEFINE_USE_VRM_0_x, },
			new BuildTargetGroup[] { BuildTargetGroup.Android, }
		);

		static void AddScriptingDefineSymbols(ScriptingDefinedSettings setting)
		{
			for (int group_index = 0; group_index < setting.targetGroups.Length; group_index++)
			{
				var group = setting.targetGroups[group_index];
				string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
				List<string> allDefines = definesString.Split(';').ToList();
				for (int symbol_index = 0; symbol_index < setting.scriptingDefinedSymbols.Length; symbol_index++)
				{
					if (!allDefines.Contains(setting.scriptingDefinedSymbols[symbol_index]))
					{
						sb.Clear().Append("AddDefineSymbols() ").Append(setting.scriptingDefinedSymbols[symbol_index]).Append(" to group ").Append(group); DEBUG(sb);
						allDefines.Add(setting.scriptingDefinedSymbols[symbol_index]);
					}
					else
					{
						sb.Clear().Append("AddDefineSymbols() ").Append(setting.scriptingDefinedSymbols[symbol_index]).Append(" already existed."); DEBUG(sb);
					}
				}
				PlayerSettings.SetScriptingDefineSymbolsForGroup(
					group,
					string.Join(";", allDefines.ToArray())
				);
			}
		}
		static void RemoveScriptingDefineSymbols(ScriptingDefinedSettings setting)
		{
			for (int group_index = 0; group_index < setting.targetGroups.Length; group_index++)
			{
				var group = setting.targetGroups[group_index];
				string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
				List<string> allDefines = definesString.Split(';').ToList();
				for (int symbol_index = 0; symbol_index < setting.scriptingDefinedSymbols.Length; symbol_index++)
				{
					if (allDefines.Contains(setting.scriptingDefinedSymbols[symbol_index]))
					{
						sb.Clear().Append("RemoveDefineSymbols() ").Append(setting.scriptingDefinedSymbols[symbol_index]).Append(" from group ").Append(group); DEBUG(sb);
						allDefines.Remove(setting.scriptingDefinedSymbols[symbol_index]);
					}
					else
					{
						sb.Clear().Append("RemoveDefineSymbols() ").Append(setting.scriptingDefinedSymbols[symbol_index]).Append(" already existed."); DEBUG(sb);
					}
				}
				PlayerSettings.SetScriptingDefineSymbolsForGroup(
					group,
					string.Join(";", allDefines.ToArray())
				);
			}
		}
		static bool HasScriptingDefineSymbols(ScriptingDefinedSettings setting)
		{
			for (int group_index = 0; group_index < setting.targetGroups.Length; group_index++)
			{
				var group = setting.targetGroups[group_index];
				string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
				List<string> allDefines = definesString.Split(';').ToList();
				for (int symbol_index = 0; symbol_index < setting.scriptingDefinedSymbols.Length; symbol_index++)
				{
					if (!allDefines.Contains(setting.scriptingDefinedSymbols[symbol_index]))
					{
						return false;
					}
				}
			}

			return true;
		}

		const string XR_LOADER_OPENXR_NAME = "UnityEngine.XR.OpenXR.OpenXRLoader";
		internal static bool ViveOpenXRAndroidAssigned { get { return XRPackageMetadataStore.IsLoaderAssigned(XR_LOADER_OPENXR_NAME, BuildTargetGroup.Android); } }

		static PreferenceAvatarAsset m_AssetAvatar = null;
		static void CheckPreferenceAssets()
		{
			if (File.Exists(PreferenceAvatarAsset.AssetPath))
			{
				m_AssetAvatar = AssetDatabase.LoadAssetAtPath(PreferenceAvatarAsset.AssetPath, typeof(PreferenceAvatarAsset)) as PreferenceAvatarAsset;
			}
			else
			{
				string folderPath = PreferenceAvatarAsset.AssetPath.Substring(0, PreferenceAvatarAsset.AssetPath.LastIndexOf('/'));
				DirectoryInfo folder = Directory.CreateDirectory(folderPath);
				sb.Clear().Append("CheckPreferenceAssets() Creates folder: Assets/").Append(folder.Name); DEBUG(sb);

				m_AssetAvatar = ScriptableObject.CreateInstance(typeof(PreferenceAvatarAsset)) as PreferenceAvatarAsset;
				m_AssetAvatar.SupportVrm0 = false;
				m_AssetAvatar.SupportVrm1 = false;

				sb.Clear().Append("CheckPreferenceAssets() Creates the asset: ").Append(PreferenceAvatarAsset.AssetPath); DEBUG(sb);
				AssetDatabase.CreateAsset(m_AssetAvatar, PreferenceAvatarAsset.AssetPath);
			}
		}

		static int checkPreferenceAssetsFrame = 0;
		static void OnUpdate()
		{
			if (!ViveOpenXRAndroidAssigned) { return; }

			checkPreferenceAssetsFrame++;
			checkPreferenceAssetsFrame %= 1200; // 10s
			if (checkPreferenceAssetsFrame != 0) { return; }

			CheckPreferenceAssets();

			if (m_AssetAvatar)
			{
				// Adds the script symbol if VRM0 is imported.
				if (File.Exists(PreferenceAvatarAsset.kVrm0Asset))
				{
					if (!HasScriptingDefineSymbols(m_ScriptDefineSettingVrm0))
					{
						sb.Clear().Append("OnUpdate() Adds m_ScriptDefineSettingVrm0."); DEBUG(sb);
						AddScriptingDefineSymbols(m_ScriptDefineSettingVrm0);
					}
					m_AssetAvatar.SupportVrm0 = true;
				}
				else
				{
					if (HasScriptingDefineSymbols(m_ScriptDefineSettingVrm0))
					{
						sb.Clear().Append("OnUpdate() Removes m_ScriptDefineSettingVrm0."); DEBUG(sb);
						RemoveScriptingDefineSymbols(m_ScriptDefineSettingVrm0);
					}
					m_AssetAvatar.SupportVrm0 = false;
				}

				m_AssetAvatar.SupportVrm1 = File.Exists(PreferenceAvatarAsset.kVrm1Asset);
			}
		}
		#endregion

		#region Preferences
		const string kPreferenceName = "VIVE OpenXR";
		private static GUIContent m_Vrm0Option = new GUIContent("VRM 0", "Avatar format.");
		private static GUIContent m_Vrm1Option = new GUIContent("VRM 1", "Avatar format.");

		internal static void ImportModule(string packagePath, bool interactive = false)
		{
			string target = Path.Combine("Packages/com.htc.upm.vive.openxr/UnityPackages~", packagePath);
			sb.Clear().Append("ImportModule: " + target); DEBUG(sb);
			AssetDatabase.ImportPackage(target, interactive);
		}

		static bool avatarOption = true;
#pragma warning disable 0618
		[PreferenceItem(kPreferenceName)]
#pragma warning restore 0618
		private static void OnPreferencesGUI()
		{
			if (EditorApplication.isCompiling)
			{
				EditorGUILayout.LabelField("Compiling...");
				return;
			}
			if (PackageManagerHelper.isAddingToList)
			{
				EditorGUILayout.LabelField("Installing packages...");
				return;
			}
			if (PackageManagerHelper.isRemovingFromList)
			{
				EditorGUILayout.LabelField("Removing packages...");
				return;
			}

			PackageManagerHelper.PreparePackageList();
			if (PackageManagerHelper.isPreparingList)
			{
				EditorGUILayout.LabelField("Checking Packages...");
				return;
			}

			CheckPreferenceAssets();

			GUIStyle sectionTitleStyle = new GUIStyle(EditorStyles.label);
			sectionTitleStyle.fontSize = 16;
			sectionTitleStyle.richText = true;
			sectionTitleStyle.fontStyle = FontStyle.Bold;

			#region Avatar
			GUILayout.BeginHorizontal();
			GUILayout.Space(10);
			GUILayout.Label("Avatar", sectionTitleStyle);
			GUILayout.EndHorizontal();

			GUIStyle foldoutStyle = EditorStyles.foldout;
			foldoutStyle.fontSize = 14;
			foldoutStyle.fontStyle = FontStyle.Normal;

			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			avatarOption = EditorGUILayout.Foldout(avatarOption, "Supported Format", foldoutStyle);
			GUILayout.EndHorizontal();

			foldoutStyle.fontSize = 12;
			foldoutStyle.fontStyle = FontStyle.Normal;

			if (m_AssetAvatar && avatarOption)
			{
				/// VRM 0
				GUILayout.Space(5);
				GUILayout.BeginHorizontal();
				GUILayout.Space(35);
				if (!m_AssetAvatar.SupportVrm0)
				{
					bool toggled = EditorGUILayout.ToggleLeft(m_Vrm0Option, false, GUILayout.Width(230f));
					if (toggled)
					{
						sb.Clear().Append("OnPreferencesGUI() Adds ").Append(PreferenceAvatarAsset.kVrm0Package); DEBUG(sb);
						ImportModule(PreferenceAvatarAsset.kVrm0Package);
					}
				}
				else
				{
					EditorGUILayout.ToggleLeft(m_Vrm0Option, true, GUILayout.Width(230f));
				}
				GUILayout.EndHorizontal();

				/// VRM 1
				GUILayout.Space(5);
				GUILayout.BeginHorizontal();
				GUILayout.Space(35);
				if (!m_AssetAvatar.SupportVrm1)
				{
					bool toggled = EditorGUILayout.ToggleLeft(m_Vrm1Option, false, GUILayout.Width(230f));
					if (toggled)
					{
						sb.Clear().Append("OnPreferencesGUI() Adds ").Append(PreferenceAvatarAsset.kVrm1Package); DEBUG(sb);
						ImportModule(PreferenceAvatarAsset.kVrm1Package);
					}
				}
				else
				{
					EditorGUILayout.ToggleLeft(m_Vrm1Option, true, GUILayout.Width(230f));
				}
				GUILayout.EndHorizontal();
			}
			#endregion
		}
		#endregion
	}
}
#endif