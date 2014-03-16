using System;
using System.Collections.Generic;

namespace LLT
{
	public interface ITSTreeStream : IDisposable
	{
		ITSTextAsset TextAsset { get; }

		TSTreeStreamTag RootTag { get; }
		TSTreeStreamTag FindTag(params string[] path);

		bool RebuildPath(TSTreeStreamTag tag, out string path);
        string GetName(TSTreeStreamTag tag);

		ITSObject GetObject(TSTreeStreamTag tag);

		ITSTreeStream Parent { get; set; }
	}
}

