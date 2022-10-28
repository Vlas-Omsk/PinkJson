using System;

namespace PinkJson2
{
    public class UnexpectedJsonEnumerableItemException : PinkJsonException
    {
        public UnexpectedJsonEnumerableItemException(JsonEnumerableItem item, JsonEnumerableItemType[] expectedItemTypes) :
            base($"Unexpectedt token {item.Type} expected {string.Join(", ", expectedItemTypes)}")
        {
            Item = item;
        }

        public JsonEnumerableItem Item { get; }
    }
}
