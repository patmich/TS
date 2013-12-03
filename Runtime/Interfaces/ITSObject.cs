namespace LLT
{
	public interface ITSObject : System.IDisposable
	{
		int Position { get; set; }
		void Init(ITSTreeStream tree);
	}
}