namespace LLT
{
	public interface ITSObject
	{
		int Position { get; set; }
		void Init(ITSTreeStream tree);
	}
}