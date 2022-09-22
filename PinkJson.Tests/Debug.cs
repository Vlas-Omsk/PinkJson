#if DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PinkJson;
using System.Threading;
using System.Diagnostics;

namespace json
{
    class Test
    {
        [JsonProperty("problems")]
        string lol;
    }

    class Debug
    {
        static void Main()
        {
            var json = new Json(File.ReadAllText("json.json"));
            var test = Json.ToObject<Test>(json);
            json = Json.FromObject(test, true);

            var str = json.ToFormatString();
            Console.WriteLine(str);

            Console.ReadLine();
        }
    }
}
#endif