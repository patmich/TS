using System;
using System.Collections.Generic;

namespace LLT
{
	public interface ITSTreeStream
	{
		TSTreeStreamTag RootTag { get; }
		bool RebuildPath(TSTreeStreamTag tag, out string path);
        string GetName(TSTreeStreamTag tag);
        List<ITSTreeStreamDFSEnumerator> Links { get; }
		void Link(TSTreeStreamTag tag, ITSTreeStreamDFSEnumerator dfs);
<<<<<<< HEAD
		IntPtr Ptr { get; }	
		ITSObject GetObject(TSTreeStreamTag tag);
		
=======
		IntPtr Ptr { get; }
>>>>>>> ee27a72531ee4ae70edec09cf59386e883cb1654
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

