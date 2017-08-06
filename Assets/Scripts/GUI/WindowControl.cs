using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

public class WindowControl
{
	[DllImport("user32.dll")]
	private static extern IntPtr GetActiveWindow();

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int SetWindowTextW(IntPtr hWnd, string text);
	
	public static void SetWindowTitle(string title) {
		SetWindowTextW(GetActiveWindow(), title);
	}
}
