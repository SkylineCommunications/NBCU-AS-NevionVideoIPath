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
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS.InterApp.Messages;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	internal class TagAudioDialog : Dialog
	{
		private readonly IEngine engine;
		private readonly IDmsElement tagElement;

		private string currentPid;

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
			OutputSelectionDropDown = new DropDown { IsSorted = true };

			ChannelSelectionLabel = new Label("Source:");
			ChannelSelectionDropDown = new DropDown { IsSorted = true };

			ChannelAudioEncodingLabel = new Label("Source PID:");
			ChannelAudioEncodingDropDown = new DropDown { IsSorted = true };

			ChannelAudioMaskLabel = new Label("Audio Channel:");
			ChannelAudioMaskDropDown = new DropDown(new List<string> { "None", "Front Left", "Front Right", "Center", "Low-Frequency Effects", "Surround Left", "Surround Right" });
			ChangeAudioButton = new Button("Change Audio") { Style = ButtonStyle.CallToAction };
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

			ChannelAudioEncodingLabel.Width = 100;
			ChannelAudioEncodingDropDown.Width = 400;
			OutputSelectionDropDown.Width = 400;
			ChannelSelectionDropDown.Width = 400;
			ChannelAudioMaskDropDown.Width = 400;
			ChangeAudioButton.Width = 150;
			CancelButton.Width = 100;
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
			var outputsPermitted = userConfig.Destinations.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => Utils.RemoveBracketPrefix(x).Trim()).Distinct();
			if (userConfig.Destinations == "ALL")
			{
				outputsPermitted = tagElement.GetTable(TAGMCSIds.OutputConfigTable.TablePid).GetDisplayKeys().Where(x => x.Contains("Routable"));
			}

			var defaultOutput = outputsPermitted.FirstOrDefault();
			OutputSelectionDropDown.SetOptions(outputsPermitted);

			SetChannelDropdown(defaultOutput, out string defaultSourceId);
			SetAudioPidsDropdown(defaultSourceId);

			InitializeEvents();
		}

		public void InitializeEvents()
		{
			OutputSelectionDropDown.Changed += (sender, args) =>
			{
				var selectedOutput = OutputSelectionDropDown.Selected;
				SetChannelDropdown(selectedOutput, out string defaultSourceId);
				SetAudioPidsDropdown(defaultSourceId);
			};

			ChannelSelectionDropDown.Changed += (sender, args) =>
			{
				var selectedChannel = ChannelSelectionDropDown.Selected;
				var sourceId = TagUtils.GetIdFromName(tagElement, TAGMCSIds.ChannelConfigTable.TablePid, selectedChannel);
				SetAudioPidsDropdown(sourceId);
			};

			ChangeAudioButton.Pressed += (sender, args) =>
			{
				var selectedOutput = OutputSelectionDropDown.Selected;
				var outputId = TagUtils.GetIdFromName(tagElement, TAGMCSIds.OutputConfigTable.TablePid, selectedOutput);

				var selectedChannel = ChannelSelectionDropDown.Selected;
				var sourceId = TagUtils.GetIdFromName(tagElement, TAGMCSIds.ChannelConfigTable.TablePid, selectedChannel);

				var interAppHelper = new TagMCS(engine.GetUserConnection(), tagElement.AgentId, tagElement.Id);
				var getOutputConfig = new GetOutputConfigRequest(outputId, MessageIdentifier.ID);
				var outputConfigResponse = interAppHelper.SendMessage(getOutputConfig, TimeSpan.FromSeconds(30)) as GetOutputConfigResponse;
				if (outputConfigResponse == null)
				{
					ErrorMessageDialog.ShowMessage(engine, $"Updating the output failed, as the response from the MCS is invalid.");
					engine.ExitFail("Failure");
				}

				var selectedPid = this.ChannelAudioEncodingDropDown.Selected;
				var match = Regex.Match(selectedPid, @"Aud\((\d+)\)\s+PID\s+(\d+)");

				string audioId = match.Groups[1].Value;
				string pid = match.Groups[2].Value;

				var outputConfig = outputConfigResponse.Output;
				outputConfig.Processing.Audio[0].Mask = channelMaskingMap[ChannelAudioMaskDropDown.Selected];
				outputConfig.Input.Audio[0].AudioIndex = audioId == "undefined" ? string.Empty : audioId;
				outputConfig.Input.Audio[0].AudioPid = pid;
				outputConfig.Input.Audio[0].Channel = sourceId;
				outputConfig.Processing.Muxing.Audio[0].Pid = "202";

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

			CancelButton.Pressed += (sender, args) => engine.ExitSuccess("Changed Audio Canceled by User");
		}

		private void SetChannelDropdown(string defaultOutput, out string defaultSourceId)
		{
			var outputLayoutFilter = new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Pid = TAGMCSIds.OutputsLayoutsTable.Pid.Output, Value = defaultOutput };
			var outputLayoutRow = tagElement.GetTable(TAGMCSIds.OutputsLayoutsTable.TablePid).QueryData(new List<ColumnFilter> { outputLayoutFilter }).FirstOrDefault();
			var outputLayoutId = Convert.ToString(outputLayoutRow[TAGMCSIds.OutputsLayoutsTable.Idx.LayoutID]);

			var layoutFilter = new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Pid = TAGMCSIds.AllLayoutChannelsTable.Pid.LayoutID, Value = outputLayoutId };
			var layoutChannelRows = tagElement.GetTable(TAGMCSIds.AllLayoutChannelsTable.TablePid).QueryData(new List<ColumnFilter> { layoutFilter });

			var outputAudioLabel = defaultOutput + "/1";
			var outputAudioFilter = new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Pid = TAGMCSIds.OutputAudiosTable.Pid.Label, Value = outputAudioLabel };
			var firstOutputAudioRow = tagElement.GetTable(TAGMCSIds.OutputAudiosTable.TablePid).QueryData(new List<ColumnFilter> { outputAudioFilter }).FirstOrDefault();

			var outputChannel = Convert.ToString(firstOutputAudioRow[TAGMCSIds.OutputAudiosTable.Idx.Channel]);
			currentPid = Convert.ToString(firstOutputAudioRow[TAGMCSIds.OutputAudiosTable.Idx.InputPID]);
			var currentMask = Convert.ToInt32(firstOutputAudioRow[TAGMCSIds.OutputAudiosTable.Idx.OutputMask]);

			var defaultSourceRow = layoutChannelRows.First(x => Convert.ToInt32(x[TAGMCSIds.AllLayoutChannelsTable.Idx.Position]) == 1);
			var defaultSource = Convert.ToString(defaultSourceRow[TAGMCSIds.AllLayoutChannelsTable.Idx.ChannelTitle]);
			defaultSourceId = Convert.ToString(defaultSourceRow[TAGMCSIds.AllLayoutChannelsTable.Idx.ChannelSourceId]);

			var channelsInLayout = layoutChannelRows
				.Select(x => Convert.ToString(x[TAGMCSIds.AllLayoutChannelsTable.Idx.ChannelTitle])).Where(x => IsValidChannel(x)).ToList();

			var outputHasChannel = IsValidChannel(outputChannel);
			if (outputHasChannel)
			{
				channelsInLayout.Add(outputChannel);
				defaultSourceId = Convert.ToString(firstOutputAudioRow[TAGMCSIds.OutputAudiosTable.Idx.ChannelID]);
			}

			ChannelSelectionDropDown.SetOptions(channelsInLayout);
			ChannelSelectionDropDown.Selected = outputHasChannel ? outputChannel : defaultSource;
			ChannelAudioMaskDropDown.Selected = currentMask < 1 ? "None" : channelMaskingMap.FirstOrDefault(x => x.Value == currentMask).Key;
		}

		private void SetAudioPidsDropdown(string defaultSourceId)
		{
			var pidsChannelFilter = new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Pid = TAGMCSIds.ChannelPidsTable.Pid.ChannelId, Value = defaultSourceId };
			var pidsAudioFilter = new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Pid = TAGMCSIds.ChannelPidsTable.Pid.Type, Value = Convert.ToString((int)TAGMCSIds.ChannelPidsTable.ChannelPidsType.Audio) };
			var pidsAes3Filter = new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Pid = TAGMCSIds.ChannelPidsTable.Pid.Type, Value = Convert.ToString((int)TAGMCSIds.ChannelPidsTable.ChannelPidsType.AES3) };
			var audioRowsForChannel = tagElement.GetTable(TAGMCSIds.ChannelPidsTable.TablePid).QueryData(new List<ColumnFilter> { pidsChannelFilter, pidsAudioFilter, pidsAes3Filter });

			var audioDisplays = new List<string>();
			var currentPidFormat = String.Empty;
			foreach (var row in audioRowsForChannel)
			{
				var language = Convert.ToString(row[TAGMCSIds.ChannelPidsTable.Idx.Language]);
				var audioKey = Convert.ToString(row[TAGMCSIds.ChannelPidsTable.Idx.Index]);
				var match = Regex.Match(audioKey, @"(?:Audio|AES3|AES67)/(\d+)", RegexOptions.IgnoreCase);
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
					audioFormat = $"Don't Care";
				}

				var pidFormat = $"{language.ToUpper()} - Aud({audioId}) PID {pid} - {audioFormat}";
				audioDisplays.Add(pidFormat);

				if (pid == currentPid)
				{
					currentPidFormat = pidFormat;
				}
			}

			var statusPidsChannelFilter = new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Pid = TAGMCSIds.ChannelStatusComponentsTable.Pid.ChannelID, Value = defaultSourceId };
			var statusPidsAudioFilter = new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Pid = TAGMCSIds.ChannelStatusComponentsTable.Pid.ContentType, Value = Convert.ToString((int)TAGMCSIds.ChannelPidsTable.ChannelPidsType.Audio) };
			var statusPidsAes3Filter = new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Pid = TAGMCSIds.ChannelStatusComponentsTable.Pid.ContentType, Value = Convert.ToString((int)TAGMCSIds.ChannelPidsTable.ChannelPidsType.AES3) };
			var statusPidsAes67Filter = new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Pid = TAGMCSIds.ChannelStatusComponentsTable.Pid.ContentType, Value = Convert.ToString((int)TAGMCSIds.ChannelPidsTable.ChannelPidsType.AES67) };
			var statusAudioRowsForChannel = tagElement.GetTable(TAGMCSIds.ChannelStatusComponentsTable.TablePid).QueryData(new List<ColumnFilter> { statusPidsChannelFilter, statusPidsAudioFilter, statusPidsAes3Filter, statusPidsAes67Filter });

			foreach (var row in statusAudioRowsForChannel)
			{
				var pid = Convert.ToString(row[TAGMCSIds.ChannelStatusComponentsTable.Idx.PID]);
				if (audioRowsForChannel.Any(r => Convert.ToString(r[TAGMCSIds.ChannelPidsTable.Idx.PID]) == pid && Convert.ToString(r[TAGMCSIds.ChannelPidsTable.Idx.ChannelId]) == defaultSourceId))
				{
					continue;
				}

				var index = Convert.ToString(row[TAGMCSIds.ChannelStatusComponentsTable.Idx.Index]);
				if (index == "-1")
				{
					index = "undefined";
				}

				var pidFormat = $"Aud({index}) PID {pid}";
				audioDisplays.Add(pidFormat);

				if (pid == currentPid)
				{
					currentPidFormat = pidFormat;
				}
			}

			ChannelAudioEncodingDropDown.Options = audioDisplays;
			ChannelAudioEncodingDropDown.Selected = currentPidFormat;
		}

		public bool IsValidChannel(string channel)
		{
			return !String.IsNullOrWhiteSpace(channel) && channel != "None";
		}
	}
}