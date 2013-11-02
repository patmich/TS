using System;

namespace LLT
{
	public interface ITSTreeStream
	{
		TSTreeStreamTag RootTag { get; }
		bool RebuildPath(TSTreeStreamTag tag, out string path);
        string GetName(TSTreeStreamTag tag);
        
		int ReadInt32(int position);
		float ReadSingle(int position);
		byte ReadByte(int position);
		ushort ReadUInt16(int position);
		void Write(int position, int val);
		void Write(int position, byte val);
		void Write(int position, float val);
		void Write(int position, ushort val);
	}
}

