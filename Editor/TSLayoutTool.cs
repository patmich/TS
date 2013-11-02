using System;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;

namespace LLT
{
	public static class TSLayoutTool
	{
		private class EntryProperty
		{
			public int Offset { get; private set; }
			public Type Type { get; private set; }
			
			public EntryProperty(int offset, Type type)
			{
				Offset = offset;	
				Type = type;
			}
		}
		
		private class OffsetProperty
		{
			public string Name { get; private set; }
			public int Offset { get; private set; }
			public Type Type { get; private set; }
			
			public OffsetProperty(string name, int offset, Type type)
			{
				Name = name;
				Offset = offset;	
				Type = type;
			}
		}
		
		public static void Execute(Type type, string output)
		{
			var inplaceFileName = string.Format("{0}/Generated/{1}.cs", output, type.Name);
			Directory.CreateDirectory(Path.GetDirectoryName(inplaceFileName));
			using(var inplace = new FileStream(inplaceFileName, FileMode.Create))
			{
				var textWriter = new StreamWriter(inplace);
				
				if(!type.IsPublic || !type.IsSealed || !type.IsSubclassOf(typeof(TSTreeStreamEntry)))
				{
					throw new Exception("Type not as expected");
				}
				
				textWriter.WriteLine(string.Format("namespace {0}",type.Namespace));
				textWriter.WriteLine("{");
				textWriter.WriteLine(string.Format("\tpublic sealed partial class {0} : {1}, {2}", type.Name, type.BaseType.FullName, type.GetInterfaces().Select(x=>x.FullName).Aggregate((x,y)=>x + "," + y)));
				textWriter.WriteLine("\t{");
			
				var offset = 0;
				var nextOffset = 0;
				
				var layoutAttributes = type.GetCustomAttributes(typeof(TSLayoutAttribute), false).Cast<TSLayoutAttribute>().OrderBy(x=>x.Order);
				
				foreach(var val in OffsetBuilder(offset, type))
				{
					var offsetProperty = val as OffsetProperty;
					if(offsetProperty != null)
					{
						textWriter.WriteLine(string.Format("\t\tpublic const int {0}_Offset = {1};", offsetProperty.Name, offsetProperty.Offset));
					}
				}
				
				textWriter.WriteLine(string.Format("\t\tpublic const int {0}SizeOf = {1};", type.Name, SizeOf(type, true)));
				

				textWriter.WriteLine();
				foreach(TSLayoutAttribute layoutAttribute in layoutAttributes)
				{
					if(layoutAttribute.Type.IsSubclassOf(typeof(TSTreeStreamEntry)))
					{
						textWriter.WriteLine(string.Format("\t\tpublic readonly {0} {1} = new {0}();", layoutAttribute.Type.FullName, layoutAttribute.Name));
					}
				}
				
				textWriter.WriteLine();
				
				foreach(TSLayoutAttribute layoutAttribute in layoutAttributes)
				{
					var size = SizeOf(layoutAttribute.Type, false);
					offset = nextOffset;
					nextOffset = ComputeOffset(ref offset, size);
					
					if(layoutAttribute.Type.IsValueType)
					{
						var memberType = layoutAttribute.Type;
						var memberTypeName = memberType.Name;
						
						if(memberType == typeof(Single))
						{
							memberType = typeof(float);
							memberTypeName = "float";
						}
						if(memberType == typeof(Int32))
						{
							memberType = typeof(int);
							memberTypeName = "int";
						}
						if(memberType == typeof(Byte))
						{
							memberType = typeof(byte);
							memberTypeName = "byte";
						}
						if(memberType == typeof(UInt16))
						{
							memberType = typeof(ushort);
							memberTypeName = "ushort";
						}
						if(memberType == typeof(UInt32))
						{
							memberType = typeof(uint);
							memberTypeName = "uint";
						}
						
						size = Marshal.SizeOf(memberType);
						
						textWriter.WriteLine(string.Format("\t\tpublic {0} {1}", memberTypeName, layoutAttribute.Name));
						textWriter.WriteLine("\t\t{");
						textWriter.WriteLine("\t\t\tget");
						textWriter.WriteLine("\t\t\t{");
						textWriter.WriteLine(string.Format("\t\t\t\treturn _tree.Read{0}(_position + {1});", memberType.Name, offset));
						textWriter.WriteLine("\t\t\t}");
						textWriter.WriteLine("\t\t\tset");
						textWriter.WriteLine("\t\t\t{");
						textWriter.WriteLine(string.Format("\t\t\t\t_tree.Write(_position + {0}, value);", offset));
						textWriter.WriteLine("\t\t\t}");
						textWriter.WriteLine("\t\t}");
					}
				}
				
				if(layoutAttributes.Count(x=>x.Type.IsSubclassOf(typeof(TSTreeStreamEntry))) > 0)
				{
					textWriter.WriteLine();
					textWriter.WriteLine("\t\tpublic override int Position");
					textWriter.WriteLine("\t\t{");
					textWriter.WriteLine("\t\t\tget");
					textWriter.WriteLine("\t\t\t{");
					textWriter.WriteLine("\t\t\t\treturn _position;");
					textWriter.WriteLine("\t\t\t}");
					textWriter.WriteLine("\t\t\tset");
					textWriter.WriteLine("\t\t\t{");
					textWriter.WriteLine("\t\t\t\t_position = value;");

					nextOffset = 0;
					offset = 0;
					
					foreach(TSLayoutAttribute layoutAttribute in layoutAttributes)
					{
						var size = SizeOf(layoutAttribute.Type, false);
						offset = nextOffset;
						nextOffset = ComputeOffset(ref offset, size);
						
						if(layoutAttribute.Type.IsSubclassOf(typeof(TSTreeStreamEntry)))
						{
							textWriter.WriteLine(string.Format("\t\t\t\t{0}.Position = _position + {1};", layoutAttribute.Name, offset));
						}
					}
					
					textWriter.WriteLine("\t\t\t}");
					textWriter.WriteLine("\t\t}");
				}
				
				textWriter.WriteLine();
				textWriter.WriteLine("\t\tpublic override void Init(ITSTreeStream tree)");
				textWriter.WriteLine("\t\t{");
				textWriter.WriteLine("\t\t\t_tree = tree;");
				
				foreach(TSLayoutAttribute layoutAttribute in layoutAttributes)
				{
					if(layoutAttribute.Type.IsSubclassOf(typeof(TSTreeStreamEntry)))
					{
						textWriter.WriteLine(string.Format("\t\t\t{0}.Init(_tree);", layoutAttribute.Name));
					}
				}
				
				textWriter.WriteLine("\t\t}");
				textWriter.WriteLine();
				textWriter.WriteLine("\t\tpublic override int SizeOf");
				textWriter.WriteLine("\t\t{");
				textWriter.WriteLine("\t\t\tget");
				textWriter.WriteLine("\t\t\t{");
				textWriter.WriteLine(string.Format("\t\t\t\treturn {0};", SizeOf(type, true)));
				textWriter.WriteLine("\t\t\t}");
				textWriter.WriteLine("\t\t}");
				textWriter.WriteLine("\t}");
				textWriter.WriteLine("}");
				textWriter.Flush();
			}
			
			var structLayoutFileName = string.Format("{0}/Generated/{1}StructLayout.cs", output, type.Name);
			Directory.CreateDirectory(Path.GetDirectoryName(structLayoutFileName));
			using(var structLayout = new FileStream(structLayoutFileName, FileMode.Create))
			{
				var textWriter = new StreamWriter(structLayout);
				
				if(!type.IsPublic || !type.IsSubclassOf(typeof(TSTreeStreamEntry)))
				{
					throw new Exception("Type not as expected");
				}
				
				
				textWriter.WriteLine("using System.Runtime.InteropServices;");
				textWriter.WriteLine(string.Format("namespace {0}",type.Namespace));
				textWriter.WriteLine("{");
				textWriter.WriteLine(string.Format("\t[StructLayout(LayoutKind.Explicit, Size={0})]", SizeOf(type, true)));
				textWriter.WriteLine(string.Format("\tpublic struct {0}StructLayout", type.Name));
				textWriter.WriteLine("\t{");
			
				var offset = 0;
				var nextOffset = 0;
				
				var layoutAttributes = type.GetCustomAttributes(typeof(TSLayoutAttribute), false).Cast<TSLayoutAttribute>().OrderBy(x=>x.Order);
				foreach(TSLayoutAttribute layoutAttribute in layoutAttributes)
				{
					var size = SizeOf(layoutAttribute.Type, false);
					offset = nextOffset;
					nextOffset = ComputeOffset(ref offset, size);
					
					if(layoutAttribute.Type.IsSubclassOf(typeof(TSTreeStreamEntry)))
					{
						textWriter.WriteLine(string.Format("\t\t[FieldOffset({0})]", offset));
						textWriter.WriteLine(string.Format("\t\tpublic {0}StructLayout {1};", layoutAttribute.Type.FullName, layoutAttribute.Name));
					}
					else if(layoutAttribute.Type.IsValueType)
					{
						var memberType = layoutAttribute.Type;
						var memberTypeName = memberType.Name;
						
						if(memberType == typeof(Single))
						{
							memberType = typeof(float);
							memberTypeName = "float";
						}
						if(memberType == typeof(Int32))
						{
							memberType = typeof(int);
							memberTypeName = "int";
						}
						if(memberType == typeof(Byte))
						{
							memberType = typeof(byte);
							memberTypeName = "byte";
						}
						if(memberType == typeof(UInt16))
						{
							memberType = typeof(ushort);
							memberTypeName = "ushort";
						}
						if(memberType == typeof(UInt32))
						{
							memberType = typeof(uint);
							memberTypeName = "uint";
						}
						
						textWriter.WriteLine(string.Format("\t\t[FieldOffset({0})]", offset));
						textWriter.WriteLine(string.Format("\t\tpublic {0} {1};", memberTypeName, layoutAttribute.Name));
					}
				}
				
				textWriter.WriteLine("\t}");
				textWriter.WriteLine("}");
				textWriter.Flush();
			}
		}
		
		public static int SizeOf(Type type, bool align)
		{
			var size = 0;
			if(type.IsValueType)
			{
				return Marshal.SizeOf(type);
			}
			
			foreach(var val in OffsetBuilder(0, type))
			{
				if(!(val is OffsetProperty))
				{
					size = (int)val;
					break;
				}
			}
			
			if(align)
			{
				size = ((int)Math.Ceiling(size/(float)TSTreeStream<TSObject>.Alignment)) * TSTreeStream<TSObject>.Alignment;
			}
			return size;
		}
	
		private static IEnumerable OffsetBuilder(int offset, Type type)
		{
			var start = offset;
			var nextOffset = 0;
			foreach(TSLayoutAttribute layoutAttribute in type.GetCustomAttributes(typeof(TSLayoutAttribute), false).Cast<TSLayoutAttribute>().OrderBy(x=>x.Order))
			{
				if(layoutAttribute.Type.IsSubclassOf(typeof(TSTreeStreamEntry)) && layoutAttribute.Type.GetCustomAttributes(typeof(TSLayoutAttribute), false).Length > 0)
				{
					foreach(var val in OffsetBuilder(offset, layoutAttribute.Type))
					{
						if(!(val is OffsetProperty))
						{
							nextOffset = ComputeOffset(ref offset, (int)val);
							break;
						}
					}
					foreach(var val in OffsetBuilder(offset, layoutAttribute.Type))
					{
						var offsetProperty = val as OffsetProperty;
						if(offsetProperty != null)
						{
							yield return new OffsetProperty(string.Format("{0}_{1}", layoutAttribute.Name, offsetProperty.Name), offsetProperty.Offset, offsetProperty.Type);
						}
					}
					offset = nextOffset;
				}
				else if(layoutAttribute.Type.IsValueType)
				{
					nextOffset = ComputeOffset(ref offset, Marshal.SizeOf(layoutAttribute.Type));
					yield return new OffsetProperty(string.Format("{0}", layoutAttribute.Name), offset, layoutAttribute.Type);
					offset = nextOffset;
				}
			}
			yield return nextOffset - start;
		}
		
		public static int ComputeOffset(ref int offset, int size)
		{
			var bucket0 = (int)(offset/(float)TSTreeStream<TSObject>.Alignment);
			var bucket1 = (int)((offset + size - 1)/(float)TSTreeStream<TSObject>.Alignment);
			if(bucket0 != bucket1)
			{
				offset = ((int)Math.Ceiling(offset/(float)TSTreeStream<TSObject>.Alignment)) * TSTreeStream<TSObject>.Alignment;
			}
			return offset + size;
		}
	}
}