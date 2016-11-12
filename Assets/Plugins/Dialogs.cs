using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

public class Dialogs
{
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]  
	public class OpenFileName 
	{
		public int      structSize = 0;
		public IntPtr   dlgOwner = IntPtr.Zero; 
		public IntPtr   instance = IntPtr.Zero;
		public String   filter = null;
		public String   customFilter = null;
		public int      maxCustFilter = 0;
		public int      filterIndex = 0;
		public String   file = null;
		public int      maxFile = 0;
		public String   fileTitle = null;
		public int      maxFileTitle = 0;
		public String   initialDir = null;
		public String   title = null;   
		public int      flags = 0; 
		public short    fileOffset = 0;
		public short    fileExtension = 0;
		public String   defExt = null; 
		public IntPtr   custData = IntPtr.Zero;  
		public IntPtr   hook = IntPtr.Zero;  
		public String   templateName = null; 
		public IntPtr   reservedPtr = IntPtr.Zero; 
		public int      reservedInt = 0;
		public int      flagsEx = 0;
	}

	[DllImport("user32.dll")]
	public static extern IntPtr GetActiveWindow();

	[DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
	public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);   
	[DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
	public static extern bool GetSaveFileName([In, Out] OpenFileName ofn);   

	public static string ShowFileDialog(string title, string filter, string initialDir, bool isSave) {
		OpenFileName ofn = new OpenFileName();
		ofn.dlgOwner = GetActiveWindow();
		ofn.title = title;
		ofn.structSize = Marshal.SizeOf(ofn);
		ofn.filter = filter;
		ofn.file = new string(new char[256]);
		ofn.maxFile = ofn.file.Length;
		ofn.initialDir = initialDir;
		
		//OFN_EXPLORER|OFN_PATHMUSTEXIST|OFN_NOCHANGEDIR
		ofn.flags=0x00080000|0x00000800|0x00000008;
		
		if (isSave) {
			if (GetSaveFileName(ofn)) {
				return ofn.file;
			}
		} else {
			if (GetOpenFileName(ofn)) {
				return ofn.file;
			}
		}
		return null;
	}
	
	[DllImport("user32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
	public static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);
	
	public enum MessageType {
		Ok = 0,
		OkCancel = 1,
		AbortRetryIgnore = 2,
		YesNoCancel = 3,
		YesNo = 4,
		RetryCancel = 5,
	}
	public enum MessageIcon {
		None = 0,
		Stop = 16,
		Question = 32,
		Exclamation = 48,
		Information = 64,
	}
	public enum MessageResult {
		Ok = 1,
		Cancel = 2,
		Abort = 3,
		Retry = 4,
		Ignore = 5,
		Yes = 6,
		No = 7,
	}
	public static MessageResult ShowMessage(string title, string caption, 
		MessageType type = MessageType.Ok, MessageIcon icon = MessageIcon.None) {
		return (MessageResult)MessageBox(GetActiveWindow(), title, caption, 
			(uint)type | (uint)icon);
	}
}
