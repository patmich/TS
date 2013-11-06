
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
		
		protected abstract List<O> Objects { get; }
		public abstract ITSTreeStreamDFSEnumerator Iter { get; }
			
		private byte[] _buffer;		
		private readonly List<string> _lookup = new List<string>();
		private readonly List<ITSTreeStreamDFSEnumerator> _links = new List<ITSTreeStreamDFSEnumerator>();
		private readonly ITSTreeStreamDFSEnumerator _dfs;
		
		private TSTreeStreamTag _rootTag;
		private GCHandle _handle;
		public IntPtr Ptr { get; private set; }
        
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
			
			for(var i = 0; i < Objects.Count; i++)
			{
				Objects[i].Init(this);
			}
            
            _handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
            Ptr = _handle.AddrOfPinnedObject();
		}
		
		
		internal TSTreeStreamTag CreateTag(int position)
		{
			return new TSTreeStreamTag(this, position);
		}
		
		public void Link(TSTreeStreamTag tag, ITSTreeStreamDFSEnumerator dfs)
		{
			CoreAssert.Fatal(_links.Count < ushort.MaxValue);
			tag.LinkIndex = (ushort)_links.Count;
			
			_links.Add(dfs);
		}
		
		public O GetObject(int position)
		{
			var obj = new O();
			obj.Position = position;
			
			CoreAssert.Fatal(Objects.Count(x=>x.Position == position) == 0);
			Objects.Add(obj);
		
			var tag = CreateTag(position);
			tag.ObjectIndex = (ushort)(Objects.Count - 1);
			
			return obj;
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
				return null;
			}
			
			CoreAssert.Fatal(0 <= tag.ObjectIndex && tag.ObjectIndex < Objects.Count);
			return Objects[tag.ObjectIndex];
		}
		
		public O FindObject(params string[] path)
		{
			return FindObject(RootTag, path) as O;
		}
		
		public O FindObject(TSTreeStreamTag tag, params string[] path)
		{
			Iter.MoveTo(tag, path);
			return Iter.CurrentObject as O;
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
                else if(tag.Position <= entryPosition && entryPosition <= tag.FirstChildPosition)
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
		
		public void Dispose ()
		{
			if(_handle.IsAllocated)
            {
                Ptr = IntPtr.Zero;
                _handle.Free();
            }
		}
		
        ~TSTreeStream()
        {
            Dispose();
        }
        
		public List<KeyValuePair<ITSTreeNode, int>> InitFromTree(ITSTreeNode root, ICoreStreamable meta, TSFactory factory)
		{
			var positions = new List<KeyValuePair<ITSTreeNode, int>>();
			
			_buffer = new byte[TSTreeStreamTag.TSTreeStreamTagSizeOf];
			
			using(var stream = new MemoryStream())
			{
				var writer = new BinaryWriter(stream);
				using(var treeStream = new MemoryStream())
				{
					BuildRecursive(new BinaryWriter(treeStream), root, ref positions);
					
					long align = 0;
					// Write meta data.
					if(meta != null)
					{
						meta.Write(writer);
						
						align = Alignment - (stream.Position % Alignment);
						if(align != Alignment)
						{
							stream.Position += align;
						}
					}
					
					// Write string lookup table.
					writer.Write(_lookup.Count);
					for (var i = 0; i < _lookup.Count; i++)
		            {
		                foreach (char c in _lookup[i])
		                {
		                    writer.Write((byte)c);
		                }
		                writer.Write((byte)0);
		            }
					
					align = Alignment - (stream.Position % Alignment);
					if(align != Alignment)
					{
						stream.Position += align;
					}
					
					// Cache tree position.
					var treePosition = writer.BaseStream.Position;
					
					var copyBuffer = new byte[treeStream.Position];
					treeStream.Position = 0;
					treeStream.Read(copyBuffer, 0, copyBuffer.Length);
					
					// Write tree data.
					writer.Write(copyBuffer);
					
					// Build buffer.
					_buffer = new byte[writer.BaseStream.Position];
					stream.Position = 0;
					stream.Read(_buffer, 0, _buffer.Length);
					
					_rootTag = CreateTag((int)treePosition);
					
					for(var i = 0; i < positions.Count; i++)
					{
						positions[i] = new KeyValuePair<ITSTreeNode, int>(positions[i].Key, positions[i].Value + (int)treePosition);
					}
				}
			}
			
			return positions;
		}
		
		private TSTreeStreamTagStructLayout BuildRecursive(BinaryWriter writer, ITSTreeNode current, ref List<KeyValuePair<ITSTreeNode, int>> positions)
		{
			using(var stream = new MemoryStream())
			{
				var subTreeWriter = new BinaryWriter(stream);
				
				var childPositions = new List<KeyValuePair<ITSTreeNode, int>>();
				var childs = new List<TSTreeStreamTagStructLayout>();
				for (var i = 0; i < current.Childs.Count; i++)
				{
				    var child = BuildRecursive(subTreeWriter, current.Childs[i], ref childPositions);
				    childs.Add(child);
				}
				
				var tag = new TSTreeStreamTagStructLayout();
				tag.LinkIndex = ushort.MaxValue;
				
				if(!string.IsNullOrEmpty(current.Name))
				{
					var nameIndex = _lookup.IndexOf(current.Name);
					if(nameIndex == -1)
					{
						CoreAssert.Fatal(_lookup.Count < ushort.MaxValue);
						tag.NameIndex = (ushort)_lookup.Count;
						_lookup.Add(current.Name);
					}
					else
					{
						CoreAssert.Fatal(nameIndex < ushort.MaxValue);
						tag.NameIndex = (ushort)nameIndex;
					}
				}
				else
				{
					tag.NameIndex = ushort.MaxValue;
				}
				
				tag.ObjectIndex = ushort.MaxValue;
				
				CoreAssert.Fatal(current.FactoryTypeIndex < byte.MaxValue);
				tag.TypeIndex = (byte)current.FactoryTypeIndex;
				
				CoreAssert.Fatal(current.SizeOf < ushort.MaxValue);
				tag.EntrySizeOf = (ushort)current.SizeOf;
			
				var subTreeSizeOf = 0;
				for(var i = 0; i < childs.Count; i++)
				{
					subTreeSizeOf += childs[i].EntrySizeOf + childs[i].SubTreeSizeOf + TSTreeStreamTag.TSTreeStreamTagSizeOf;
				}

				CoreAssert.Fatal(subTreeSizeOf < int.MaxValue);
				tag.SubTreeSizeOf = subTreeSizeOf;
				
				var currentPosition = (int)writer.BaseStream.Position;
				
				var buffer = new byte[Marshal.SizeOf(tag)];
				var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
				Marshal.StructureToPtr(tag, handle.AddrOfPinnedObject(), false);
				handle.Free();
				writer.Write(buffer);

				writer.Write(current.ToBytes());
				
				for(var i = 0; i < childPositions.Count; i++)
				{
					childPositions[i] = new KeyValuePair<ITSTreeNode, int>(childPositions[i].Key, childPositions[i].Value + (int)writer.BaseStream.Position);
				}
				
				buffer = new byte[stream.Position];
				stream.Position = 0;
				stream.Read(buffer, 0, buffer.Length);
				writer.Write(buffer);
				
				positions.AddRange(childPositions);
				positions.Add(new KeyValuePair<ITSTreeNode, int>(current,currentPosition));
				
				return tag;
			}
		}
	}
}