using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LLT
{
	public interface ITSTextAsset : ICoreTextAsset
	{
		byte[] Bytes { get; }

		IntPtr AddrOfPinnedObject();
		ITSTextAsset GetInstance();
		void Offset(int offset);
	}
}