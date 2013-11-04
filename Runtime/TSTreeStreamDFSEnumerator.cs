using System.Collections.Generic;
using System.Collections;

namespace LLT
{
	public class TSTreeStreamDFSEnumerator<T>
		where T : TSTreeStreamEntry, new()
	{
		private sealed class TagList : List<TSTreeStreamTag>
		{
			private readonly ITSTreeStream _tree;
			
			public TagList(ITSTreeStream tree) : base()
			{
				_tree = tree;
			}
			
			public new TSTreeStreamTag this[int index]
			{
				get
				{
					if(index < Count)
					{
						return base[index];
					}
					
					CoreAssert.Fatal(index <= Count);
					var retVal = new TSTreeStreamTag(_tree);
					base.Add(retVal);
					return retVal;
				}
			}
		}
		
		private readonly ITSTreeStream _tree;
		private readonly TagList _tagList;
		private int _index;
		
		public T Parent { get; private set; }
		
		public TSTreeStreamTag ParentTag
		{
			get
			{
				CoreAssert.Fatal(_index > 0);
				return _tagList[_index - 1];
			}
		}
		
		public TSTreeStreamTag Current
    	{
        	get 
			{
				return _tagList[_index]; 
			}
    	}

		
		public TSTreeStreamDFSEnumerator(ITSTreeStream tree)
		{
			_tree = tree;
			
			_tagList = new TagList(_tree);
			
			CoreAssert.Fatal(_tree.RootTag != null);
			_tagList[0].Position = _tree.RootTag.Position;
			
			Parent = new T();
			Parent.Init(_tree);
			Parent.Position = _tagList[0].EntryPosition;
		}
		
		public virtual bool MoveNext (bool skipSubTree)
		{
			if(skipSubTree)
			{
				var poped = false;
				while(_index > 0 && _tagList[_index].SiblingPosition == _tagList[_index-1].SiblingPosition)
				{
					_index--;
					poped = true;
				}
				if(_index == 0)
				{
					return false;
				}
				else
				{
					_tagList[_index].Position = _tagList[_index].SiblingPosition;
					
					if(poped)
					{
						Parent.Position = _tagList[_index - 1].EntryPosition;
					}
				}
			}
			else if(_tagList[_index].SubTreeSizeOf == 0)
			{
				if(_tagList[_index].SiblingPosition < _tagList[_index-1].SiblingPosition)
				{
					_tagList[_index].Position = _tagList[_index].SiblingPosition;
				}
				else
				{
					while(_index > 0 && _tagList[_index].SiblingPosition == _tagList[_index-1].SiblingPosition)
					{
						_index--;
					}
					
					if(_index == 0)
					{
						return false;
					}
					else
					{
						_tagList[_index].Position = _tagList[_index].SiblingPosition;
						Parent.Position = _tagList[_index - 1].EntryPosition;
					}
				}
			}
			else
			{
				Parent.Position = _tagList[_index].EntryPosition;
				_index++;
				_tagList[_index].Position = _tagList[_index - 1].FirstChildPosition;
			}
			
			return true;
		}
		
		public void Dispose ()
		{
			
		}

		public void Reset ()
		{
			throw new System.NotImplementedException ();
		}
	}
}

