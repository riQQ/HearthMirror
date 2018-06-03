using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HearthMirror
{
	class Native
	{
		const string kernel32 = "kernel32.dll";

		[DllImport(kernel32, SetLastError = true)]
		public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

		public static IntPtr OpenProcess(Process proc, ProcessAccessFlags flags) => OpenProcess(flags, false, proc.Id);

		[DllImport(kernel32, SetLastError = true)]
		public static extern IntPtr CreateToolhelp32Snapshot(SnapshotFlags dwFlags, uint th32ProcessID);

		[DllImport(kernel32, SetLastError = true)]
		public static extern bool Module32First(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

		[DllImport(kernel32, SetLastError = true)]
		public static extern bool Module32Next(IntPtr hSnapshot, ref MODULEENTRY32 lpme);
	}

	[Flags]
	public enum ProcessAccessFlags : uint
	{
		All = 0x001F0FFF,
		Terminate = 0x00000001,
		CreateThread = 0x00000002,
		VirtualMemoryOperation = 0x00000008,
		VirtualMemoryRead = 0x00000010,
		VirtualMemoryWrite = 0x00000020,
		DuplicateHandle = 0x00000040,
		CreateProcess = 0x000000080,
		SetQuota = 0x00000100,
		SetInformation = 0x00000200,
		QueryInformation = 0x00000400,
		QueryLimitedInformation = 0x00001000,
		Synchronize = 0x00100000
	}

	[Flags]
	public enum SnapshotFlags : uint
	{
		HeapList = 0x00000001,
		Process = 0x00000002,
		Thread = 0x00000004,
		Module = 0x00000008,
		Module32 = 0x00000010,
		Inherit = 0x80000000,
		All = 0x0000001F,
		NoHeaps = 0x40000000
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct MODULEENTRY32
	{
		public uint dwSize;
		public uint th32ModuleID;
		public uint th32ProcessID;
		public uint GlblcntUsage;
		public uint ProccntUsage;
		public IntPtr modBaseAddr;
		public uint modBaseSize;
		public IntPtr hModule;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public string szModule;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szExePath;
	};
}
