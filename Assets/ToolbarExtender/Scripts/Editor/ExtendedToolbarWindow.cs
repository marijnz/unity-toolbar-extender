using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ToolbarExtender
{
	public class ExtendedToolbarWindow : EditorWindow
	{
		#region EditorWindow management

		protected static class Styles
		{
			public static GUIStyle commandButtonStyle;
			public static GUIStyle appToolbar;

			static Styles()
			{
				commandButtonStyle = new GUIStyle("Command");
				commandButtonStyle.fontSize = 16;
				commandButtonStyle.alignment = TextAnchor.MiddleCenter;
				commandButtonStyle.imagePosition = ImagePosition.ImageAbove;
				commandButtonStyle.fontStyle = FontStyle.Bold;
				appToolbar = new GUIStyle("AppToolbar");
			}
		}

		class ToolbarInstance
		{
			public Type type;
			public ExtendedToolbarWindow window;
			public float horizontalOffset;
			public float width;
		}

		static readonly BindingFlags BindingFlags = (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

		static List<ToolbarInstance> toolbarInstances;

		protected static void RegisterToolbarWindow<T>(float horizontalOffset, float width) where T : ExtendedToolbarWindow
		{
			// Ensure there's an update loop
			if(toolbarInstances == null)
			{
				toolbarInstances = new List<ToolbarInstance>();
				EditorApplication.update += EnsureWindowExistsUpdate;
			}

			toolbarInstances.Add(new ToolbarInstance
			{
				type = typeof(T),
				horizontalOffset = horizontalOffset,
				width = width
			});
		}

		static void EnsureWindowExistsUpdate()
		{
			foreach (var instance in toolbarInstances)
			{
				if(instance.window == null)
				{
					var windows = Resources.FindObjectsOfTypeAll(instance.type);

					if(windows.Length > 1)
					{
						Debug.LogError("Found more than one instance of " + instance.type);
					}
					else if (windows.Length == 1)
					{
						instance.window = (ExtendedToolbarWindow) windows[0];
					}

					if(instance.window == null)
					{
						instance.window = (ExtendedToolbarWindow) CreateInstance(instance.type);
						instance.window.position = Rect.zero;
						instance.window.ShowPopup();
					}

					instance.window.horizontalOffset = instance.horizontalOffset;
					instance.window.width = instance.width;
				}
			}
		}

		#endregion

		#region EditorWindow implementation

		[SerializeField] bool initialized;
		float horizontalOffset;
		float width;

		protected virtual void OnGUI()
		{
			// Draw background that's of the same color as the toolbar
			if(Event.current.type == EventType.Repaint)
			{
				Styles.appToolbar.Draw(new Rect(0, 0, position.width, position.height), false, false, false, false);
			}
		}

		void OnInspectorUpdate()
		{
			// Initialize after 1 "frame"
			if(!initialized)
			{
				initialized = true;
				Initialize();
			}

			UpdatePosition();
		}

		void OnEnable()
		{
			EditorApplication.quitting += OnQuitting;
		}

		void OnDisable()
		{
			EditorApplication.quitting -= OnQuitting;
		}

		void OnQuitting()
		{
			// Destroy when quitting Unity, to avoid warnings on the next opening of Unity.
			// This means any serialized state will be gone when Unity is quit.
			EditorApplication.quitting -= OnQuitting;
			DestroyImmediate(this);
		}

		void Initialize()
		{
			initialized = true;
			var parent = GetField(GetType(), "m_Parent").GetValue(this);

			var toolbarRef = GetToolbar();

			var parentWindowProp = GetProperty(parent.GetType(), "window");
			var parentWindow = (UnityEngine.Object) parentWindowProp.GetValue(parent, new object[0]);

			// Set the root view to null so our window doesn't get cleared when closing it
			var r = GetField(parentWindow.GetType(), "m_RootView");
			r.SetValue(parentWindow, null);

			// Close (old) container window
			var m = GetMethod(parentWindow.GetType(), "Close");
			m.Invoke(parentWindow, new object[0]);

			// Child to toolbar
			var classType = GetUnityEditorType("UnityEditor.View");
			var addChild = GetMethod(toolbarRef.GetType(), "AddChild", classType);
			addChild.Invoke(toolbarRef, new[] { parent });
		}

		void UpdatePosition()
		{
			// Get parent position
			var toolbarRef = GetToolbar();
			var windowProperty = GetProperty(toolbarRef.GetType(), "window");
			var window = windowProperty.GetValue(toolbarRef, new object[0]);

			var positionProperty = GetProperty(window.GetType(), "position");
			var windowPosition = (Rect) positionProperty.GetValue(window, null);

			var rect = new Rect();
			rect.width = width;
			rect.height = 30;
			rect.x = windowPosition.width / 2 + horizontalOffset - 18;
			rect.y = 1;

			// Set position
			var parent = GetField(GetType(), "m_Parent").GetValue(this);
			var method = GetMethod(parent.GetType(), "SetPosition");
			method.Invoke(parent, new object[] { rect });
		}

		#endregion

		#region Reflection helpers

		static object GetToolbar()
		{
			var classType = GetUnityEditorType("UnityEditor.Toolbar");
			return GetField(classType, "get").GetValue(null);
		}

		static Type GetUnityEditorType(string name)
		{
			var design = typeof(EditorWindow).Assembly;
			var classType = design.GetType(name);
			return classType;
		}

		static FieldInfo GetField(Type type, string fieldName)
		{
			var fieldInfo = type.GetField(fieldName, BindingFlags);
			Debug.Assert(fieldInfo != null, "Expected " + fieldName + " of " + type + " not to be null");
			return fieldInfo;
		}

		static MethodInfo GetMethod(Type type, string methodName)
		{
			var methodInfo = type.GetMethod(methodName, BindingFlags);
			Debug.Assert(methodInfo != null, "Expected " + methodName + " of " + type + " not to be null");
			return methodInfo;
		}

		static MethodInfo GetMethod(Type type, string methodName, params Type[] parameterTypes)
		{
			var methodInfo = type.GetMethod(methodName, BindingFlags, null, parameterTypes, null);
			Debug.Assert(methodInfo != null, "Expected " + methodName + " of " + type + " not to be null");
			return methodInfo;
		}

		static PropertyInfo GetProperty(Type type, string propertyName)
		{
			var propertyInfo = type.GetProperty(propertyName, BindingFlags);
			Debug.Assert(propertyInfo != null, "Expected " + propertyName + " of " + type + " not to be null");
			return propertyInfo;
		}

		#endregion
	}
}