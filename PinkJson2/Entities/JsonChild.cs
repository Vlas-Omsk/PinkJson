using System;

namespace PinkJson2
{
    public abstract class JsonChild : IJson
    {
        public object Value { get; set; }

        internal JsonChild()
        {
        }

        public IJson this[string key]
        {
            get
            {
                if (Value is IJson)
                    return (Value as IJson)[key];
                throw new InvalidObjectTypeException(typeof(IJson));
            }
            set
            {
                if (Value is IJson)
                    (Value as IJson)[key] = value;
                throw new InvalidObjectTypeException(typeof(IJson));
            }
        }

        public IJson this[int index]
        {
            get
            {
                if (Value is IJson)
                    return (Value as IJson)[index];
                throw new InvalidObjectTypeException(typeof(IJson));
            }
            set
            {
                if (Value is IJson)
                    (Value as IJson)[index] = value;
                throw new InvalidObjectTypeException(typeof(IJson));
            }
        }

        public int IndexOfKey(string key)
        {
            if (Value is IJson)
                return (Value as IJson).IndexOfKey(key);
            throw new InvalidObjectTypeException(typeof(IJson));
        }

        public override string ToString()
        {
            return new MinifiedFormatter().Format(this);
        }
    }
}
