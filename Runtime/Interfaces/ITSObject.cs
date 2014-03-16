namespace LLT
{
	public interface ITSObject : System.IDisposable
	{
		ITSTreeStream Tree { get; }
		int Position { get; set; }
		void Init(ITSTreeStream tree);

	}
}