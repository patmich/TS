
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace LLT
{
	[Serializable]
	public abstract class TSTreeStream<O> : IDisposable, ITSTreeStream
		where O : class, ITSObject, new()
	{
		public const int Alignment = 4;

		private ITSTextAsset _textAsset;

		public abstract ITSTreeStreamDFSEnumerator Iter { get; }

		private ITSTreeStream _parent;
		private TSTreeStreamTag _rootTag;

		private readonly List<string> _lookup = new List<string>();
		private List<O> _objects;

		public ITSTextAsset TextAsset
		{
			get
			{
				return _textAsset;
			}
		}

		public virtual ITSTreeStream Parent
		{
			set
			{
				_parent = value;
			}
			get
			{
				return _parent;
			}
		}

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

		public int GetInstanceID()
		{
			throw new NotImplementedException();
		}

		public TSTreeStream(ITSTextAsset textAsset, ICoreStreamable meta, List<O> objects)
		{
			_textAsset = textAsset.GetInstance();
			_objects = objects;

			using(var stream = new MemoryStream(_textAsset.Bytes))
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

				_textAsset.Offset((int)stream.Position);
				_rootTag = CreateTag(0);
			}

			for(var i = 0; i < _objects.Count; i++)
			{
				_objects[i].Init(this);
			}
		}

		public TSTreeStreamTag CreateTag(int position)
		{
			return new TSTreeStreamTag(this, position);
		}
		
		public string GetName(TSTreeStreamTag tag)
		{
			if(tag.NameIndex == ushort.MaxValue)
			{
				return string.Empty;
			}
			
			CoreAssert.Fatal(0 <= tag.NameIndex && tag.NameIndex < _lookup.Count, "tag.NameIndex: " + tag.NameIndex + " " + _lookup.Count);
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
				UnityEngine.Debug.Log(Iter.Current.Position + " " + Iter.Current.ObjectIndex);
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

		public void Dispose ()
		{
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