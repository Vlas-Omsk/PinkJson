namespace PinkJson2.Formatters
{
    // This type is only used for formatting when calling TypeConverter.ChangeType from ValueFormatter
    public readonly struct FormattedValue
    {
        public FormattedValue(object value)
        {
            Value = value;
        }

        public object Value { get; }
    }
}
