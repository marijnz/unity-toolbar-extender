using ToolbarExtender;
using UnityEditor;
using UnityEngine;

namespace ToolbarExtenderExample
{
	[InitializeOnLoad]
	public class LeftButtonToolbarWindow : ExtendedToolbarWindow
	{
		static LeftButtonToolbarWindow()
		{
			RegisterToolbarWindow<LeftButtonToolbarWindow>(horizontalOffset:-90);
		}

		protected override void OnGUI()
		{
			base.OnGUI();

			var buttonRect = new Rect(0, 0, position.width, position.height);
			buttonRect.y = 4;

			if(GUI.Button(buttonRect, "1", Styles.commandButtonStyle))
			{
				SceneHelper.StartScene("Assets/ToolbarExtender/Example/Scenes/Scene1.unity");
			}
		}
	}

	[InitializeOnLoad]
	public class RightButtonToolbarWindow : ExtendedToolbarWindow
	{
		static RightButtonToolbarWindow()
		{
			RegisterToolbarWindow<RightButtonToolbarWindow>(horizontalOffset:90);
		}

		protected override void OnGUI()
		{
			base.OnGUI();

			var buttonRect = new Rect(0, 0, position.width, position.height);
			buttonRect.y = 4;

			if(GUI.Button(buttonRect, "2", Styles.commandButtonStyle))
			{
				SceneHelper.StartScene("Assets/ToolbarExtender/Example/Scenes/Scene2.unity");
			}
		}
	}
}