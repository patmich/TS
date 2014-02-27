using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace LLT
{
	public static class TSTreeStreamBuilder
	{
		private const int _alignment = 4;

		public static void Build(ITSTreeNode rootNode, ICoreStreamable meta, TSFactory factory, out byte[] buffer, out List<KeyValuePair<ITSTreeNode, int>> positions)
		{
			positions = new List<KeyValuePair<ITSTreeNode, int>>();
			buffer = new byte[TSTreeStreamTag.TSTreeStreamTagSizeOf];

			var lookup = new List<string>();

			using(var stream = new MemoryStream())
			{
				var writer = new BinaryWriter(stream);
				using(var treeStream = new MemoryStream())
				{
					BuildRecursive(new BinaryWriter(treeStream), rootNode, ref lookup, ref positions);
					
					long align = 0;

					// Write meta data.
					if(meta != null)
					{
						meta.Write(writer);
						
						align = _alignment - (stream.Position % _alignment);
						if(align != _alignment)
						{
							stream.Position += align;
						}
					}
					
					// Write string lookup table.
					writer.Write(lookup.Count);
					for (var i = 0; i < lookup.Count; i++)
					{
						foreach (char c in lookup[i])
						{
							writer.Write((byte)c);
						}
						writer.Write((byte)0);
					}
					
					align = _alignment - (stream.Position % _alignment);
					if(align != _alignment)
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
					buffer = new byte[writer.BaseStream.Position];
					stream.Position = 0;
					stream.Read(buffer, 0, buffer.Length);

					for(var i = 0; i < positions.Count; i++)
					{
						positions[i] = new KeyValuePair<ITSTreeNode, int>(positions[i].Key, positions[i].Value + (int)treePosition);
					}
				}
			}
		}
		
		private static TSTreeStreamTagStructLayout BuildRecursive(BinaryWriter writer, ITSTreeNode current, ref List<string> lookup, ref List<KeyValuePair<ITSTreeNode, int>> positions)
		{
			using(var stream = new MemoryStream())
			{
				var subTreeWriter = new BinaryWriter(stream);
				
				var childPositions = new List<KeyValuePair<ITSTreeNode, int>>();
				var childs = new List<TSTreeStreamTagStructLayout>();
				for (var i = 0; i < current.Childs.Count; i++)
				{
					var child = BuildRecursive(subTreeWriter, current.Childs[i], ref lookup, ref childPositions);
					childs.Add(child);
				}
				
				var tag = new TSTreeStreamTagStructLayout();
				tag.LinkIndex = ushort.MaxValue;
				
				if(!string.IsNullOrEmpty(current.Name))
				{
					var nameIndex = lookup.IndexOf(current.Name);
					if(nameIndex == -1)
					{
						CoreAssert.Fatal(lookup.Count < ushort.MaxValue);
						tag.NameIndex = (ushort)lookup.Count;
						lookup.Add(current.Name);
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