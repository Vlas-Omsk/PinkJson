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

        private enum TestType
        {
            Test1,
            Test2,
            TestTest,
            Unknown
        }

        private class Packet
        {
            [JsonProperty(Name = "type1")]
            public TestType Type1 { get; set; }
            [JsonProperty(Name = "type2")]
            public TestType Type2 { get; set; }
            [JsonProperty(Name = "type3")]
            public TestType Type3 { get; set; }
        }

        [Fact]
        public void RegisterConversionsTest()
        {
            TypeConverter.Default.Register(typeof(TestType), new TypeConversion(TypeConversionDirection.ToType, (object obj, Type targetType, ref bool handled) =>
            {
                if (obj is string @string)
                {
                    @string = Regex.Replace(@string, "(^.|_.)", x =>
                    {
                        var ch = x.Value[0] == '_' ? x.Value[1] : x.Value[0];
                        return char.ToUpper(ch).ToString();
                    });

                    if (!Enum.TryParse(@string, out TestType type))
                        type = TestType.Unknown;

                    handled = true;
                    return type;
                }

                return null;
            }));

            var obj = new JsonObject(
                new JsonKeyValue("type1", "value"), 
                new JsonKeyValue("type2", "test2"), 
                new JsonKeyValue("type3", "test_test")
            );
            var packet = obj.Deserialize<Packet>();

            Assert.Equal(TestType.Unknown, packet.Type1);
            Assert.Equal(TestType.Test2, packet.Type2);
            Assert.Equal(TestType.TestTest, packet.Type3);
        }

        [Fact]
        public void RegisterConversions2Test()
        {
            TypeConverter.Default.Register(typeof(string), new TypeConversion(TypeConversionDirection.FromType, (object obj, Type targetType, ref bool handled) =>
            {
                if (targetType != typeof(TestType))
                    return null;

                if (obj is string @string)
                {
                    @string = Regex.Replace(@string, "(^.|_.)", x =>
                    {
                        var ch = x.Value[0] == '_' ? x.Value[1] : x.Value[0];
                        return char.ToUpper(ch).ToString();
                    });

                    if (!Enum.TryParse(@string, out TestType type))
                        type = TestType.Unknown;

                    handled = true;
                    return type;
                }

                return null;
            }));

            var obj = new JsonObject(
                new JsonKeyValue("type1", "value"), 
                new JsonKeyValue("type2", "test2"), 
                new JsonKeyValue("type3", "test_test")
            );
            var packet = obj.Deserialize<Packet>();

            Assert.Equal(TestType.Unknown, packet.Type1);
            Assert.Equal(TestType.Test2, packet.Type2);
            Assert.Equal(TestType.TestTest, packet.Type3);
        }
    }
}
