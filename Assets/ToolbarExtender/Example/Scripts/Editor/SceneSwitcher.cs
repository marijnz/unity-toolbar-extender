using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityToolbarExtender.Examples
{
	static class ToolbarStyles
	{
		public static GUIStyle commandButtonStyle;
		public static GUIStyle appToolbar;

		static ToolbarStyles()
		{
			commandButtonStyle = new GUIStyle("Command");
			commandButtonStyle.fontSize = 16;
			commandButtonStyle.alignment = TextAnchor.MiddleCenter;
			commandButtonStyle.imagePosition = ImagePosition.ImageAbove;
			commandButtonStyle.fontStyle = FontStyle.Bold;
		}
	}

	[InitializeOnLoad]
	public class SceneSwitchLeftButton
	{
		static SceneSwitchLeftButton()
		{
			ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
		}

		static void OnToolbarGUI()
		{
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("1", ToolbarStyles.commandButtonStyle))
			{
				SceneHelper.StartScene("Assets/ToolbarExtender/Example/Scenes/Scene1.unity");
			}
		}
	}

	[InitializeOnLoad]
	public class SceneSwitchRightButton
	{
		static SceneSwitchRightButton()
		{
			ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
		}

		static void OnToolbarGUI()
		{
			if(GUILayout.Button("2", ToolbarStyles.commandButtonStyle))
			{
				SceneHelper.StartScene("Assets/ToolbarExtender/Example/Scenes/Scene2.unity");
			}
			GUILayout.FlexibleSpace();
		}
	}

	static class SceneHelper
	{
		static string sceneToOpen;

		public static void StartScene(string scene)
		{
			if(EditorApplication.isPlaying)
			{
				EditorApplication.isPlaying = false;
			}

			sceneToOpen = scene;
			EditorApplication.update += OnUpdate;
		}

		static void OnUpdate()
		{
			if (sceneToOpen == null ||
			    EditorApplication.isPlaying || EditorApplication.isPaused ||
			    EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
			{
				return;
			}

			EditorApplication.update -= OnUpdate;

			if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				EditorSceneManager.OpenScene(sceneToOpen);
				EditorApplication.isPlaying = true;
			}
			sceneToOpen = null;
		}
	}
}