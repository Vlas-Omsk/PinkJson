namespace PinkJson2
{
    // This type is only used for json serialization when calling TypeConverter.ChangeType from ISerializable
    public readonly struct SerializedJsonValue
    {
        public SerializedJsonValue(object value)
        {
            Value = value;
        }

        public object Value { get; }
    }
}
