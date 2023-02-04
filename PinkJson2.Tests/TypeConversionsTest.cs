using PinkJson2.Formatters;
using PinkJson2.KeyTransformers;
using PinkJson2.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace PinkJson2.Examples
{
    public class TypeConversionsTest
    {
        [Fact]
        public void DateTimeTest()
        {
            var array = new JsonArray();
            array.AddValueLast(new DateTime(2000, 5, 23).ToISO8601String());

            var str = array.ToString();

            var json = Json.Parse(str);

            Assert.Equal(json.ToJson()[0].Get<DateTime>(), new DateTime(2000, 5, 23));
        }

        private class Movie
        {
            public string Name { get; set; }
            public DateTime ReleaseDate { get; set; }
            [JsonProperty(DeserializeToType = typeof(string[]))]
            public IEnumerable<string> Genres { get; set; }

            public IList<string> Tags { get; set; }
        }

        [Fact]
        public void ClassMemberTypeConversionsTest()
        {
            var json = Json.Parse(@"{
			  'Name': 'Bad Boys',
			  'ReleaseDate': '1995-4-7T00:00:00',
			  'Genres': [
				'Action',
				'Comedy'
			  ]
			}".Replace('\'', '"')).ToJson();

            var m = json.Deserialize<Movie>();

            Assert.Equal(DateTime.Parse("1995-4-7T00:00:00"), m.ReleaseDate);
            Assert.IsType<string[]>(m.Genres);
            Assert.Equal("Action", m.Genres.ElementAt(0));
            Assert.Equal("Comedy", m.Genres.ElementAt(1));
        }

        private enum TestEnum
        {
            Test1,
            Test2,
            TestTest,
            Unknown
        }

        private class Packet
        {
            [JsonProperty(Name = "type1")]
            public TestEnum Type1 { get; set; }
            [JsonProperty(Name = "type2")]
            public TestEnum Type2 { get; set; }
            [JsonProperty(Name = "type3")]
            public TestEnum Type3 { get; set; }
        }

        [Fact]
        public void ConvertStringToEnumTest()
        {
            var typeConverter = new TypeConverter();

            typeConverter.Register(new TypeConversion(
                typeof(TestEnum),
                TypeConversionDirection.ToType,
                (object obj, Type targetType, ref bool handled) =>
                {
                    if (obj is string @string)
                    {
                        @string = Regex.Replace(@string, "(^.|_.)", x =>
                        {
                            var ch = x.Value[0] == '_' ? x.Value[1] : x.Value[0];
                            return char.ToUpper(ch).ToString();
                        });

                        if (!Enum.TryParse(@string, out TestEnum type))
                            type = TestEnum.Unknown;

                        handled = true;
                        return type;
                    }

                    return null;
                }
            ));

            var obj = new JsonObject(
                new JsonKeyValue("type1", "value"), 
                new JsonKeyValue("type2", "test2"), 
                new JsonKeyValue("type3", "test_test")
            );
            var packet = obj.Deserialize<Packet>(new ObjectSerializerOptions() { TypeConverter = typeConverter });

            Assert.Equal(TestEnum.Unknown, packet.Type1);
            Assert.Equal(TestEnum.Test2, packet.Type2);
            Assert.Equal(TestEnum.TestTest, packet.Type3);
        }

        [Fact]
        public void ConvertEnumFromStringTest()
        {
            var typeConverter = new TypeConverter();

            typeConverter.Register(new TypeConversion(
                typeof(string), 
                TypeConversionDirection.FromType, 
                (object obj, Type targetType, ref bool handled) =>
                {
                    if (targetType != typeof(TestEnum))
                        return null;

                    var str = (string)obj;

                    str = Regex.Replace(str, "(^.|_.)", x =>
                    {
                        var ch = x.Value[0] == '_' ? x.Value[1] : x.Value[0];
                        return char.ToUpper(ch).ToString();
                    });

                    if (!Enum.TryParse(str, out TestEnum type))
                        type = TestEnum.Unknown;

                    handled = true;
                    return type;
                }
            ));

            var obj = new JsonObject(
                new JsonKeyValue("type1", "value"), 
                new JsonKeyValue("type2", "test2"), 
                new JsonKeyValue("type3", "test_test")
            );
            var packet = obj.Deserialize<Packet>(new ObjectSerializerOptions() { TypeConverter = typeConverter });

            Assert.Equal(TestEnum.Unknown, packet.Type1);
            Assert.Equal(TestEnum.Test2, packet.Type2);
            Assert.Equal(TestEnum.TestTest, packet.Type3);
        }

        [Fact]
        public void ConvertStringFromEnumTest()
        {
            var typeConverter = new TypeConverter();
            typeConverter.Register(new TypeConversion(
                typeof(TestEnum),
                TypeConversionDirection.FromType,
                (object obj, Type targetType, ref bool handled) =>
                {
                    if (targetType == typeof(FormattedValue))
                    {
                        var enumName = Enum.GetName((TestEnum)obj);

                        handled = true;
                        return new FormattedValue(new SnakeCaseKeyTransformer().TransformKey(enumName));
                    }

                    return null;
                }
            ));

            var packet = new Packet()
            {
                Type1 = TestEnum.Unknown,
                Type2 = TestEnum.Test2,
                Type3 = TestEnum.TestTest,
            };
            var packetJson = packet.Serialize().ToJsonString(typeConverter);
            var newPacketJson = Json.Parse(packetJson).ToJson();

            Assert.Equal("unknown", newPacketJson["type1"].Value);
            Assert.Equal("test2", newPacketJson["type2"].Value);
            Assert.Equal("test_test", newPacketJson["type3"].Value);
        }

        [Fact]
        public void ConvertEnumToStringTest()
        {
            var typeConverter = new TypeConverter();
            typeConverter.Register(new TypeConversion(
                typeof(FormattedValue),
                TypeConversionDirection.ToType,
                (object obj, Type targetType, ref bool handled) =>
                {
                    if (obj is TestEnum enumValue)
                    {
                        var enumName = Enum.GetName(enumValue);

                        handled = true;
                        return new FormattedValue(new SnakeCaseKeyTransformer().TransformKey(enumName));
                    }

                    return null;
                }
            ));

            var packet = new Packet()
            {
                Type1 = TestEnum.Unknown,
                Type2 = TestEnum.Test2,
                Type3 = TestEnum.TestTest,
            };
            var packetJson = packet.Serialize().ToJsonString(typeConverter);
            var newPacketJson = Json.Parse(packetJson).ToJson();

            Assert.Equal("unknown", newPacketJson["type1"].Value);
            Assert.Equal("test2", newPacketJson["type2"].Value);
            Assert.Equal("test_test", newPacketJson["type3"].Value);
        }
    }
}
