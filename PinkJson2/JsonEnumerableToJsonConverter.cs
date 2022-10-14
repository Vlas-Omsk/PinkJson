using System;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2
{
    public sealed class JsonEnumerableToJsonConverter
    {
        private IEnumerator<JsonEnumerableItem> _enumerator;

        public IJson Convert(IEnumerable<JsonEnumerableItem> enumerable)
        {
            IJson json;

            _enumerator = enumerable.GetEnumerator();
            _enumerator.MoveNext();

            switch (_enumerator.Current.Type)
            {
                case JsonEnumerableItemType.ObjectBegin:
                    json = ConvertObject();
                    break;
                case JsonEnumerableItemType.ArrayBegin:
                    json = ConvertArray();
                    break;
                case JsonEnumerableItemType.Key:
                    json = ConvertKeyValue();
                    break;
                case JsonEnumerableItemType.Value:
                    json = new JsonArrayValue(_enumerator.Current.Value);
                    break;
                default:
                    throw new Exception();
            }

            _enumerator.Dispose();

            return json;
        }

        private object ConvertValue()
        {
            switch (_enumerator.Current.Type)
            {
                case JsonEnumerableItemType.ObjectBegin:
                    return ConvertObject();
                case JsonEnumerableItemType.ArrayBegin:
                    return ConvertArray();
                case JsonEnumerableItemType.Key:
                    return ConvertKeyValue();
                case JsonEnumerableItemType.Value:
                    return _enumerator.Current.Value;
                default:
                    throw new Exception();
            }
        }

        private JsonKeyValue ConvertKeyValue()
        {
            var key = (string)_enumerator.Current.Value;
            TryMoveNext(JsonEnumerableItemType.ObjectBegin, JsonEnumerableItemType.ArrayBegin, JsonEnumerableItemType.Value);
            var value = ConvertValue();

            return new JsonKeyValue(key, value);
        }

        private JsonObject ConvertObject()
        {
            var json = new JsonObject();

            while (true)
            {
                TryMoveNext(JsonEnumerableItemType.Key, JsonEnumerableItemType.ObjectEnd);
                if (_enumerator.Current.Type == JsonEnumerableItemType.ObjectEnd)
                    break;
                json.Add(ConvertKeyValue());
            }

            return json;
        }

        private JsonArray ConvertArray()
        {
            var json = new JsonArray();

            while (true)
            {
                TryMoveNext(JsonEnumerableItemType.ObjectBegin, JsonEnumerableItemType.ArrayBegin, JsonEnumerableItemType.Value, JsonEnumerableItemType.ArrayEnd);
                if (_enumerator.Current.Type == JsonEnumerableItemType.ArrayEnd)
                    break;
                json.Add(new JsonArrayValue(ConvertValue()));
            }

            return json;
        }

        private void TryMoveNext(params JsonEnumerableItemType[] expectedItemTypes)
        {
            if (!_enumerator.MoveNext())
                throw new Exception();

            if (!expectedItemTypes.Contains(_enumerator.Current.Type))
                throw new Exception();
        }
    }
}
