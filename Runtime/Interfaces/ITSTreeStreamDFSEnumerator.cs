using System;

namespace LLT
{
	public interface ITSTreeStreamDFSEnumerator
	{
		bool MoveNext(bool skipSubTree);
	}
}

