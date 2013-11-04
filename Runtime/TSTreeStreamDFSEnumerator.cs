using System.Collections.Generic;
using System.Collections;
using System;

namespace LLT
{
	public class TSTreeStreamDFSEnumerator<R, T, E> : ITSTreeStreamDFSEnumerator
		where T : TSTreeStreamEntry, new()
		where E : TSTreeStreamDFSEnumerator<R, T, E>
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
		
		private readonly R _root;
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
				if(_link && _subEnumerator.Index > 1)
                {
                    return _subEnumerator.Parent;
                }
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
				
				return _tagList[_index]; 
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
                    
                    return new IntPtr((byte*)_tree.Ptr.ToPointer() + _tagList[_index - 1].EntryPosition);
                }
            }
        }
#endif
        
		public R Root
		{
			get
			{
				if(_link)
				{
					return _subEnumerator.Root;
				}
				
				return _root;
			}
		}
		
		public TSTreeStreamDFSEnumerator(R root, ITSTreeStream tree)
		{
			_root = root;
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
			if(_tagList[_index].LinkIndex != ushort.MaxValue)
			{
				if(_subEnumerator == null)
				{
					_link = true;
					_subEnumerator = _tree.Links[_tagList[_index].LinkIndex] as E;
					CoreAssert.Fatal(_subEnumerator != null);
                    
                    _parent.Position = _tagList[_index].EntryPosition;
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
						_parent.Position = _tagList[_index - 1].EntryPosition;
					}
				}
			}
            else if(skipSubTree)
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
                        _parent.Position = _tagList[_index - 1].EntryPosition;
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
						_parent.Position = _tagList[_index - 1].EntryPosition;
					}
				}
			}
			else
			{
				_parent.Position = _tagList[_index].EntryPosition;
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
			_index = 0;
			_parent.Position = _tagList[0].EntryPosition;
			_link = false;
		}
	}
}

