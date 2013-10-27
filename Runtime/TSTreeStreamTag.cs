using System.Collections.Generic;

namespace LLT
{
	[TSLayout(typeof(ushort), "NameIndex", 0)]
	[TSLayout(typeof(ushort), "EntrySizeOf", 1)]
	[TSLayout(typeof(int), "SubTreeSizeOf", 2)]
	[TSLayout(typeof(ushort), "JumpIndex", 3)]
	[TSLayout(typeof(ushort), "ObjectIndex", 4)]
	[TSLayout(typeof(byte), "TypeIndex", 5)]
	public sealed partial class TSTreeStreamTag : TSTreeStreamEntry
	{
		public int EntryPosition 
		{
			get
			{
				return Position + TSTreeStreamTag.TSTreeStreamTagSizeOf;
			}
		}
		
		public int FirstChildPosition
		{
			get
			{
				return Position + TSTreeStreamTag.TSTreeStreamTagSizeOf + EntrySizeOf;
			}
		}
		
		public int SiblingPosition
		{
			get
			{
				return Position + TSTreeStreamTag.TSTreeStreamTagSizeOf + EntrySizeOf + SubTreeSizeOf;
			}
		}
		
		internal TSTreeStreamTag(ITSTreeStream tree, int position, ITSFactoryInstance current, List<TSTreeStreamTag> childs)
	    {
	       	_tree = tree;
	        Position = position;
	 	
			CoreAssert.Fatal(current.SizeOf < ushort.MaxValue);
			EntrySizeOf = (ushort)current.SizeOf;
			
			CoreAssert.Fatal(current.FactoryTypeIndex < byte.MaxValue);
			TypeIndex = (byte)current.FactoryTypeIndex;
			
			ObjectIndex = ushort.MaxValue;
			JumpIndex = ushort.MaxValue;
			
			uint subTreeSizeOf = 0;
			for(var i = 0; i < childs.Count; i++)
	        {
	            subTreeSizeOf += (uint)(childs[i].EntrySizeOf + TSTreeStreamTag.TSTreeStreamTagSizeOf + childs[i].SubTreeSizeOf);
			}
			
			CoreAssert.Fatal(subTreeSizeOf < ushort.MaxValue);
			SubTreeSizeOf = (ushort)subTreeSizeOf;
	    }
		
		public TSTreeStreamTag(ITSTreeStream tree, int position)
		{
			_tree = tree;
	        Position = position;
		}
		
		public TSTreeStreamTag(ITSTreeStream tree)
		{
			_tree = tree;
		}
	}
}