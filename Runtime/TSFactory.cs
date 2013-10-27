using System;

namespace LLT
{
	public abstract class TSFactory
	{
		public abstract Type[] FactoryTypes
		{
			get;
		}
		
		public T Create<T>(int typeIndex, string name) where T : class, ITSFactoryInstance
	    {
			CoreAssert.Fatal(typeIndex < FactoryTypes.Length);
			var t = System.Activator.CreateInstance(FactoryTypes[typeIndex]) as T;
			
	        CoreAssert.Fatal(t != null);
			t.Name = name;
			
	        return t;
	    }
	}
}