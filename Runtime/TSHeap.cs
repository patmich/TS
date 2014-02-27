using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LLT
{
	public sealed class TSHeap
	{
		private readonly TSHeap Instance = new TSHeap();

		private TSHeap()
		{

		}

		private int _count;

		private TSHeapEntry[] _entries = new TSHeapEntry[1 << 8];
		private GCHandle[] _handles = new GCHandle[1 << 8];

		private readonly Stack<int> _freeId = new Stack<int>();


		public TSHeapEntry Pin(byte[] buffer)
		{
			var id = 0;
			if(_freeId.Count > 0)
			{
				id = _freeId.Pop();
			}
			else
			{
				id = _count++;
			}

			if(_entries.Length <= id)
			{
				var tempEntries = new TSHeapEntry[_entries.Length << 1];
				System.Buffer.BlockCopy(_entries, 0, tempEntries, 0, _entries.Length);
				_entries = tempEntries;

				var tempHandles = new GCHandle[_handles.Length << 1];
				System.Buffer.BlockCopy(_handles, 0, tempHandles, 0, _handles.Length);
				_handles = tempHandles;
			}

			_entries[id].Id = id;

			var handle = GCHandle.Alloc(buffer);

			_handles[id] = handle;
			_entries[id].Ptr = handle.AddrOfPinnedObject();

			return _entries[id];
		}

		public void Release(TSHeapEntry entry)
		{
			_handles[entry.Id].Free();
			_entries[entry.Id].Ptr = IntPtr.Zero;
			_freeId.Push(entry.Id);
		}
	}
}