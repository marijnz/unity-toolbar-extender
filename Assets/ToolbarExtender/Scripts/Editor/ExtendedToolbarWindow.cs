using System;
using System.Collections.Generic;
using System.Reflection;
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

						var noShadow =  Enum.ToObject(GetUnityEditorType("UnityEditor.ShowMode"), 3);
						var showWithModeMethod = GetMethod(typeof(EditorWindow), "ShowWithMode");

						showWithModeMethod.Invoke(instance.window, new[] { noShadow });
					}

					instance.window.horizontalOffset = instance.horizontalOffset;
					instance.window.width = instance.width;
				}
			}
		}

		#endregion

		#region EditorWindow implementation

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

		void OnEnable()
		{
		//	EditorApplication.quitting += OnQuitting;
			EditorApplication.update += OnUpdate;
		}

		void OnDisable()
		{
			//EditorApplication.quitting -= OnQuitting;
			EditorApplication.update -= OnUpdate;
		}

		void OnUpdate()
		{
			UpdatePosition();
			Repaint();
		}

		void OnQuitting()
		{
			// Destroy when quitting Unity, to avoid warnings on the next opening of Unity.
			// This means any serialized state will be gone when Unity is quit.
		//	EditorApplication.quitting -= OnQuitting;
		//	DestroyImmediate(this);
		}

		object mainWindow;

		void UpdatePosition()
		{
			var containerWindowType = GetUnityEditorType("UnityEditor.ContainerWindow");

			if(mainWindow == null)
			{
				Initialize();
			}

			var windowPosition = (Rect) GetProperty(containerWindowType, "position").GetValue(mainWindow, null);

			var rect = new Rect
			{
				width = width,
				height = 30,
				x = windowPosition.x + windowPosition.width / 2 + horizontalOffset - 18,
				y = windowPosition.y
			};
			position = rect;
		}

		void Initialize()
		{
			var containerWindowType = GetUnityEditorType("UnityEditor.ContainerWindow");
			var showModeProperty = GetProperty(containerWindowType, "showMode");

			foreach (UnityEngine.Object o in Resources.FindObjectsOfTypeAll(containerWindowType))
			{
				if( (int) showModeProperty.GetValue(o, null) == 4)
				{
					mainWindow = o;
					break;
				}
			}
		}

		#endregion

		#region Reflection helpers

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