using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace PinkJson2.Tests
{
    public static class ParserBenchmark
    {
		public static void Start()
        {
			// new: 9 ms
			JsonParserPerformanceTest("Json\\test1.json");
			Console.WriteLine();

			// old: 157 ms
			// new: 83 ms
			JsonParserPerformanceTest("Json\\discord_detectable.json");
			Console.WriteLine();
		}

		private static void JsonParserPerformanceTest(string fileName)
		{
			using (var md5 = MD5.Create())
			using (var stream = File.OpenRead(fileName))
				Console.WriteLine(string.Concat(md5.ComputeHash(stream).Select(x => x.ToString("X2"))));

			var sw = Stopwatch.StartNew();

			using (var stream = File.OpenText(fileName))
				Json.Parse(stream);

			sw.Stop();

			long objectsCount;

			using (var stream = File.OpenText(fileName))
			{
				var json = Json.Parse(stream);
				objectsCount = GetLength(json as IEnumerable<IJson>);
			}

			var fileInfo = new FileInfo(fileName);

			Console.WriteLine("Elapsed " + sw.ElapsedMilliseconds + " ms");
			Console.WriteLine("File length " + fileInfo.Length + " symbols");
			Console.WriteLine("Json objects count " + objectsCount);
		}

		private static long GetLength(IEnumerable<IJson> json)
		{
			long length = 0;
			foreach (var item in json)
			{
				if (item.Value is IEnumerable<IJson> list)
					length += GetLength(list);
				length++;
			}
			return length;
		}
	}
}
