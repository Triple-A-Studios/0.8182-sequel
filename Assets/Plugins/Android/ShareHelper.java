package com.tripleastudios.opoint8182.share;

import android.app.Activity;
import android.content.ClipData;
import android.content.Context;
import android.content.Intent;
import android.net.Uri;
import android.util.Log;

import java.io.File;

/**
 * Opens the Android share sheet with a PNG and a line of text (Daily play structure, Pass 8b). The image is a file in the app's
 * cache folder, handed to the other app through {@link ShareFileProvider}: no storage permission is needed, and nothing is saved
 * to the gallery (the share sheet offers Save itself). Called from C# (AndroidShareSheet.cs).
 */
public final class ShareHelper
{
	private static final String TAG = "Opoint8182Share";
	static final String DIR_NAME = "share";

	private ShareHelper()
	{
	}

	/** The provider's authority, the same string the manifest declares. */
	static String authority(Context context)
	{
		return context.getPackageName() + ".share";
	}

	/** The folder the provider serves from. */
	static File shareDir(Context context)
	{
		return new File(context.getCacheDir(), DIR_NAME);
	}

	/** Creates the share folder (empty: earlier cards are deleted) and returns its path for C# to write the PNG into. */
	public static String prepareShareDir(Activity activity)
	{
		File dir = shareDir(activity);
		if (dir.isDirectory())
		{
			File[] old = dir.listFiles();
			if (old != null)
			{
				for (File file : old) file.delete();
			}
		}
		else
		{
			dir.mkdirs();
		}

		return dir.getAbsolutePath();
	}

	/** Shows the share sheet for the PNG at {@code path} (a file inside the share folder). Returns false when it could not be started. */
	public static boolean share(final Activity activity, String path, String text, String title)
	{
		try
		{
			File file = new File(path);
			if (!file.isFile()) return false;

			Uri uri = Uri.parse("content://" + authority(activity) + "/" + DIR_NAME + "/" + Uri.encode(file.getName()));

			Intent send = new Intent(Intent.ACTION_SEND);
			send.setType("image/png");
			send.putExtra(Intent.EXTRA_STREAM, uri);
			send.putExtra(Intent.EXTRA_TEXT, text);
			send.setClipData(ClipData.newRawUri("", uri));
			send.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);

			final Intent chooser = Intent.createChooser(send, title);
			activity.runOnUiThread(new Runnable()
			{
				@Override
				public void run()
				{
					try
					{
						activity.startActivity(chooser);
					}
					catch (Exception e)
					{
						Log.w(TAG, "startActivity failed", e);
					}
				}
			});
			return true;
		}
		catch (Exception e)
		{
			Log.w(TAG, "share failed", e);
			return false;
		}
	}
}
