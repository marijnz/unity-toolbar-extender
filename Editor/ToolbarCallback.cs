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
#if UNITY_2020_1_OR_NEWER
		static Type m_iWindowBackendType = typeof(Editor).Assembly.GetType("UnityEditor.IWindowBackend");
		static PropertyInfo m_windowBackend = m_guiViewType.GetProperty("windowBackend",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		static PropertyInfo m_viewVisualTree = m_iWindowBackendType.GetProperty("visualTree",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#else
		static PropertyInfo m_viewVisualTree = m_guiViewType.GetProperty("visualTree",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#endif
		static FieldInfo m_imguiContainerOnGui = typeof(IMGUIContainer).GetField("m_OnGUIHandler",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		static ScriptableObject m_currentToolbar;

		/// <summary>
		/// Callback for toolbar OnGUI method.
		/// </summary>
		public static Action OnToolbarGUI;
		public static Action OnToolbarGUILeft;
		public static Action OnToolbarGUIRight;

#if UNITY_6000_3_OR_NEWER
		// bypass Unity 6.3+ warnings about unsupported elements by using a weirder callback reflection method
		// see https://github.com/marijnz/unity-toolbar-extender/issues/39 for discussion
	
		private static int setupAttempts;
		private const int MaxSetupAttempts = 200;

		static ToolbarCallback()
		{
			EditorApplication.update -= Initialize;
			EditorApplication.update += Initialize;
		}

		private static void Initialize()
		{
			setupAttempts++;

			Type mainToolbarWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.MainToolbarWindow");

			if (mainToolbarWindowType == null)
			{
				EditorApplication.update -= Initialize;

				return;
			}

			UnityEngine.Object[] toolbars = Resources.FindObjectsOfTypeAll(mainToolbarWindowType);

			if (toolbars.Length == 0)
			{
				if (setupAttempts > MaxSetupAttempts)
				{
						Debug.LogWarning("[CustomToolbar] Could not find MainToolbarWindow instance after multiple attempts. Aborting.");
						EditorApplication.update -= Initialize;
				}

				return;
			}

			var toolbarWindow = (EditorWindow)toolbars[0];
			VisualElement root = toolbarWindow.rootVisualElement;

			if (root == null)
			{
				EditorApplication.update -= Initialize;

				return;
			}

			VisualElement middleContainer = root.Q(className: "unity-overlay-container__middle-container");

			if (middleContainer == null)
			{
				if (setupAttempts > MaxSetupAttempts)
				{
						Debug.LogWarning("[CustomToolbar] Found MainToolbarWindow, but its middle-container is not ready. Aborting.");
						EditorApplication.update -= Initialize;
				}

				return;
			}

			VisualElement parentContainer = middleContainer.parent;

			if (parentContainer == null)
			{
				EditorApplication.update -= Initialize;

				return;
			}

			var leftDock = new VisualElement
			{
						name = "CustomToolbarLeft",
						style =
						{
									flexGrow = 1,
									flexDirection = FlexDirection.Row,
									flexBasis = 0,
									justifyContent = Justify.FlexEnd,
									alignItems = Align.Center
						}
			};

			var rightDock = new VisualElement
			{
						name = "CustomToolbarRight",
						style =
						{
									flexGrow = 1,
									flexDirection = FlexDirection.Row,
									flexBasis = 0,
									justifyContent = Justify.FlexStart,
									alignItems = Align.Center
						}
			};

			parentContainer.Insert(parentContainer.IndexOf(middleContainer), leftDock);
			parentContainer.Insert(parentContainer.IndexOf(middleContainer) + 1, rightDock);

			leftDock.Add(new IMGUIContainer(static () => OnToolbarGUILeft?.Invoke()));
			rightDock.Add(new IMGUIContainer(static () => OnToolbarGUIRight?.Invoke()));

			EditorApplication.update -= Initialize;
		}

		// end Unity 6.3 workaround
#else		
		
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
#if UNITY_2021_1_OR_NEWER
					var root = m_currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
					var rawRoot = root.GetValue(m_currentToolbar);
					var mRoot = rawRoot as VisualElement;
					RegisterCallback("ToolbarZoneLeftAlign", OnToolbarGUILeft);
					RegisterCallback("ToolbarZoneRightAlign", OnToolbarGUIRight);

					void RegisterCallback(string root, Action cb) {
						var toolbarZone = mRoot.Q(root);

						var parent = new VisualElement()
						{
							style = {
								flexGrow = 1,
								flexDirection = FlexDirection.Row,
							}
						};
						var container = new IMGUIContainer();
						container.style.flexGrow = 1;
						container.onGUIHandler += () => { 
							cb?.Invoke();
						}; 
						parent.Add(container);
						toolbarZone.Add(parent);
					}

					// Handle root visual element being detached from m_currentToolbar when changing between displays of different DPI
					mRoot.RegisterCallback<DetachFromPanelEvent>(_ => m_currentToolbar = null);
#else
#if UNITY_2020_1_OR_NEWER
					var windowBackend = m_windowBackend.GetValue(m_currentToolbar);

					// Get it's visual tree
					var visualTree = (VisualElement) m_viewVisualTree.GetValue(windowBackend, null);
#else
					// Get it's visual tree
					var visualTree = (VisualElement) m_viewVisualTree.GetValue(m_currentToolbar, null);
#endif

					// Get first child which 'happens' to be toolbar IMGUIContainer
					var container = (IMGUIContainer) visualTree[0];

					// (Re)attach handler
					var handler = (Action) m_imguiContainerOnGui.GetValue(container);
					handler -= OnGUI;
					handler += OnGUI;
					m_imguiContainerOnGui.SetValue(container, handler);
					
#endif
				}
			}
		}

		static void OnGUI()
		{
			var handler = OnToolbarGUI;
			if (handler != null) handler();
		}

#endif
	}
}
