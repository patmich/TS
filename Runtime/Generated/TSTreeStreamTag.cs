namespace LLT
{
	public sealed partial class TSTreeStreamTag : TSTreeStreamEntry, ITSFactoryInstance
	{
		public const int NameIndex_Offset = 0;
		public const int EntrySizeOf_Offset = 2;
		public const int SubTreeSizeOf_Offset = 4;
		public const int JumpIndex_Offset = 8;
		public const int ObjectIndex_Offset = 10;
		public const int TypeIndex_Offset = 12;
		public const int TSTreeStreamTagSizeOf = 16;


		public ushort NameIndex
		{
			get
			{
				return _tree.ReadUInt16(_position + 0);
			}
			set
			{
				_tree.Write(_position + 0, value);
			}
		}
		public ushort EntrySizeOf
		{
			get
			{
				return _tree.ReadUInt16(_position + 2);
			}
			set
			{
				_tree.Write(_position + 2, value);
			}
		}
		public int SubTreeSizeOf
		{
			get
			{
				return _tree.ReadInt32(_position + 4);
			}
			set
			{
				_tree.Write(_position + 4, value);
			}
		}
		public ushort JumpIndex
		{
			get
			{
				return _tree.ReadUInt16(_position + 8);
			}
			set
			{
				_tree.Write(_position + 8, value);
			}
		}
		public ushort ObjectIndex
		{
			get
			{
				return _tree.ReadUInt16(_position + 10);
			}
			set
			{
				_tree.Write(_position + 10, value);
			}
		}
		public byte TypeIndex
		{
			get
			{
				return _tree.ReadByte(_position + 12);
			}
			set
			{
				_tree.Write(_position + 12, value);
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
			}
		}

		public override void Init(ITSTreeStream tree)
		{
			_tree = tree;
		}

		public override int SizeOf
		{
			get
			{
				return 16;
			}
		}
	}
}
