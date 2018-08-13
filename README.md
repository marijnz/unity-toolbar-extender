# unity-toolbar-extender

Extend the Unity Toolbar with your own UI code. Please note that it's **super hacky** as the code is heavily relying on using reflection to access Unity's internal code. It might not work anymore with a new Unity update.

Add buttons to quickly access scenes, add sliders, toggles, anything. 

![Imgur](https://i.imgur.com/rYqe8bN.png)

## How to
This example code is shown in action in the gif below. Just copy the class, change it and make as many as you like. You can remove the _Example_ folder from the project. All you need is _ExtendedToolbarWindow.cs_.
```
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
```


![Imgur](https://i.imgur.com/4adMePz.gif)
