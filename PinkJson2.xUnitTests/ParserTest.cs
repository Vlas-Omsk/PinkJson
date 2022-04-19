using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PinkJson2.xUnitTests
{
    public class ParserTest
    {
        [Fact]
        public void DateTimeTest()
        {
            var array = new JsonArray();
            array.AddValueLast(new DateTime(2000, 5, 23).ToISO8601String());

            var str = array.ToString();

            var json = Json.Parse(str);

            Assert.Equal(json[0].Get<DateTime>(), new DateTime(2000, 5, 23));
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
        public void TypeConversionTest()
        {
            var json = Json.Parse(@"{
			  'Name': 'Bad Boys',
			  'ReleaseDate': '1995-4-7T00:00:00',
			  'Genres': [
				'Action',
				'Comedy'
			  ]
			}".Replace('\'', '"'));

            var m = json.Deserialize<Movie>();

            Assert.Equal(DateTime.Parse("1995-4-7T00:00:00"), m.ReleaseDate);
            Assert.IsType<string[]>(m.Genres);
            Assert.Equal("Action", m.Genres.ElementAt(0));
            Assert.Equal("Comedy", m.Genres.ElementAt(1));
        }
    }
}
