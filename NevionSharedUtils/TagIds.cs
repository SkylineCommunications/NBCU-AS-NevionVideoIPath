namespace NevionSharedUtils
{
	public class TAGMCSIds
	{
		public class AllLayoutsTable
		{
			public static readonly int TablePid = 5600;

			public class Idx
			{
				public static readonly int Index = 0;
				public static readonly int Title = 2;
				public static readonly int DisplayKey = 11;
			}

			public class Pid
			{
				public static readonly int Title = 5603;
				public static readonly int Layout = 5605;
				public static readonly int DisplayKey = 5612;
			}
		}

		public class ChannelConfigTable
		{
			public static readonly int TablePid = 2100;
		}

		public class LayoutTable
		{
			public static readonly int TablePid = 3600;
		}

		public class ChannelPidsTable
		{
			public static readonly int TablePid = 2500;

			public enum ChannelPidsType
			{
				Audio = 2,
			}

			public class Idx
			{
				public static readonly int Index = 0;
				public static readonly int PID = 1;
				public static readonly int Type = 2;
				public static readonly int TransportMonitored = 3;
				public static readonly int ContentMonitored = 4;
				public static readonly int OutOfPMT = 5;
				public static readonly int Scramble = 6;
				public static readonly int PCRonPID = 7;
				public static readonly int BitrateMin = 8;
				public static readonly int BitrateMax = 9;
				public static readonly int DescriptorsHash = 10;
				public static readonly int VideoCodec = 11;
				public static readonly int Resolution = 12;
				public static readonly int AspectRatio = 13;
				public static readonly int ProfileLevel = 14;
				public static readonly int ColorSpace = 15;
				public static readonly int FrameRate = 16;
				public static readonly int Language = 17;
				public static readonly int AudioCodec = 18;
				public static readonly int AudioChannels = 19;
				public static readonly int DialNorm = 20;
				public static readonly int SampleRate = 21;
				public static readonly int Page = 22;
				public static readonly int VendorID = 23;
				public static readonly int PrivateData = 24;
				public static readonly int BitPerSample = 25;
				public static readonly int AESCodec = 26;
				public static readonly int AESChannels = 27;
				public static readonly int ProfileID = 28;
				public static readonly int DisplaysName = 29;
			}

			public class Pid
			{
				public static readonly int Index = 2501;
				public static readonly int PID = 2502;
				public static readonly int Type = 2503;
				public static readonly int TransportMonitored = 2504;
				public static readonly int ContentMonitored = 2505;
				public static readonly int OutOfPMT = 2506;
				public static readonly int Scramble = 2507;
				public static readonly int PCRonPID = 2508;
				public static readonly int BitrateMin = 2509;
				public static readonly int BitrateMax = 2510;
				public static readonly int DescriptorsHash = 2511;
				public static readonly int VideoCodec = 2512;
				public static readonly int Resolution = 2513;
				public static readonly int AspectRatio = 2514;
				public static readonly int ProfileLevel = 2515;
				public static readonly int ColorSpace = 2516;
				public static readonly int FrameRate = 2517;
				public static readonly int Language = 2518;
				public static readonly int AudioCodec = 2519;
				public static readonly int AudioChannels = 2520;
				public static readonly int DialNorm = 2521;
				public static readonly int SampleRate = 2522;
				public static readonly int Page = 2523;
				public static readonly int VendorID = 2524;
				public static readonly int PrivateData = 2525;
				public static readonly int BitPerSample = 2526;
				public static readonly int AESCodec = 2527;
				public static readonly int AESChannels = 2528;
				public static readonly int ProfileID = 2529;
				public static readonly int DisplaysName = 2530;
			}
		}

		public static class OutputAudiosTable
		{
			public static readonly int TablePid = 3300;

			public static class Idx
			{
				public static readonly int Index = 0;
				public static readonly int Label = 1;
				public static readonly int OutputID = 2;
				public static readonly int Output = 3;
				public static readonly int ChannelID = 4;
				public static readonly int Channel = 5;
				public static readonly int InputIndex = 6;
				public static readonly int InputPID = 7;
				public static readonly int Mode = 8;
				public static readonly int OutputPID = 9;
				public static readonly int OutputMask = 10;
			}

			public static class Pid
			{
				public static readonly int Index = 3301;
				public static readonly int Label = 3302;
				public static readonly int OutputID = 3303;
				public static readonly int Output = 3304;
				public static readonly int ChannelID = 3305;
				public static readonly int Channel = 3306;
				public static readonly int InputIndex = 3307;
				public static readonly int InputPID = 3308;
				public static readonly int Mode = 3309;
				public static readonly int OutputPID = 3310;
				public static readonly int OutputMask = 3311;
			}
		}

	}
}
