namespace LLT
{
	public sealed partial class TSTreeStreamTag : LLT.ITSFactoryInstance
	{
		public const int NameIndex_Offset = 0;
		public const int EntrySizeOf_Offset = 2;
		public const int SubTreeSizeOf_Offset = 4;
		public const int LinkIndex_Offset = 8;
		public const int ObjectIndex_Offset = 10;
		public const int TypeIndex_Offset = 12;
		public const int TSTreeStreamTagSizeOf = 16;


		public ushort NameIndex
		{
			get
			{
				return _tree.TextAsset.ReadUInt16(_position + 0);
			}
			set
			{
				_tree.TextAsset.Write(_position + 0, value);
			}
		}
		public ushort EntrySizeOf
		{
			get
			{
				return _tree.TextAsset.ReadUInt16(_position + 2);
			}
			set
			{
				_tree.TextAsset.Write(_position + 2, value);
			}
		}
		public int SubTreeSizeOf
		{
			get
			{
				return _tree.TextAsset.ReadInt32(_position + 4);
			}
			set
			{
				_tree.TextAsset.Write(_position + 4, value);
			}
		}
		public ushort LinkIndex
		{
			get
			{
				return _tree.TextAsset.ReadUInt16(_position + 8);
			}
			set
			{
				_tree.TextAsset.Write(_position + 8, value);
			}
		}
		public ushort ObjectIndex
		{
			get
			{
				return _tree.TextAsset.ReadUInt16(_position + 10);
			}
			set
			{
				_tree.TextAsset.Write(_position + 10, value);
			}
		}
		public byte TypeIndex
		{
			get
			{
				return _tree.TextAsset.ReadByte(_position + 12);
			}
			set
			{
				_tree.TextAsset.Write(_position + 12, value);
			}
		}

		public int SizeOf
		{
			get
			{
				return 16;
			}
		}
	}
}
