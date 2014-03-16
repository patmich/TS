using System;

namespace LLT
{
	public class TSObject : ITSObject
	{
		public TSObject ()
		{
		}
		
		public void Init (ITSTreeStream tree)
		{
			throw new NotImplementedException ();
		}
		
		public int Position 
		{
			get 
			{
				throw new NotImplementedException ();
			}
			set 
			{
				throw new NotImplementedException ();
			}
		}

		public ITSTreeStream Tree 
		{
			get 
			{
				throw new NotImplementedException ();
			}
		}

        public void Dispose()
        {
            throw new NotImplementedException ();
        }
	}
}

