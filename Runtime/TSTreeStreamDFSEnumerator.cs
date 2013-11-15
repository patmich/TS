using System.Collections.Generic;
using System.Collections;
using System;

namespace LLT
{
	public class TSTreeStreamDFSEnumerator<T, E> : ITSTreeStreamDFSEnumerator
		where T : TSTreeStreamEntry, new()
			where E : TSTreeStreamDFSEnumerator<T, E>
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

		private readonly TagList _tagList;
		
		protected readonly ITSTreeStream _tree;
		protected E _subEnumerator;
		protected bool _link;
		
		private int _index;
		private T _parent;
		
		public int Index
		{
			get
			{
				return _index;
			}
		}
		
		public T Parent 
		{
			get
			{
				if(_link)
				{
					return _subEnumerator.Parent;
				}
				
				_parent.Position = ParentTag.EntryPosition;
				return _parent;
			}
		}
		
		public TSTreeStreamTag ParentTag
		{
			get
			{
				if(_link && _subEnumerator.Index > 1)
				{
					return _subEnumerator.ParentTag;
				}
				
				CoreAssert.Fatal(_index > 0);
				return _tagList[_index - 1];
			}
		}
		
		public TSTreeStreamTag Current
		{
			get 
			{
				if(_link)
				{
					return _subEnumerator.Current;
				}
				
				CoreAssert.Fatal(_index < _tagList.Count);
				return _tagList[_index]; 
			}
		}
		
		public string CurrentName
		{
			get
			{
				if(_link)
				{
					return _subEnumerator.CurrentName;
				}
				return _tree.GetName(Current);
			}
		}
		
		public ITSObject CurrentObject
		{
			get
			{
				if(_link)
				{
					return _subEnumerator.CurrentObject;
				}
				return _tree.GetObject(Current);
			}
		}
		
		public ITSObject ParentObject
		{
			get
			{
				if(_link)
				{
					return _subEnumerator.ParentObject;
				}
				return _tree.GetObject(ParentTag);
			}
		}
		
        public ITSTreeStream Tree
        {
            get
            {
                if(_link)
                {
                    return _subEnumerator.Tree;
                }
                return _tree;
            }
        }
        
		#if ALLOW_UNSAFE
		public IntPtr CurrentPtr
		{
			get
			{
				unsafe
				{
					if(_link)
					{
						return _subEnumerator.CurrentPtr;
					}
					
					return new IntPtr((byte*)_tree.Ptr.ToPointer() + _tagList[_index].EntryPosition);
				}
			}
		}
		public IntPtr ParentPtr
		{
			get
			{
				unsafe
				{
					if(_link && _subEnumerator.Index > 1)
					{
						return _subEnumerator.ParentPtr;
					}
					
					CoreAssert.Fatal(_index > 0);
					return new IntPtr((byte*)_tree.Ptr.ToPointer() + _tagList[_index - 1].EntryPosition);
				}
			}
		}
		#endif
		
		public TSTreeStreamDFSEnumerator(ITSTreeStream tree)
		{
			_tree = tree;
			
			_tagList = new TagList(_tree);
			
			CoreAssert.Fatal(_tree.RootTag != null);
			_tagList[0].Position = _tree.RootTag.Position;
			
			_parent = new T();
			_parent.Init(_tree);
			_parent.Position = _tagList[0].EntryPosition;
			
			_link = false;
		}
		
		public virtual bool MoveNext (bool skipSubTree)
		{
			if(_tagList[_index].LinkIndex != ushort.MaxValue || _link)
			{
				if(!_link)
				{
					_link = true;
					_subEnumerator = _tree.Links[_tagList[_index].LinkIndex] as E;
					CoreAssert.Fatal(_subEnumerator != null);
					_subEnumerator.Reset();
					
					_index++;
				}
				if(_subEnumerator.MoveNext(skipSubTree))
				{
					return true;
				}
				else
				{
					_subEnumerator.Reset();
					_subEnumerator = null;
					_link = false;
					
					_index--;
					
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
					}
				}
			}
			else if(skipSubTree)
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
						//_parent.Position = _tagList[_index - 1].EntryPosition;
					}
				}
			}
			else
			{
				//_parent.Position = _tagList[_index].EntryPosition;
				_index++;
				_tagList[_index].Position = _tagList[_index - 1].FirstChildPosition;
			}
			
			return true;
		}
		
		public void Dispose ()
		{
			
		}
		
		public void Reset(TSTreeStreamTag parent, TSTreeStreamTag tag)
		{
            CoreAssert.Fatal(parent.Tree == _tree && tag.Tree == _tree);
			_index = 1;
			
			_tagList[0].Position = parent.Position;
			_tagList[1].Position = tag.Position;
			_link = false;
			
			_subEnumerator = null;
		}
		
		public void Reset(TSTreeStreamTag tag)
		{
            CoreAssert.Fatal(tag.Tree == _tree);
            
			_index = 0;
			
			_tagList[0].Position = tag.Position;
			_link = false;
			
			_subEnumerator = null;
		}
		
		public void Reset ()
		{
			_index = 0;
			
			_tagList[0].Position = _tree.RootTag.Position;
			_link = false;
			
			_subEnumerator = null;
		}
		
		public bool MoveTo(TSTreeStreamTag tag, params string[] path)
		{
			Reset(tag);
			
			if(path.Length == 0)
			{
				return true;
			}
			
			var index = 0;
			var skipSubTree = false;
			while(MoveNext(skipSubTree))
			{
				if(CurrentName == path[index])
				{
					if(++index == path.Length)
					{
						return true;
					}
					
					skipSubTree = false;
				}
				else
				{
					skipSubTree = true;
				}
			}
			
			return false;
		}
        
        
	}
}

