namespace LLT
{
	public interface ITSFactoryInstance
	{
		int FactoryTypeIndex { get; }
		string Name { get; set; }
		int SizeOf { get; }
	}
}