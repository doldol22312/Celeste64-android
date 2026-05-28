using Android.App;
using Android.Content.PM;
using Android.Runtime;

namespace Celeste64.Android;

[Activity(
	Label = Celeste64.Game.GameTitle,
	MainLauncher = true,
	ScreenOrientation = ScreenOrientation.Landscape,
	Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
	ConfigurationChanges =
		ConfigChanges.Keyboard |
		ConfigChanges.KeyboardHidden |
		ConfigChanges.Orientation |
		ConfigChanges.ScreenLayout |
		ConfigChanges.ScreenSize |
		ConfigChanges.SmallestScreenSize)]
public sealed class MainActivity : Org.Libsdl.App.SDLActivity
{
	public MainActivity()
	{
	}

	public MainActivity(IntPtr handle, JniHandleOwnership transfer)
		: base(handle, transfer)
	{
	}

	protected override void RunMain()
		=> AndroidHost.Run(this);
}
