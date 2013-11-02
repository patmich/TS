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
		private int _entryPosition;
		private int _firstChildPosition;
		private int _siblingPosition;
		
		public int EntryPosition 
		{
			get
			{
				return _entryPosition;
			}
		}
		
		public int FirstChildPosition
		{
			get
			{
				return _firstChildPosition;
			}
		}
		
		public int SiblingPosition
		{
			get
			{
				return _siblingPosition;
			}
		}
		
		public override int Position 
		{
			get 
			{
				return _position;
			}
			set 
			{
				_position = value;
				_entryPosition = _position + TSTreeStreamTag.TSTreeStreamTagSizeOf;
				_firstChildPosition = _entryPosition + EntrySizeOf;
				_siblingPosition = _firstChildPosition + SubTreeSizeOf;
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
			
			CoreAssert.Fatal(subTreeSizeOf < int.MaxValue);
			SubTreeSizeOf = (int)subTreeSizeOf;
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