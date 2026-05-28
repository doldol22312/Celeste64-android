using Android.App;
using Android.Runtime;
using Java.Interop;

namespace Org.Libsdl.App;

[Register("org/libsdl/app/SDLActivity", DoNotGenerateAcw = true)]
public class SDLActivity : Activity
{
	public SDLActivity()
	{
	}

	protected SDLActivity(IntPtr handle, JniHandleOwnership transfer)
		: base(handle, transfer)
	{
	}

	private static Delegate? cb_runMain;

	private delegate void JniMarshalPPV(IntPtr jnienv, IntPtr native__this);

	private static Delegate GetRunMainHandler()
		=> cb_runMain ??= JNINativeWrapper.CreateDelegate(new JniMarshalPPV(n_RunMain));

	private static void n_RunMain(IntPtr jnienv, IntPtr native__this)
	{
		var self = Java.Lang.Object.GetObject<SDLActivity>(jnienv, native__this, JniHandleOwnership.DoNotTransfer);
		self?.RunMain();
	}

	[Register("runMain", "()V", "GetRunMainHandler")]
	protected virtual void RunMain()
	{
	}
}
