using System;

namespace LLT
{
	public interface ITSTreeStreamDFSEnumerator
	{
		bool MoveNext(bool skipSubTree);
		bool MoveTo(TSTreeStreamTag tag, params string[] path);
		
		void Reset();
		void Reset(TSTreeStreamTag tag);
		
		TSTreeStreamTag Current { get; }
		string CurrentName { get; }
		ITSObject CurrentObject { get; }
        ITSObject ParentObject { get; }
	}
}

