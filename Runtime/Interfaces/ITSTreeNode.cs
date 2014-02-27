using System.Collections.Generic;

namespace LLT
{
	public interface ITSTreeNode : ITSFactoryInstance
	{
		List<ITSTreeNode> Childs { get; }

		// Provide interface for lookup.
		byte[] ToBytes(List<string> lookup);
	}
}