using System.Collections.Generic;

namespace LLT
{
	public interface ITSTreeNode : ITSFactoryInstance
	{
		List<ITSTreeNode> Childs { get; }
		byte[] ToBytes();
	}
}