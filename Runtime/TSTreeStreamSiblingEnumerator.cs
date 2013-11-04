using System.Collections.Generic;
using System.Collections;

namespace LLT
{
	public sealed class TSTreeStreamSiblingEnumerator : IEnumerator<TSTreeStreamTag>
	{		
		private TSTreeStreamTag _parent;
		private readonly TSTreeStreamTag _initial;
		private readonly TSTreeStreamTag  _current;
		private bool _empty;
		private bool _initialized;
		
		public bool Done { get; private set; }
		
		public TSTreeStreamTag Current
    	{
        	get 
			{
				if(!_initialized)
				{
					return null;
				}
				
				return _current; 
			}
    	}


	    object IEnumerator.Current
	    {
	        get 
			{
				return Current; 
			}
	    }
		
		public TSTreeStreamSiblingEnumerator(ITSTreeStream tree)
		{
			_initial = new TSTreeStreamTag(tree);
			_current = new TSTreeStreamTag(tree);
		}
		
		public void Init(TSTreeStreamTag parent, TSTreeStreamTag initial)
		{
			_parent = parent;
			
			if(_parent.FirstChildPosition == _parent.SiblingPosition)
			{
				_empty = true;
				return;
			}
			
			_empty = false;
			
			_initial.Init(initial.Tree);
			_current.Init(initial.Tree);
			
			_initial.Position = initial.Position;
			_current.Position = _initial.Position;
			
			_initialized = false;
			Done = false;
		}
		
		public void Init(TSTreeStreamTag parent)
		{
			_parent = parent;
			
			if(_parent.FirstChildPosition == _parent.SiblingPosition)
			{
				_empty = true;
				return;
			}
			
			_empty = false;
			
			_initial.Position = _parent.FirstChildPosition;
			_current.Position = _initial.Position;
			_initialized = false;
			Done = false;
		}
		
		public bool MoveNext ()
		{
			if(_empty)
			{
				return false;
			}
		
			if(!_initialized)
			{
				_initialized = true;
				return true;
			}
			
			CoreAssert.Fatal(_parent != null && _initial != null && _current != null);
			if(_parent.SiblingPosition == _current.SiblingPosition)
			{
				Done = true;
				return false;
			}
			
			_current.Position = _current.SiblingPosition;
			return true;
		}
		
		public void Dispose ()
		{
			
		}

		public void Reset ()
		{
			if(_empty)
			{
				return;
			}
			
			_initialized = false;
			_current.Position = _initial.Position;
			Done = false;
		}
	}
}