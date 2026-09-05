// Copyright HTC Corporation All Rights Reserved.

using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public static class PackageManagerHelper
{
	private static bool s_wasPreparing;
	private static bool m_wasAdded;
	private static bool s_wasRemoved;
	private static ListRequest m_listRequest;
	private static AddRequest m_addRequest;
	private static RemoveRequest m_removeRequest;
	private static string s_fallbackIdentifier;

	public static bool isPreparingList
	{
		get
		{
			if (m_listRequest == null) { return s_wasPreparing = true; }

			switch (m_listRequest.Status)
			{
				case StatusCode.InProgress:
					return s_wasPreparing = true;
				case StatusCode.Failure:
					if (!s_wasPreparing)
					{
						Debug.LogError("Something wrong when adding package to list. error:" + m_listRequest.Error.errorCode + "(" + m_listRequest.Error.message + ")");
					}
					break;
				case StatusCode.Success:
					break;
			}

			return s_wasPreparing = false;
		}
	}

	public static bool isAddingToList
	{
		get
		{
			if (m_addRequest == null) { return m_wasAdded = false; }

			switch (m_addRequest.Status)
			{
				case StatusCode.InProgress:
					return m_wasAdded = true;
				case StatusCode.Failure:
					if (!m_wasAdded)
					{
						AddRequest request = m_addRequest;
						m_addRequest = null;
						if (string.IsNullOrEmpty(s_fallbackIdentifier))
						{
							Debug.LogError("Something wrong when adding package to list. error:" + request.Error.errorCode + "(" + request.Error.message + ")");
						}
						else
						{
							Debug.Log("Failed to install package: \"" + request.Error.message + "\". Retry with fallback identifier \"" + s_fallbackIdentifier + "\"");
							AddToPackageList(s_fallbackIdentifier);
						}

						s_fallbackIdentifier = null;
					}
					break;
				case StatusCode.Success:
					if (!m_wasAdded)
					{
						m_addRequest = null;
						s_fallbackIdentifier = null;
						ResetPackageList();
					}
					break;
			}

			return m_wasAdded = false;
		}
	}

	public static bool isRemovingFromList
	{
		get
		{
			if (m_removeRequest == null) { return s_wasRemoved = false; }

			switch (m_removeRequest.Status)
			{
				case StatusCode.InProgress:
					return s_wasRemoved = true;
				case StatusCode.Failure:
					if (!s_wasRemoved)
					{
						if (m_removeRequest != null) { Debug.LogError("Something wrong when removing package from list. error:" + m_removeRequest.Error.errorCode + "(" + m_removeRequest.Error.message + ")"); }
						var request = m_removeRequest;
						m_removeRequest = null;
					}
					break;
				case StatusCode.Success:
					if (!s_wasRemoved)
					{
						m_removeRequest = null;
						ResetPackageList();
					}
					break;
			}

			return s_wasRemoved = false;
		}
	}

	public static void PreparePackageList()
	{
		if (m_listRequest != null) { return; }
		m_listRequest = Client.List(true, true);
	}

	public static void ResetPackageList()
	{
		s_wasPreparing = false;
		m_listRequest = null;
	}

	public static bool IsPackageInList(string name, out UnityEditor.PackageManager.PackageInfo packageInfo)
	{
		packageInfo = null;
		if (m_listRequest == null || m_listRequest.Result == null) return false;

		foreach (var package in m_listRequest.Result)
		{
			if (package.name.Equals(name))
			{
				packageInfo = package;
				return true;
			}
		}
		return false;
	}

	public static void AddToPackageList(string identifier, string fallbackIdentifier = null)
	{
		Debug.Assert(m_addRequest == null);

		m_addRequest = Client.Add(identifier);
		s_fallbackIdentifier = fallbackIdentifier;
	}

	public static void RemovePackage(string identifier)
	{
		Debug.Assert(m_removeRequest == null);

		m_removeRequest = Client.Remove(identifier);
	}

	public static PackageCollection GetPackageList()
	{
		if (m_listRequest == null || m_listRequest.Result == null)
		{
			return null;
		}

		return m_listRequest.Result;
	}
}
