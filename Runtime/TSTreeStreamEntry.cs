using System;
using System.IO;

namespace LLT
{
	
	
	public class TSTreeStreamEntry : ITSFactoryInstance
	{
	    public ITSTreeStream _tree;
		protected int _position;
		private string _name;
		
		public string Name 
		{
			get 
			{
				return _name;
			}
			set 
			{
				_name = value;
			}
		}
		
		public virtual int SizeOf 
		{
			get 
			{
				throw new NotImplementedException ();
			}
		}
		
		public virtual int Position
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
		
		public int FactoryTypeIndex 
		{
			get 
			{
				throw new NotImplementedException ();
			}
		}
		
		public TSTreeStreamEntry()
		{
		}
		
		public virtual void Init(ITSTreeStream tree)
		{
			_tree = tree;
		}
		
		public void Affect(TSPropertyType propertyType, int offset, float val)
		{
			switch(propertyType)
			{
				case TSPropertyType._byte:_tree.Write(_position + offset, (byte)val);break;
				case TSPropertyType._ushort:_tree.Write(_position + offset, (ushort)val);break;
				case TSPropertyType._int:_tree.Write(_position + offset, (int)val);break;
				case TSPropertyType._float:_tree.Write(_position + offset, val);break;
			}
		}
	}
}