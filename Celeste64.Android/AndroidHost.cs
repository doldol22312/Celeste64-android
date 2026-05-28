using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Android.Content;
using Android.Content.Res;
using Foster.Framework;
using AndroidLog = Android.Util.Log;

namespace Celeste64.Android;

internal static class AndroidHost
{
	private const string Tag = "Celeste64";
	private const string ContentMarkerName = ".celeste64-content-version";
	private static bool errorHandlerRegistered;

	[DllImport("FosterPlatform", EntryPoint = "FosterAndroidSetupThread")]
	private static extern int FosterAndroidSetupThread();

	public static void Run(Context context)
	{
		AndroidLog.Info(Tag, "RunMain entered");
		RegisterErrorHandler(context);
		SetInvariantCulture();

		try
		{
			AndroidLog.Info(Tag, "Preparing SDL Android thread");
			if (FosterAndroidSetupThread() == 0)
				AndroidLog.Warn(Tag, "SDL Android thread setup reported failure");

			AndroidLog.Info(Tag, "Extracting content");
			var contentPath = ExtractContent(context);
			AndroidLog.Info(Tag, $"Content path: {contentPath}");
			Assets.SetContentPath(contentPath);

			AndroidLog.Info(Tag, "Starting Foster game loop");
			App.Run<Game>(Game.GamePath, 1280, 720, fullscreen: true);
			AndroidLog.Warn(Tag, "Foster game loop returned");
		}
		catch (Exception e)
		{
			HandleError(context, e);
		}
	}

	private static void RegisterErrorHandler(Context context)
	{
		if (errorHandlerRegistered)
			return;

		errorHandlerRegistered = true;
		AppDomain.CurrentDomain.UnhandledException += (_, e) =>
		{
			if (e.ExceptionObject is Exception exception)
				HandleError(context, exception);
		};
	}

	private static void SetInvariantCulture()
	{
		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
	}

	private static string ExtractContent(Context context)
	{
		var filesDir = context.FilesDir?.AbsolutePath;
		if (string.IsNullOrEmpty(filesDir))
			throw new InvalidOperationException("Android files directory is unavailable");

		var contentRoot = Path.GetFullPath(Path.Combine(filesDir, Assets.AssetFolder));
		VerifyExtractPath(filesDir, contentRoot);

		var markerPath = Path.Combine(contentRoot, ContentMarkerName);
		var marker = GetContentMarker(context);
		if (File.Exists(markerPath) && File.ReadAllText(markerPath) == marker)
		{
			AndroidLog.Info(Tag, "Content already extracted");
			return contentRoot;
		}

		if (Directory.Exists(contentRoot))
			Directory.Delete(contentRoot, recursive: true);

		Directory.CreateDirectory(contentRoot);
		CopyAssetDirectory(context.Assets!, Assets.AssetFolder, contentRoot);
		File.WriteAllText(markerPath, marker);
		return contentRoot;
	}

	private static void VerifyExtractPath(string filesDir, string contentRoot)
	{
		var root = Path.GetFullPath(filesDir)
			.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

		if (!contentRoot.StartsWith(root, StringComparison.Ordinal))
			throw new InvalidOperationException($"Refusing to extract content outside app storage: {contentRoot}");
	}

	private static string GetContentMarker(Context context)
	{
		try
		{
#pragma warning disable CA1422
			var packageName = context.PackageName ?? string.Empty;
			var packageInfo = context.PackageManager?.GetPackageInfo(packageName, 0);
#pragma warning restore CA1422
			return $"{Game.VersionString}:{packageInfo?.LastUpdateTime ?? 0}";
		}
		catch
		{
			return Game.VersionString;
		}
	}

	private static void CopyAssetDirectory(AssetManager assets, string assetPath, string destination)
	{
		var children = assets.List(assetPath) ?? [];
		if (children.Length == 0)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
			using var input = assets.Open(assetPath);
			using var output = File.Create(destination);
			input.CopyTo(output);
			return;
		}

		Directory.CreateDirectory(destination);
		foreach (var child in children)
			CopyAssetDirectory(assets, $"{assetPath}/{child}", Path.Combine(destination, child));
	}

	private static void HandleError(Context context, Exception e)
	{
		var text = BuildErrorLog(e);
		AndroidLog.Error(Tag, text);

		try
		{
			var filesDir = context.FilesDir?.AbsolutePath;
			if (!string.IsNullOrEmpty(filesDir))
				File.WriteAllText(Path.Combine(filesDir, "ErrorLog.txt"), text);
		}
		catch (Exception writeError)
		{
			AndroidLog.Error(Tag, writeError.ToString());
		}
	}

	private static string BuildErrorLog(Exception e)
	{
		StringBuilder error = new();
		error.AppendLine($"Celeste 64 {Game.VersionString}");
		error.AppendLine($"Error Log ({DateTime.Now})");
		error.AppendLine("Call Stack:");
		error.AppendLine(e.ToString());
		error.AppendLine("Game Output:");

		lock (Foster.Framework.Log.Logs)
			error.AppendLine(Foster.Framework.Log.Logs.ToString());

		return error.ToString();
	}
}
