
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace LLT
{
	[Serializable]
	public abstract class TSTreeStream<O> : IDisposable, ITSTreeStream, ITreeStreamQuery<O>
		where O : class, ITSObject, new()
	{
		public const int Alignment = 4;
		
		
		public abstract ITSTreeStreamDFSEnumerator Iter { get; }
			
		private byte[] _buffer;		
		private readonly List<string> _lookup = new List<string>();
		private readonly List<ITSTreeStreamDFSEnumerator> _links = new List<ITSTreeStreamDFSEnumerator>();
		private readonly ITSTreeStreamDFSEnumerator _dfs;
		
		private TSTreeStreamTag _rootTag;
		private GCHandle _handle;
		public IntPtr Ptr { get; private set; }

        private List<O> _objects = new List<O>();

		public TSTreeStreamTag RootTag
		{
			get
			{
				if(_rootTag == null)
				{
					_rootTag = new TSTreeStreamTag(this);
				}
				
				return _rootTag;
			}
		}
		
		public int Length 
		{
			get
			{
				return _buffer.Length;
			}
		}
		
		public List<ITSTreeStreamDFSEnumerator> Links
		{
			get
			{
				return _links;
			}
		}
		
		public void InitFromBytes(byte[] buffer, ICoreStreamable meta, TSFactory factory)
		{
			_buffer = buffer;
			
			using(var stream = new MemoryStream(_buffer))
			{
				var binaryReader = new BinaryReader(stream);
				
				if(meta != null)
				{
					meta.Read(binaryReader);
				}
				
				var align = Alignment - (stream.Position % Alignment);
				if(align != Alignment)
				{
					stream.Position += align;
				}
				
				int count = binaryReader.ReadInt32();
				_lookup.Capacity = count;
				
				var sb = new StringBuilder();
				for(var i = 0; i < count; i++)
	            {
					byte b;
					
					while((b = binaryReader.ReadByte()) != 0)
					{
						sb.Append((char)b);
					}
					
					_lookup.Add(sb.ToString());
					
					sb.Remove(0, sb.Length);
				}
				
				align = Alignment - (stream.Position % Alignment); 
				if(align != Alignment)
				{
					stream.Position += align;
				}
				
				_rootTag = CreateTag((int)stream.Position);
			}
			
            _handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
            Ptr = _handle.AddrOfPinnedObject();
		}
		
		
		public TSTreeStreamTag CreateTag(int position)
		{
			return new TSTreeStreamTag(this, position);
		}
		
		public void Link(TSTreeStreamTag tag, ITSTreeStreamDFSEnumerator dfs)
		{
			CoreAssert.Fatal(_links.Count < ushort.MaxValue);
			tag.LinkIndex = (ushort)_links.Count;
			
			_links.Add(dfs);
		}
		
		
		
		public string GetName(TSTreeStreamTag tag)
		{
			if(tag.NameIndex == ushort.MaxValue)
			{
				return string.Empty;
			}
			
			CoreAssert.Fatal(0 <= tag.NameIndex && tag.NameIndex < _lookup.Count);
			return _lookup[tag.NameIndex];
		}
		
		public ITSObject GetObject(TSTreeStreamTag tag)
		{
			if(tag.ObjectIndex == ushort.MaxValue)
			{                
				var obj = new O();
                obj.Position = tag.Position;
                obj.Init(this);
                
                CoreAssert.Fatal(_objects.Count(x=>x.Position == tag.Position) == 0);
                _objects.Add(obj);
                
                tag.ObjectIndex = (ushort)(_objects.Count - 1);
                return obj;
			}
			
            CoreAssert.Fatal(0 <= tag.ObjectIndex && tag.ObjectIndex < _objects.Count);
            return _objects[tag.ObjectIndex];
		}
		
		public O FindObject(params string[] path)
		{
			return FindObject(RootTag, path) as O;
		}
		
		public O FindObject(TSTreeStreamTag tag, params string[] path)
		{
            CoreAssert.Fatal(tag.Tree == this);
			if(Iter.MoveTo(tag, path))
            {
                if(Iter.Current.LinkIndex != ushort.MaxValue)
                {
                    Iter.MoveNext(false);
                    return Iter.ParentObject as O;
                }
                
    			return Iter.CurrentObject as O;
            }
            return null;
		}
		
        public O FindFirstObject(string name)
        {
            return FindFirstObject(RootTag, name);
        }

        public O FindFirstObject(TSTreeStreamTag tag, string name)
        {
            Iter.Reset(tag);
            
            while(Iter.MoveNext(false))
            {
                if(Iter.CurrentName == name)
                {
                    return FindObject(Iter.Current);
                }
            }
            
            return null;
        }

        public List<O> GetChilds(TSTreeStreamTag tag)
        {
            CoreAssert.Fatal(tag.Tree == this);
            Iter.Reset(tag);

            var retVal = new List<O>();
            var skipSubTree = false;
            while(Iter.MoveNext(skipSubTree))
            {
                skipSubTree = true;
                retVal.Add(Iter.CurrentObject as O);
            }

            return retVal;
        }

        public void FillChilds(TSTreeStreamTag tag, List<O> childs)
        {
            CoreAssert.Fatal(tag.Tree == this);
            Iter.Reset(tag);
            
            childs.Clear();
            var skipSubTree = false;
            while(Iter.MoveNext(skipSubTree))
            {
                skipSubTree = true;
                childs.Add(Iter.CurrentObject as O);
            }
        }

		public TSTreeStreamTag FindTag(params string[] path)
		{
			return FindTag(RootTag, path);
		}
		
		public TSTreeStreamTag FindTag(TSTreeStreamTag tag, params string[] path)
		{
			Iter.MoveTo(tag, path);
			return Iter.Current;
		}
		
        public bool RebuildPath(TSTreeStreamTag tag, out string path)
        {
            if(tag == null)
            {
                path = string.Empty;
                return false;
            }
            
            var entryPosition = tag.EntryPosition;
            var parentTag = new TSTreeStreamTag(this);
            tag = new TSTreeStreamTag(this);
             
            parentTag.Position = RootTag.Position;
            tag.Position = parentTag.FirstChildPosition;
            
            CoreAssert.Fatal(parentTag.NameIndex != ushort.MaxValue);
            path = GetName(parentTag);
            
            if(parentTag.EntryPosition == entryPosition)
            {
                return true;
            }
            
            while(tag.Position < entryPosition)
            {
                if(tag.EntryPosition == entryPosition)
                {
                    if(tag.NameIndex != ushort.MaxValue)
                    {
                        path = path + "/" + GetName(tag);
                        return true;   
                    }
                    return false;
                }
				else if(tag.FirstChildPosition < entryPosition && entryPosition < tag.SiblingPosition)
                {
                    if(tag.NameIndex != ushort.MaxValue)
                    {
                        path = path + "/" + GetName(tag);
                    }
                    
                    CoreAssert.Fatal(tag.SubTreeSizeOf > 0);
                    parentTag.Position = tag.Position;
                    tag.Position = tag.FirstChildPosition;
                }
                else
                {
                    if(tag.SiblingPosition == parentTag.SiblingPosition)
                    {
                        return false;
                    }
                    tag.Position = tag.SiblingPosition;
                }
            }
            
            return false;
        }
		
		public int ReadInt32(int position)
		{
			CoreAssert.Fatal(position + sizeof(int) <= _buffer.Length);
			
#if  !ALLOW_UNSAFE
			return BitConverter.ToInt32(_buffer, position);
#else
			unsafe
			{
				fixed(byte* ptr = &_buffer[position])
				{
					return *(int*)ptr;
				}
			}		
#endif
		}
		
		public uint ReadUInt32(int position)
		{
			CoreAssert.Fatal(position + sizeof(uint) <= _buffer.Length);
			
#if  !ALLOW_UNSAFE
			return BitConverter.ToUInt32(_buffer, position);
#else
			unsafe
			{
				fixed(byte* ptr = &_buffer[position])
				{
					return *(uint*)ptr;
				}
			}		
#endif
		}
		
		public float ReadSingle(int position)
		{
			CoreAssert.Fatal(position + sizeof(float) <= _buffer.Length);

#if  !ALLOW_UNSAFE
			return BitConverter.ToSingle(_buffer, position);
#else
			unsafe
			{
				fixed(byte* ptr = &_buffer[position])
				{
					return *(float*)ptr;
				}
			}		
#endif
	
		}
		
		public byte ReadByte(int position)
		{
			CoreAssert.Fatal(position < _buffer.Length);
			return _buffer[position];
		}
		
		public ushort ReadUInt16(int position)
		{
			CoreAssert.Fatal(position + sizeof(ushort) <= _buffer.Length);
#if  !ALLOW_UNSAFE
			return BitConverter.ToUInt16(_buffer, position);
#else
			unsafe
			{
				fixed(byte* ptr = &_buffer[position])
				{
					return *(ushort*)ptr;
				}
			}		
#endif
		}
		
		public void Write(int position, int val)
		{
			CoreAssert.Fatal(position + sizeof(int) < _buffer.Length);
#if  !ALLOW_UNSAFE
			Buffer.BlockCopy(BitConverter.GetBytes(val), 0, _buffer, position, sizeof(int));
#else
			unsafe
			{
				fixed(byte* ptr = &_buffer[position])
				{
					*(int*)ptr = val;
				}
			}		
#endif
		}
		
		public void Write(int position, uint val)
		{
			CoreAssert.Fatal(position + sizeof(uint) < _buffer.Length);
#if  !ALLOW_UNSAFE
			Buffer.BlockCopy(BitConverter.GetBytes(val), 0, _buffer, position, sizeof(uint));
#else
			unsafe
			{
				fixed(byte* ptr = &_buffer[position])
				{
					*(uint*)ptr = val;
				}
			}		
#endif
		}
		
		public void Write(int position, byte val)
		{
			CoreAssert.Fatal(position < _buffer.Length);
			_buffer[position] = val;
		}
		
		public void Write(int position, float val)
		{
			CoreAssert.Fatal(position + sizeof(float) < _buffer.Length);
#if  !ALLOW_UNSAFE
			Buffer.BlockCopy(BitConverter.GetBytes(val), 0, _buffer, position, sizeof(float));
#else
			unsafe
			{
				fixed(byte* ptr = &_buffer[position])
				{
					*(float*)ptr = val;
				}
			}		
#endif
		}
		
		public void Write(int position, ushort val)
		{
			CoreAssert.Fatal(position + sizeof(ushort) < _buffer.Length);
#if  !ALLOW_UNSAFE
			Buffer.BlockCopy(BitConverter.GetBytes(val), 0, _buffer, position, sizeof(ushort));
#else
			unsafe
			{
				fixed(byte* ptr = &_buffer[position])
				{
					*(ushort*)ptr = val;
				}
			}		
#endif
		}
		
		public void WriteAllBytes(string path)
		{
			File.WriteAllBytes(path, _buffer);
		}
		
        public byte[] GetAllBytes() 
        {
            return _buffer.ToArray();
        }

		public void Dispose ()
		{
			if(_handle.IsAllocated)
            {
                Ptr = IntPtr.Zero;
                _handle.Free();
            }

            for(var i = 0; i < _objects.Count; i++)
            {
                _objects[i].Dispose();
            }
		}
		
        ~TSTreeStream()
        {
            Dispose();
        }
	}
}