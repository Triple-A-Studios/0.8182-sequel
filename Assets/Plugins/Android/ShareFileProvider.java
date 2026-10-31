package com.tripleastudios.opoint8182.share;

import android.content.ContentProvider;
import android.content.ContentValues;
import android.database.Cursor;
import android.database.MatrixCursor;
import android.net.Uri;
import android.os.ParcelFileDescriptor;
import android.provider.OpenableColumns;

import java.io.File;
import java.io.FileNotFoundException;
import java.io.IOException;

/**
 * Serves the share card PNG to the app the player picks in the share sheet (Daily play structure, Pass 8b). Read only, not exported
 * (access comes from the URI permission the share intent grants), and limited to files directly inside the share folder, so nothing
 * else of the app can be reached through it. A tiny provider of our own instead of androidx FileProvider so the build needs no
 * extra library.
 */
public class ShareFileProvider extends ContentProvider
{
	@Override
	public boolean onCreate()
	{
		return true;
	}

	@Override
	public String getType(Uri uri)
	{
		return "image/png";
	}

	@Override
	public Cursor query(Uri uri, String[] projection, String selection, String[] selectionArgs, String sortOrder)
	{
		File file = resolve(uri);
		if (file == null) return null;

		String[] columns = projection != null ? projection : new String[] { OpenableColumns.DISPLAY_NAME, OpenableColumns.SIZE };
		Object[] row = new Object[columns.length];
		for (int i = 0; i < columns.length; i++)
		{
			if (OpenableColumns.DISPLAY_NAME.equals(columns[i])) row[i] = file.getName();
			else if (OpenableColumns.SIZE.equals(columns[i])) row[i] = file.length();
		}

		MatrixCursor cursor = new MatrixCursor(columns, 1);
		cursor.addRow(row);
		return cursor;
	}

	@Override
	public ParcelFileDescriptor openFile(Uri uri, String mode) throws FileNotFoundException
	{
		File file = resolve(uri);
		if (file == null) throw new FileNotFoundException(uri.toString());

		return ParcelFileDescriptor.open(file, ParcelFileDescriptor.MODE_READ_ONLY);
	}

	@Override
	public Uri insert(Uri uri, ContentValues values)
	{
		throw new UnsupportedOperationException("read only");
	}

	@Override
	public int delete(Uri uri, String selection, String[] selectionArgs)
	{
		throw new UnsupportedOperationException("read only");
	}

	@Override
	public int update(Uri uri, ContentValues values, String selection, String[] selectionArgs)
	{
		throw new UnsupportedOperationException("read only");
	}

	/** The file the URI names, or null when it is not a file directly inside the share folder. */
	private File resolve(Uri uri)
	{
		try
		{
			String name = uri.getLastPathSegment();
			if (name == null || getContext() == null) return null;

			File dir = ShareHelper.shareDir(getContext()).getCanonicalFile();
			File file = new File(dir, name).getCanonicalFile();
			return dir.equals(file.getParentFile()) && file.isFile() ? file : null;
		}
		catch (IOException e)
		{
			return null;
		}
	}
}
