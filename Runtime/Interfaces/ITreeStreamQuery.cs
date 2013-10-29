using System;

namespace LLT
{
	public interface ITreeStreamQuery<T>
		where T : class, ITSObject, new()
	{
		T FindObject(params string[] path);
	}
}

