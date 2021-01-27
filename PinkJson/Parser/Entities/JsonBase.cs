using PinkJson.Lexer;
using PinkJson.Lexer.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Dynamic;
using System.Reflection;

namespace PinkJson
{
    public abstract class JsonBase<T> : ObjectBase, IList<T> where T : ObjectBase
    {
        public JsonBase()
        {
            Collection = new List<T>();
        }

        public JsonBase(IEnumerable<T> jsonObjects)
        {
            if (jsonObjects != null)
                Collection = new List<T>(jsonObjects);
            else
                Collection = new List<T>();
        }

        protected List<T> Collection;
        public abstract override object Value { get; set; }

        public int Count => Collection.Count;
        public bool IsReadOnly => false;

        public int IndexOf(T jsonObject)
        {
            return Collection.IndexOf(jsonObject);
        }

        public void Insert(int index, T jsonObject)
        {
            Collection.Insert(index, jsonObject);
        }

        public void RemoveAt(int index)
        {
            Collection.RemoveAt(index);
        }

        public void Add(T jsonObject)
        {
            Collection.Add(jsonObject);
        }

        public void Clear()
        {
            Collection.Clear();
        }

        public bool Contains(T jsonObject)
        {
            return Collection.Contains(jsonObject);
        }

        public void CopyTo(T[] jsonObjects, int arrayIndex)
        {
            Collection.CopyTo(jsonObjects, arrayIndex);
        }

        public bool Remove(T jsonObject)
        {
            return Collection.Remove(jsonObject);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        public new T this[int index]
        {
            get => Collection[index];
            set => Collection[index] = value;
        }
    }
}
