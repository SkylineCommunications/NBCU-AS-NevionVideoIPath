namespace NevionSharedUtils
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;
	using System.Threading;

	public static class Utils
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
	}

	public static class NevionIds
	{
		public static readonly int CurrentServicesTable = 1500;
		public static readonly int CurrentServicesDestinationId = 1508;
		public static readonly int CurrentServicesCancelButton = 1515;
		public static readonly int CurrentServicesIndexIdx = 0;
		public static readonly int CurrentServicesDestinationNamesIdx = 3;
	}

	public static class TagIds
	{
		public static readonly int ChannelTable = 2100;
	}
}