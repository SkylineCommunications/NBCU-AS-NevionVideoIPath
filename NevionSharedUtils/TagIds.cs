namespace NevionSharedUtils
{
	public class TAGMCSIds
	{
		public static class AllLayoutChannelsTable
		{
			public static readonly int TablePid = 5600;

			public static class Idx
			{
				public static readonly int Index = 0;
				public static readonly int ChannelSourceId = 1;
				public static readonly int ChannelTitle = 2;
				public static readonly int LayoutID = 3;
				public static readonly int Layout = 4;
				public static readonly int Position = 5;
				public static readonly int UMD1 = 6;
				public static readonly int UMD2 = 7;
				public static readonly int UMD3 = 8;
				public static readonly int UMD4 = 9;
				public static readonly int Tags = 10;
				public static readonly int DisplayKey = 11;
			}

			public static class Pid
			{
				public static readonly int Index = 5601;
				public static readonly int ChannelSource = 5602;
				public static readonly int Title = 5603;
				public static readonly int LayoutID = 5604;
				public static readonly int Layout = 5605;
				public static readonly int Position = 5606;
				public static readonly int UMD1 = 5607;
				public static readonly int UMD2 = 5608;
				public static readonly int UMD3 = 5609;
				public static readonly int UMD4 = 5610;
				public static readonly int Tags = 5611;
				public static readonly int DisplayKey = 5612;
			}
		}

		public class ChannelConfigTable
		{
			public static readonly int TablePid = 2100;

			public static class Pid
			{
				public static readonly int Id = 2101;
				public static readonly int Label = 2102;
			}

			public static class Idx
			{
				public static readonly int Id = 0;
				public static readonly int Label = 1;
			}
		}

		public class OutputConfigTable
		{
			public static readonly int TablePid = 3100;

			public static class Pid
			{
				public static readonly int Index = 3101;
				public static readonly int Label = 3102;
				public static readonly int DeviceId = 3103;
				public static readonly int Device = 3104;
				public static readonly int AudioAgent = 3105;
				public static readonly int ShowAlarms = 3106;
				public static readonly int DisplayOnInputLoss = 3107;
				public static readonly int AudioDisplayMode = 3108;
				public static readonly int FrameRate = 3109;
				public static readonly int Resolution = 3110;
				public static readonly int Interlacing = 3111;
				public static readonly int Deblocking = 3112;
				public static readonly int Downscale4K = 3113;
				public static readonly int DownrateFps = 3114;
				public static readonly int CodecType = 3115;
				public static readonly int GOPSize = 3116;
				public static readonly int GOPMode = 3117;
				public static readonly int VBVLimit = 3118;
				public static readonly int VideoBitrate = 3119;
				public static readonly int JSXVideoBitrate = 3120;
				public static readonly int MINChunkLength = 3121;
				public static readonly int NumberOfChunks = 3122;
				public static readonly int IndexGenerationalInterval = 3123;
				public static readonly int ProgramNumber = 3124;
				public static readonly int TransportStreamId = 3125;
				public static readonly int PTMPid = 3126;
				public static readonly int VideoPIP = 3127;
				public static readonly int LayoutCycleInterval = 3128;
				public static readonly int JSON = 3129;
				public static readonly int HVECLatencyMode = 3130;
			}

			public static class Idx
			{
				public static readonly int Index = 0;
				public static readonly int Label = 1;
				public static readonly int DeviceId = 2;
				public static readonly int Device = 3;
				public static readonly int AudioAgent = 4;
				public static readonly int ShowAlarms = 5;
				public static readonly int DisplayOnInputLoss = 6;
				public static readonly int AudioDisplayMode = 7;
				public static readonly int FrameRate = 8;
				public static readonly int Resolution = 9;
				public static readonly int Interlacing = 10;
				public static readonly int Deblocking = 11;
				public static readonly int Downscale4K = 12;
				public static readonly int DownrateFps = 13;
				public static readonly int CodecType = 14;
				public static readonly int GOPSize = 15;
				public static readonly int GOPMode = 16;
				public static readonly int VBVLimit = 17;
				public static readonly int VideoBitrate = 18;
				public static readonly int JSXVideoBitrate = 19;
				public static readonly int MINChunkLength = 20;
				public static readonly int NumberOfChunks = 21;
				public static readonly int IndexGenerationalInterval = 22;
				public static readonly int ProgramNumber = 23;
				public static readonly int TransportStreamId = 24;
				public static readonly int PTMPid = 25;
				public static readonly int VideoPIP = 26;
				public static readonly int LayoutCycleInterval = 27;
				public static readonly int JSON = 28;
				public static readonly int HVECLatencyMode = 29;
			}
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
				NA = -1,

				Other = 0,

				Video = 1,

				Audio = 2,

				Subtitles = 3,

				Teletext = 4,

				PCR = 5,

				PMT = 6,

				ECM = 7,

				EMM = 8,

				DSM_CC = 9,

				AES3 = 10,

				ID3_Metadata = 11,

				ST_2038 = 12,

				Metadata = 13,

				AIT = 14,

				PAT = 15,

				SDT = 16,

				EIT = 17,

				CAT = 18,

				AES67 = 19,

				NIT = 20,

				TDT = 21,

				ID3 = 22,

				ETT = 23,

				ATSC_Tables = 24,
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
				public static readonly int ChannelId = 30;
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
				public static readonly int ChannelId = 2531;
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

		public static class OutputsLayoutsTable
		{
			public static readonly int TablePid = 3400;

			public static class Idx
			{
				public static readonly int Index = 0;
				public static readonly int LabelIDX = 1;
				public static readonly int OutputID = 2;
				public static readonly int Output = 3;
				public static readonly int LayoutID = 4;
				public static readonly int Layout = 5;
			}

			public static class Pid
			{
				public static readonly int Index = 3401;
				public static readonly int LabelIDX = 3402;
				public static readonly int OutputID = 3403;
				public static readonly int Output = 3404;
				public static readonly int LayoutID = 3405;
				public static readonly int Layout = 3406;
			}
		}

		public static class ChannelStatusComponentsTable
		{
			public static readonly int TablePid = 7100;

			public static class Idx
			{
				public static readonly int Id = 0;
				public static readonly int DisplayKey = 1;
				public static readonly int PID = 2;
				public static readonly int Bitrate = 3;
				public static readonly int Monitored = 4;
				public static readonly int ContentType = 5;
				public static readonly int Index = 6;
				public static readonly int ChannelID = 7;
				public static readonly int ParentPid = 8;
			}

			public static class Pid
			{
				public static readonly int Id = 7101;
				public static readonly int DisplayKey = 7102;
				public static readonly int PID = 7103;
				public static readonly int Bitrate = 7104;
				public static readonly int Monitored = 7105;
				public static readonly int ContentType = 7106;
				public static readonly int Index = 7107;
				public static readonly int ChannelID = 7108;
				public static readonly int ParentPid = 7109;
			}
		}
	}
}