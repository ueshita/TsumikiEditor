using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class Clipboard
{
	public static void SetTextData(string data)
	{
		IntPtr hwnd = GetActiveWindow();
		if (OpenClipboard(hwnd) == 0) {
			return;
		}

		EmptyClipboard();

		IntPtr globalMem = Marshal.StringToHGlobalUni(data);
		SetClipboardData(13, globalMem);

		CloseClipboard();
	}

	public static string GetTextData()
	{
		IntPtr hwnd = GetActiveWindow();
		if (OpenClipboard(hwnd) == 0) {
			return null;
		}
		
		IntPtr globalMem = GetClipboardData(13);
		if (globalMem == IntPtr.Zero) {
			CloseClipboard();
			return null;
		}

		IntPtr memPtr = GlobalLock(globalMem);
		string data = Marshal.PtrToStringUni(memPtr);
		GlobalUnlock(globalMem);

		CloseClipboard();

		return data;
	}

	#region Native functions
	
	[DllImport("kernel32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
	private static extern IntPtr GlobalAlloc(uint uFlags , uint dwBytes);
	
	[DllImport("kernel32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
	private static extern uint GlobalSize(IntPtr hMem);
	
	[DllImport("kernel32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
	private static extern IntPtr GlobalFree(IntPtr hMem);
	
	[DllImport("kernel32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
	private static extern IntPtr GlobalLock(IntPtr hMem);
	
	[DllImport("kernel32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
	private static extern uint GlobalUnlock(IntPtr hMem);
	
	[DllImport("user32.dll")]
	public static extern IntPtr GetActiveWindow();

	[DllImport("user32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
	private static extern uint OpenClipboard(IntPtr hWndNewOwner);
	
	[DllImport("user32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
	private static extern uint EmptyClipboard();

	[DllImport("user32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
	private static extern uint CloseClipboard();

	[DllImport("user32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
	private static extern uint IsClipboardFormatAvailable(uint uFormat);

	[DllImport("user32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
	private static extern IntPtr GetClipboardData(uint uFormat);

	[DllImport("user32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
	private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

	#endregion
}
