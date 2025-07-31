namespace Nevion.TagAudioDialog
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Threading;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.VirtualFunctions;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	internal class TagAudioDialog : Dialog
	{
		private readonly IEngine engine;

		public TagAudioDialog(IEngine engine) : base(engine)
		{
			this.engine = engine;

			Title = "Change TAG PID";
			ChannelAudioEncodingLabel = new Label("Channel Audio Encoding:");
			ChannelAudioEncodingDropDown = new DropDown();
			ChangeAudioButton = new Button("Change Audio");
			CancelButton = new Button("Cancel");
			int layoutPosition = 0;

			AddWidget(ChannelAudioEncodingLabel, layoutPosition, 0);
			AddWidget(ChannelAudioEncodingDropDown, layoutPosition, 1);
			AddWidget(ChangeAudioButton, ++layoutPosition, 1, HorizontalAlignment.Right);
			AddWidget(CancelButton, layoutPosition, 0, HorizontalAlignment.Left);

			ChannelAudioEncodingLabel.Width = 150;
			ChannelAudioEncodingDropDown.Width = 300;
			ChangeAudioButton.Width = 150;
			CancelButton.Width = 150;
		}

		public Label ChannelAudioEncodingLabel { get; private set; }

		public DropDown ChannelAudioEncodingDropDown { get; private set; }

		public Button ChangeAudioButton { get; private set; }

		public Button CancelButton { get; private set; }

		public void Initialize(string elementName, string channelName)
		{
			var dms = engine.GetDms();
			var tagElement = dms.GetElement(elementName);
			var tagEngineElement = engine.FindElement(elementName);

			string outputPidName = RemoveBracketPrefix(channelName) + "/1";

			var channelPidRows = tagElement.GetTable(2500).GetRows();
			var audioRowsForChannel = channelPidRows.Where(x => Convert.ToInt32(x[2]) == 2 /*Audio*/ && Convert.ToString(x[29]).Contains(channelName));

			var audioDisplays = audioRowsForChannel.Select(x =>
			{
				var language = Convert.ToString(x[17]);
				var audioKey = Convert.ToString(x[0]);
				var match = Regex.Match(audioKey, @"Audio/(\d+)", RegexOptions.IgnoreCase);
				var audioId = match.Success ? match.Groups[1].Value : "N/A";
				var pid = Convert.ToString(x[1]);

				int audioFormatCode = 0;
				if (x[19] != null)
				{
					int.TryParse(Convert.ToString(x[19]), out audioFormatCode);
				}

				string audioFormat;
				if (audioFormatCode == 6)
				{
					audioFormat = "Surround (5.1)";
				}
				else if (audioFormatCode == 2)
				{
					audioFormat = "Stereo (2.0)";
				}
				else
				{
					audioFormat = $"Unknown ({audioFormatCode})";
				}

				return $"{language.ToUpper()} - Aud({audioId}) PID {pid} - {audioFormat}";
			});

			this.ChannelAudioEncodingDropDown.Options = audioDisplays;

			var outputPidsTable = tagElement.GetTable(3300);
			var outputPidsFilter = new List<ColumnFilter> { new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Pid = 3302, Value = outputPidName } };
			var outputPidsMatchingRow = outputPidsTable.QueryData(outputPidsFilter).First();
			var outputPidKey = Convert.ToString(outputPidsMatchingRow[0]);

			this.ChangeAudioButton.Pressed += (sender, args) =>
			{
				var selectedPid = this.ChannelAudioEncodingDropDown.Selected;
				var match = Regex.Match(selectedPid, @"Aud\((\d+)\)\s+PID\s+(\d+)");
				engine.GenerateInformation($"selected pid: {selectedPid}");
				string audioId = match.Groups[1].Value;
				string pid = match.Groups[2].Value;

				tagEngineElement.SetParameterByPrimaryKey(3356, outputPidKey, channelName);
				tagEngineElement.SetParameterByPrimaryKey(3357, outputPidKey, audioId);
				tagEngineElement.SetParameterByPrimaryKey(3358, outputPidKey, pid);
				tagEngineElement.SetParameterByPrimaryKey(3360, outputPidKey, pid);
				engine.ExitSuccess("Finished");
			};
		}

		public static string RemoveBracketPrefix(string input)
		{
			if (String.IsNullOrWhiteSpace(input))
			{
				return input;
			}

			int closingBracketIndex = input.IndexOf(']');
			if (input.StartsWith("[") && closingBracketIndex != -1)
			{
				return input.Substring(closingBracketIndex + 1).TrimStart();
			}

			return input;
		}
	}
}