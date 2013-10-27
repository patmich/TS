using System.Runtime.InteropServices;
namespace LLT
{
	[StructLayout(LayoutKind.Explicit, Size=16)]
	public struct TSTreeStreamTagStructLayout
	{
		[FieldOffset(0)]
		public ushort NameIndex;
		[FieldOffset(2)]
		public ushort EntrySizeOf;
		[FieldOffset(4)]
		public int SubTreeSizeOf;
		[FieldOffset(8)]
		public ushort JumpIndex;
		[FieldOffset(10)]
		public ushort ObjectIndex;
		[FieldOffset(12)]
		public byte TypeIndex;
	}
}
