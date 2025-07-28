namespace NevionCommon_1
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;

	using System.Threading;

	using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;

	public class NevionUtils
	{
		public static bool Retry(Func<bool> func, TimeSpan timeout)
		{
			bool success;

			Stopwatch sw = new Stopwatch();
			sw.Start();

			do
			{
				success = func();
				if (!success)
				{
					Thread.Sleep(5000);
				}
			}
			while (!success && sw.Elapsed <= timeout);

			return success;
		}

		public static string GetOneDeserializedValue(string scriptParam)
		{
			if (scriptParam.Count() <= 2)
			{
				return scriptParam;
			}

			if (scriptParam.Contains("[") && scriptParam.Contains("]"))
			{
				return SecureNewtonsoftDeserialization.DeserializeObject<List<string>>(scriptParam)[0];
			}
			else
			{
				return scriptParam;
			}
		}
	}
}
