namespace Nevion.TagAudioDialog
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;

	using NevionCommon_1;

	using NevionSharedUtils;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS;
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS.API_Models;
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS.InterApp.Messages;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	internal class TagAudioDialog : Dialog
	{
		private readonly IEngine engine;
		private readonly IDmsElement tagElement;

		private static Dictionary<string, int?> channelMaskingMap = new Dictionary<string, int?>
		{
			{ "None", null },
			{ "Front Left", 1 },
			{ "Front Right", 2 },
			{ "Center", 3 },
			{ "Low-Frequency Effects", 4 },
			{ "Surround Left", 5 },
			{ "Surround Right", 6 },
		};

		public TagAudioDialog(IEngine engine, string elementName) : base(engine)
		{
			this.engine = engine;
			var dms = engine.GetDms();
			tagElement = dms.GetElement(elementName);

			Title = "Change TAG PID";
			OutputSelectionLabel = new Label("Output:");
			OutputSelectionDropDown = new DropDown();

			ChannelSelectionLabel = new Label("Source:");
			ChannelSelectionDropDown = new DropDown();

			ChannelAudioEncodingLabel = new Label("Channel Audio PID:");
			ChannelAudioEncodingDropDown = new DropDown();

			ChannelAudioMaskLabel = new Label("Audio Mask:");
			ChannelAudioMaskDropDown = new DropDown(new List<string> { "None", "Front Left", "Front Right", "Center", "Low-Frequency Effects", "Surround Left", "Surround Right" });
			ChangeAudioButton = new Button("Change Audio");
			CancelButton = new Button("Cancel");

			int layoutPosition = 0;
			AddWidget(OutputSelectionLabel, layoutPosition, 0);
			AddWidget(OutputSelectionDropDown, layoutPosition, 1);
			AddWidget(ChannelSelectionLabel, ++layoutPosition, 0);
			AddWidget(ChannelSelectionDropDown, layoutPosition, 1);
			AddWidget(ChannelAudioEncodingLabel, ++layoutPosition, 0);
			AddWidget(ChannelAudioEncodingDropDown, layoutPosition, 1);
			AddWidget(ChannelAudioMaskLabel, ++layoutPosition, 0);
			AddWidget(ChannelAudioMaskDropDown, layoutPosition, 1);
			AddWidget(ChangeAudioButton, ++layoutPosition, 1, HorizontalAlignment.Right);
			AddWidget(CancelButton, layoutPosition, 0, HorizontalAlignment.Left);

			ChannelAudioEncodingLabel.Width = 150;
			ChannelAudioEncodingDropDown.Width = 300;
			ChangeAudioButton.Width = 150;
			CancelButton.Width = 150;
		}

		public Label OutputSelectionLabel { get; private set; }

		public DropDown OutputSelectionDropDown { get; private set; }

		public Label ChannelSelectionLabel { get; private set; }

		public DropDown ChannelSelectionDropDown { get; private set; }

		public Label ChannelAudioEncodingLabel { get; private set; }

		public DropDown ChannelAudioEncodingDropDown { get; private set; }

		public Label ChannelAudioMaskLabel { get; private set; }

		public DropDown ChannelAudioMaskDropDown { get; private set; }

		public Button ChangeAudioButton { get; private set; }

		public Button CancelButton { get; private set; }

		public void Initialize()
		{
			var domHelper = new DomHelper(engine.SendSLNetMessages, DomIds.Lca_Access.ModuleId);
			var userConfig = Utils.GetDOMPermissionsByUser(domHelper, "Destination", engine);
			var outputsPermitted = userConfig.Destinations.Split(',').Select(x => Utils.RemoveBracketPrefix(x).Trim()).Distinct();
			OutputSelectionDropDown.SetOptions(outputsPermitted);

			//Tag Source: <list of current sources in the layout for the output>
			// -auto selected based on what the current channel is in the 1st output audio
			// Channel Audio PIDs: < current list of audio based on the selected Tag Source>

			string outputPidName = Utils.RemoveBracketPrefix(channelName) + "/1";

			var channelPidRows = tagElement.GetTable(TAGMCSIds.ChannelPidsTable.TablePid).GetRows();
			var audioRowsForChannel = channelPidRows.Where(row =>
				Convert.ToInt32(row[TAGMCSIds.ChannelPidsTable.Idx.Type]) == (int)TAGMCSIds.ChannelPidsTable.ChannelPidsType.Audio
				&& Convert.ToString(row[TAGMCSIds.ChannelPidsTable.Idx.DisplaysName]).Contains(channelName));

			var channelConfigTable = tagElement.GetTable(TAGMCSIds.ChannelConfigTable.TablePid);
			var displayKeys = channelConfigTable.GetDisplayKeys();
			var fullChannelName = displayKeys.FirstOrDefault(x => x.Contains(channelName));
			var channelId = channelConfigTable.GetPrimaryKey(fullChannelName);

			var audioDisplays = audioRowsForChannel.Select(row =>
			{
				var language = Convert.ToString(row[TAGMCSIds.ChannelPidsTable.Idx.Language]);
				var audioKey = Convert.ToString(row[TAGMCSIds.ChannelPidsTable.Idx.Index]);
				var match = Regex.Match(audioKey, @"Audio/(\d+)", RegexOptions.IgnoreCase);
				var audioId = match.Success ? match.Groups[1].Value : "N/A";
				var pid = Convert.ToString(row[TAGMCSIds.ChannelPidsTable.Idx.PID]);

				var audioFormatCode = 0;
				if (row[TAGMCSIds.ChannelPidsTable.Idx.AudioChannels] != null)
				{
					audioFormatCode = Convert.ToInt32(row[TAGMCSIds.ChannelPidsTable.Idx.AudioChannels]);
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

			var outputPidsTable = tagElement.GetTable(TAGMCSIds.OutputAudiosTable.TablePid);
			var outputPidsFilter = new List<ColumnFilter> { new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Pid = TAGMCSIds.OutputAudiosTable.Pid.Label, Value = outputPidName } };
			var outputPidsMatchingRow = outputPidsTable.QueryData(outputPidsFilter).First();
			var outputId = Convert.ToString(outputPidsMatchingRow[TAGMCSIds.OutputAudiosTable.Idx.OutputID]);

			this.ChangeAudioButton.Pressed += (sender, args) =>
			{
				var interAppHelper = new TagMCS(engine.GetUserConnection(), tagElement.AgentId, tagElement.Id);
				var getOutputConfig = new GetOutputConfigRequest(outputId, MessageIdentifier.ID);
				var outputConfigResponse = interAppHelper.SendMessage(getOutputConfig, TimeSpan.FromSeconds(30)) as GetOutputConfigResponse;
				if (outputConfigResponse == null)
				{
					ErrorMessageDialog.ShowMessage(engine, $"Updating the output failed, as the response from the MCS failed.");
					engine.ExitFail("Failure");
				}

				var selectedPid = this.ChannelAudioEncodingDropDown.Selected;
				var match = Regex.Match(selectedPid, @"Aud\((\d+)\)\s+PID\s+(\d+)");

				string audioId = match.Groups[1].Value;
				string pid = match.Groups[2].Value;

				var outputConfig = outputConfigResponse.Output;
				outputConfig.Processing.Audio[0].Mask = channelMaskingMap[ChannelAudioMaskDropDown.Selected];
				outputConfig.Input.Audio[0].AudioIndex = audioId;
				outputConfig.Input.Audio[0].AudioPid = pid;
				outputConfig.Input.Audio[0].Channel = channelId;
				outputConfig.Processing.Muxing.Audio[0].Pid = pid;

				var setMessage = new SetOutputConfigRequest
				{
					Output = outputConfig,
				};

				var setResponse = interAppHelper.SendMessage(setMessage, TimeSpan.FromMinutes(2)) as InterAppResponse;

				if (!setResponse.Success)
				{
					ErrorMessageDialog.ShowMessage(engine, $"Updating the output failed : {setResponse.ResponseMessage}.");
				}
				else
				{
					InformationMessageDialog.ShowMessage(engine, "Audio successfully set.");
				}

				engine.ExitSuccess("Finished");
			};
		}
	}
}