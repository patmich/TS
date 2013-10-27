using System;

namespace LLT
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
	public sealed class TSLayoutAttribute : Attribute
	{
		public Type Type { get; private set; }
		public string Name { get; private set; }
		public int Order { get; private set; }
		
		public TSLayoutAttribute(Type type, string name, int order)
		{
			Type = type;
			Name = name;
			Order = order;
		}
	}
}