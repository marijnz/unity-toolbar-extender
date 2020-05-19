using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace UnityToolbarExtender
{
	public static class ToolbarCallback
	{
		static Type m_toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
		static Type m_guiViewType = typeof(Editor).Assembly.GetType("UnityEditor.GUIView");
		static PropertyInfo m_viewVisualTree = m_guiViewType.GetProperty("visualTree",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		static FieldInfo m_imguiContainerOnGui = typeof(IMGUIContainer).GetField("m_OnGUIHandler",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		static ScriptableObject m_currentToolbar;

		/// <summary>
		/// Callback for toolbar OnGUI method.
		/// </summary>
		public static Action OnToolbarGUI;

		static ToolbarCallback()
		{
			EditorApplication.update -= OnUpdate;
			EditorApplication.update += OnUpdate;
		}

		static void OnUpdate()
		{
			// Relying on the fact that toolbar is ScriptableObject and gets deleted when layout changes
			if (m_currentToolbar == null)
			{
				// Find toolbar
				var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
				m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject) toolbars[0] : null;
				if (m_currentToolbar != null)
				{
					// Get it's visual tree
					var visualTree = (VisualElement) m_viewVisualTree.GetValue(m_currentToolbar, null);

					// Get first child which 'happens' to be toolbar IMGUIContainer
					var container = (IMGUIContainer) visualTree[0];

					// (Re)attach handler
					var handler = (Action) m_imguiContainerOnGui.GetValue(container);
					handler -= OnGUI;
					handler += OnGUI;
					m_imguiContainerOnGui.SetValue(container, handler);
				}
			}
		}

		static void OnGUI()
		{
			var handler = OnToolbarGUI;
			if (handler != null) handler();
		}
	}
}
