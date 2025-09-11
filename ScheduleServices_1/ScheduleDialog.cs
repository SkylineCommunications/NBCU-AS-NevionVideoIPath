namespace ScheduleServices_1
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;

	using NevionCommon_1;

	using NevionSharedUtils;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS;
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS.API_Models;
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS.InterApp.Messages;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Scheduling;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	using static NevionSharedUtils.NevionIds;

	public class ScheduleDialog : Dialog
	{
		private readonly Label sourceNameLabel = new Label("Source Name");
		private readonly Label sourceNameValue = new Label();
		private readonly Label destinationNamesLabel = new Label("Destination(s)");
		private readonly Label destinationNameValues = new Label();
		private readonly Label profileLabel = new Label("Profile");
		private readonly Label profileValue = new Label();

		private readonly Label serviceNameLabel = new Label("Service Name");
		private readonly TextBox serviceNameTextBox = new TextBox() { PlaceHolder = "(required)", MinWidth = 300 };
		private readonly Label descriptionLabel = new Label("Description");
		private readonly TextBox descriptionTextBox = new TextBox() { PlaceHolder = "(optional)" };
		private readonly Label tagsLabel = new Label("Tags");
		private readonly TextBox tagsTextBox = new TextBox() { PlaceHolder = "(optional)", Tooltip = "Service Tags" };

		private readonly Label startLabel = new Label("Start");
		private readonly RadioButtonList startRadioButtonList = new RadioButtonList(new[] { "Now", "In x from Now", "Date/Time" }, "Now");
		private readonly DateTimePicker startDateTimePicker = new DateTimePicker(DateTime.Now.AddMinutes(30)) { IsVisible = false };
		private readonly Time startTimePicker = new Time(TimeSpan.FromMinutes(30)) { IsVisible = false, HasSeconds = false, Minimum = TimeSpan.FromMinutes(5), Maximum = TimeSpan.FromHours(24), ClipValueToRange = true };

		private readonly Label endLabel = new Label("End");
		private readonly RadioButtonList endRadioButtonList = new RadioButtonList(new[] { "Never", "In x from Start", "Date/Time" }, "Never");
		private readonly DateTimePicker endDateTimePicker = new DateTimePicker(DateTime.Now.AddHours(1)) { IsVisible = false };
		private readonly Time endTimePicker = new Time(TimeSpan.FromHours(4)) { IsVisible = false, HasSeconds = false, Minimum = TimeSpan.FromMinutes(5), Maximum = TimeSpan.FromHours(24) };

		private readonly Label routeLabel = new Label("Route");
		private readonly RadioButtonList routeRadioButtonList = new RadioButtonList(new[] { "Point-to-Point", "Point-to-Multipoint" }, "Point-to-Multipoint");

		private readonly Label existingVIPConnections = new Label("Existing Connections") { IsVisible = false };
		private readonly Label existingConnectionsText = new Label("NOTE: The selected destination is in use, hitting connect will delete this existing connection:") { IsVisible = false };

		private readonly IDms dms;
		private readonly IDmsElement nevionElement;
		private readonly IDmsTable connectionsTable;
		private readonly Dictionary<string, string> existingConnections;

		private readonly Element nevionVideoIPathElement;

		private readonly LoggingHelper loggingHelper;

		private List<string> destinationNames;
		private string[] primaryKeysCurrentServices = new string[0];
		private bool skipVIPConnection;
		private List<ProfileComponent> components;

		public ScheduleDialog(IEngine engine) : base(engine)
		{
			Title = "Connect Services";
			dms = engine.GetDms();
			loggingHelper = new LoggingHelper(engine);

			existingConnections = new Dictionary<string, string>();

			nevionVideoIPathElement = engine.FindElementsByProtocol("Nevion Video iPath", "Production").FirstOrDefault();
			if (nevionVideoIPathElement == null)
			{
				engine.ExitFail("Nevion Video iPath element not found!");
				return;
			}

			if (!nevionVideoIPathElement.IsActive)
			{
				engine.ExitFail("Nevion Video iPath element not active!");
				return;
			}

			nevionElement = dms.GetElement("Nevion VIP - Prod");
			connectionsTable = nevionElement.GetTable(NevionConnectionsTable.TableId);

			primaryKeysCurrentServices = nevionVideoIPathElement.GetTablePrimaryKeys(NevionConnectionsTable.TableId); // Used to check if new connection entries has been added after the ConnectServices.

			startRadioButtonList.Changed += (s, o) => HandleStartOptionChanged();
			endRadioButtonList.Changed += (s, o) => HandleEndOptionChanged();

			ConnectButton.Pressed += (s, o) =>
			{
				var endTime = End.HasValue
					? Convert.ToString(End.Value.ToOADate(), CultureInfo.InvariantCulture)
					: String.Empty;

				var nevionConnection = Task.Run(() =>
				{
					var connectionName = $"{SourceName}->{DestinationNames[0]}";
					loggingHelper.GenerateInformation($"Nevion Connection: {connectionName}, Profile: {ProfileName}, Start: {Start}, End:{endTime}");

					if (!TryDeleteConnections())
					{
						var message = $"Unable to delete the pre-existing connections: {String.Join(",", existingConnections.Values)}";
						ErrorMessageDialog.ShowMessage(engine, message);
						engine.Log(message);
					}

					if (!skipVIPConnection)
					{
						TriggerConnectOnElement();
						VerifyConnectService(); // Temporary until real time updates are fully supported in the apps.
					}
				});

				var tagConnection = Task.Run(() => ConnectTagMCS(engine));

				// Block until both are finished
				Task.WaitAll(nevionConnection, tagConnection);
			};

			GenerateUI();
		}

		private bool TryDeleteConnections()
		{
			if (existingConnections.Count > 0)
			{
				if (skipVIPConnection)
				{
					// connection between source and destination exists, skip
					return true;
				}

				foreach (var key in existingConnections.Keys)
				{
					connectionsTable.GetColumn<double?>(NevionConnectionsTable.Pid.CancelButton).SetValue(key, 1);
					Thread.Sleep(1000);
				}

				bool CheckKeysDeleted()
				{
					var updatedTableKeys = connectionsTable.GetPrimaryKeys().ToList();
					var areKeysInTable = existingConnections.Keys.Any(key => updatedTableKeys.Contains(key));
					return !areKeysInTable;
				}

				if (Utils.Retry(CheckKeysDeleted, TimeSpan.FromSeconds(30)))
				{
					return true;
				}
				else
				{
					return false;
				}
			}

			return true;
		}

		private void ConnectTagMCS(IEngine engine)
		{
			try
			{
				var tagMcsElement = dms.GetElement("TAG AWS MCS");

				var tagMcs = new TagMCS(engine.GetUserConnection(), tagMcsElement.AgentId, tagMcsElement.Id);

				var destinationName = DestinationNames[0];
				var layoutName = Utils.RemoveBracketPrefix(destinationName);

				var isRTP = destinationName.StartsWith("[VIP RTP]");

				var connectionName = $"{SourceName}->{destinationName}";

				string channelId = TagUtils.GetIdFromName(tagMcsElement, TAGMCSIds.ChannelConfigTable.TablePid, destinationName);

				var errorBuilder = new StringBuilder();

				UpdateOutput(tagMcsElement, tagMcs, layoutName, channelId, isRTP, errorBuilder);
				UpdateLayout(tagMcsElement, tagMcs, layoutName, 1, channelId, errorBuilder, connectionName);

				CreateScheduledTask(tagMcsElement, channelId, connectionName);

				if (errorBuilder.Length != 0)
				{
					ErrorMessageDialog.ShowMessage(engine, errorBuilder.ToString());
					engine.Log($"{errorBuilder}");
				}
			}
			catch (Exception e)
			{
				ErrorMessageDialog.ShowMessage(engine, $"Nevion connection made, but there was a script exception while updating TAG. Please contact Skyline: {e}");
				Engine.Log($"ConnectTagMCS|Failed to update TAG: {e}");
			}
		}

		private void UpdateOutput(IDmsElement tagMcsElement, TagMCS tagMcs, string layoutName, string channelId, bool isRTP, StringBuilder errorBuilder)
		{
			try
			{
				var outputId = TagUtils.GetIdFromName(tagMcsElement, TAGMCSIds.OutputConfigTable.TablePid, layoutName);

				var getOutputConfig = new GetOutputConfigRequest(outputId, MessageIdentifier.ID);
				var outputConfigResponse = tagMcs.SendMessage(getOutputConfig, TimeSpan.FromSeconds(30)) as GetOutputConfigResponse;
				if (outputConfigResponse == null)
				{
					ErrorMessageDialog.ShowMessage(Engine, $"Updating the output failed, as the response from the MCS is invalid.");
					Engine.ExitFail("Failure");
				}

				string pid = null;
				string audioId = null;
				var component = components?.Where(x => x != null && x.Pid != null && (x.ContentType == "Audio" || x.ContentType == "AES3" || x.ContentType == "AES67")).OrderBy(c => c.Pid).FirstOrDefault();
				if (component != null)
				{
					pid = Convert.ToString(component.Pid);
					audioId = Convert.ToString(component.Index);
				}

				if (isRTP)
				{
					pid = "50";
					audioId = "1";
				}

				var outputConfig = outputConfigResponse.Output;
				outputConfig.Input.Audio[0].Channel = channelId;
				outputConfig.Processing.Audio[0].Mask = 0;
				outputConfig.Input.Audio[0].AudioPid = pid;
				outputConfig.Input.Audio[0].AudioIndex = audioId;
				outputConfig.Processing.Muxing.Audio[0].Pid = "202";

				var setMessage = new SetOutputConfigRequest
				{
					Output = outputConfig,
				};

				var setResponse = tagMcs.SendMessage(setMessage, TimeSpan.FromMinutes(2)) as InterAppResponse;

				if (!setResponse.Success)
				{
					errorBuilder.AppendLine($"Updating the output with channel failed : {setResponse.ResponseMessage}.");
				}
			}
			catch (Exception e)
			{
				errorBuilder.AppendLine($"Script exception while changing the output source. Please contact Skyline: {e}");
				Engine.Log($"UpdateOutput|Failed to update channel output: {e}");
			}
		}

		private void CreateScheduledTask(IDmsElement tag, string channelId, string connectionName)
		{
			var scriptName = "Cleanup TAG Audio Task";

			var scheduler = dms.GetAgent(tag.AgentId).Scheduler;

			var oldTask = scheduler.GetTasks().FirstOrDefault(x => x.Description == channelId);
			if (oldTask != null)
			{
				scheduler.DeleteTask(oldTask.Id);
			}

			if (!End.HasValue)
			{
				return;
			}

			if (!dms.ScriptExists(scriptName))
			{
				throw new ScriptNotFoundException($"Failed to find \"{scriptName}\" Script");
			}

			var startTime = End.Value.ToString("HH:mm:ss");
			var taskType = "once";
			var interval = "1";

			var task = new object[]
			{
				new object[] // General Info
				{
					new[]
					{
						connectionName,
						string.Empty,
						string.Empty,
						startTime,
						taskType,
						interval,
						string.Empty,
						channelId,
						"TRUE",
						string.Empty,
						string.Empty,
					},
				},
				new object[] // Repeat Info
				{
					new[]
					{
						"automation",
						scriptName,
						$"PARAMETER:10:{channelId}",
						"CHECKSET:FALSE",
						"DEFER:FALSE",
					},
				},
				new object[] {}, // Final Actions
			};

			scheduler.CreateTask(task);
		}

		private void UpdateLayout(IDmsElement tagMcsElement, TagMCS tagMcs, string layoutName, int position, string channelId, StringBuilder errorBuilder, string umdUpdate)
		{
			try
			{
				string layoutId = TagUtils.GetIdFromName(tagMcsElement, TAGMCSIds.LayoutTable.TablePid, layoutName);
				var getLayoutRequest = new GetLayoutConfigRequest(layoutId, MessageIdentifier.ID);
				var layoutResponse = tagMcs.SendMessage(getLayoutRequest, TimeSpan.FromSeconds(30)) as GetLayoutConfigResponse;

				var matchingIndex = layoutResponse.Layout.Tiles.FindIndex(x => x.Index == position);

				if (matchingIndex != -1)
				{
					layoutResponse.Layout.Tiles[matchingIndex].Channel = channelId;
				}

				layoutResponse.Layout.LayoutType = "TAG QC";
				if (layoutResponse.Layout.Tiles[0].Umd == null)
				{
					layoutResponse.Layout.Tiles[0].Umd = new List<string> { umdUpdate };
				}
				else
				{
					layoutResponse.Layout.Tiles[0].Umd[0] = umdUpdate;
				}

				var setMessage = new SetLayoutConfigRequest
				{
					Layout = layoutResponse.Layout,
				};

				var setResponse = tagMcs.SendMessage(setMessage, TimeSpan.FromMinutes(2)) as InterAppResponse;

				if (!setResponse.Success)
				{
					errorBuilder.AppendLine($"Updating the layout failed : {setResponse.ResponseMessage}.");
					return;
				}

				Thread.Sleep(2000);

				var channelUpdateMessage = new SetChannelInLayoutRequest(layoutResponse.Layout.Uuid, channelId, 1, MessageIdentifier.ID);
				var channelLayoutResponse = tagMcs.SendMessage(channelUpdateMessage, TimeSpan.FromMinutes(2)) as InterAppResponse;

				if (!channelLayoutResponse.Success)
				{
					errorBuilder.AppendLine($"Updating the layout with channel failed : {channelLayoutResponse.ResponseMessage}.");
				}
			}
			catch (Exception e)
			{
				errorBuilder.AppendLine($"Script exception while updating the TAG layout. Please contact Skyline: {e}");
				Engine.Log($"UpdateLayout|Failed to update TAG Layout: {e}");
			}
		}

		public string SourceName
		{
			get
			{
				return sourceNameValue.Text;
			}

			private set
			{
				sourceNameValue.Text = value;
			}
		}

		/// <summary>
		/// Gets the destination names, in case of multiple names the string is comma separated (",") for each element.
		/// </summary>
		public List<string> DestinationNames
		{
			get
			{
				return destinationNames;
			}

			private set
			{
				destinationNames = value;
				destinationNameValues.Text = String.Join(",", destinationNames);
			}
		}

		public string ProfileName
		{
			get
			{
				return profileValue.Text;
			}

			private set
			{
				profileValue.Text = value;
			}
		}

		public string Name
		{
			get
			{
				return serviceNameTextBox.Text;
			}

			private set
			{
				serviceNameTextBox.Text = value;
			}
		}

		public DateTime Start
		{
			get
			{
				var startSelection = startRadioButtonList.Selected;
				if (startSelection == "In x from Now")
				{
					return DateTime.Now.Add(startTimePicker.TimeSpan);
				}
				else if (startSelection == "Date/Time")
				{
					return startDateTimePicker.DateTime;
				}
				else
				{
					return DateTime.Now;
				}
			}
		}

		public DateTime? End
		{
			get
			{
				var endSelection = endRadioButtonList.Selected;
				if (endSelection == "In x from Start")
				{
					return Start.Add(endTimePicker.TimeSpan);
				}
				else if (endSelection == "Date/Time")
				{
					return endDateTimePicker.DateTime;
				}
				else
				{
					return null;
				}
			}
		}

		public Button ConnectButton { get; private set; } = new Button("Connect") { Style = ButtonStyle.CallToAction, Width = 120 };

		public Button CancelButton { get; private set; } = new Button("Cancel") { Width = 120 };

		public void SetInput(string sourceName, List<string> destinationNames, string sourceTags)
		{
			SourceName = sourceName;
			DestinationNames = destinationNames;

			if (sourceTags.Contains("JPEG-XS-3G"))
			{
				ProfileName = "JPEG-XS-3G-TO-CLOUD";
			}
			else if (sourceTags.Contains("JPEG-XS-HD"))
			{
				ProfileName = "JPEG-XS-HD-TO-CLOUD";
			}
			else if (sourceTags.Contains("SRT"))
			{
				ProfileName = "Automatic";
			}
			else
			{
				Engine.ExitFail("Could not determine profile from the given source.");
			}

			Name = $"{SourceName}->{(DestinationNames.Count > 1 ? "Multipoint" : DestinationNames[0])}";

			if (DestinationNames.Count > 1)
			{
				routeRadioButtonList.SetOptions(new[] { "Point-to-Multipoint" });
			}

			var connectionsWithDestination = connectionsTable.QueryData(new List<ColumnFilter> { new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Pid = NevionConnectionsTable.Pid.DestinationName, Value = DestinationNames[0] } });

			CheckCurrentSourcesAndDestinations(connectionsWithDestination);
			if (existingConnections.Count > 0)
			{
				if (existingConnections.Any(x => x.Value.Contains(sourceNameValue.Text) && x.Value.Contains(destinationNamesLabel.Text)))
				{
					skipVIPConnection = true;
					existingConnectionsText.Text = "NOTE: This VIP Connection already exists and will not be recreated";
					return;
				}

				foreach (var connection in existingConnections)
				{
					existingConnectionsText.Text += $"{Environment.NewLine}  • [{connection.Key}]: {connection.Value}";
				}

				existingVIPConnections.IsVisible = true;
				existingConnectionsText.IsVisible = true;
			}
		}

		public void TriggerConnectOnElement()
		{
			var visioString = String.Join(
				";",
				ProfileName,
				Name,
				SourceName,
				String.Join(",", DestinationNames),
				Convert.ToString(Start.ToOADate(), CultureInfo.InvariantCulture),
				End.HasValue ? Convert.ToString(End.Value.ToOADate(), CultureInfo.InvariantCulture) : String.Empty,
				routeRadioButtonList.Selected,
				Convert.ToInt32(!End.HasValue),
				descriptionTextBox.Text,
				tagsTextBox.Text);

			nevionVideoIPathElement.SetParameter(2309, visioString);
		}

		private void VerifyConnectService()
		{
			int retries = 0;
			bool allEntriesFound = false;
			int tableEntryCountIncludingNewEntries = primaryKeysCurrentServices.Length + DestinationNames.Count;
			while (!allEntriesFound && retries < 100)
			{
				Engine.Sleep(50);

				var allPrimaryKeys = nevionVideoIPathElement.GetTablePrimaryKeys(1500);

				allEntriesFound = allPrimaryKeys.Length == tableEntryCountIncludingNewEntries;

				retries++;
			}
		}

		private void HandleStartOptionChanged()
		{
			var startSelection = startRadioButtonList.Selected;
			if (startSelection == "In x from Now")
			{
				startDateTimePicker.IsVisible = false;
				startTimePicker.IsVisible = true;
			}
			else if (startSelection == "Date/Time")
			{
				startDateTimePicker.IsVisible = true;
				startTimePicker.IsVisible = false;
			}
			else
			{
				startDateTimePicker.IsVisible = false;
				startTimePicker.IsVisible = false;
			}
		}

		private void HandleEndOptionChanged()
		{
			var endSelection = endRadioButtonList.Selected;
			if (endSelection == "In x from Start")
			{
				endDateTimePicker.IsVisible = false;
				endTimePicker.IsVisible = true;
			}
			else if (endSelection == "Date/Time")
			{
				endDateTimePicker.IsVisible = true;
				endTimePicker.IsVisible = false;
			}
			else
			{
				endDateTimePicker.IsVisible = false;
				endTimePicker.IsVisible = false;
			}
		}

		private void GenerateUI()
		{
			int row = -1;

			AddWidget(sourceNameLabel, ++row, 0);
			AddWidget(sourceNameValue, row, 1);

			AddWidget(destinationNamesLabel, ++row, 0);
			AddWidget(destinationNameValues, row, 1);

			AddWidget(profileLabel, ++row, 0);
			AddWidget(profileValue, row, 1);

			AddWidget(new WhiteSpace(), ++row, 0);

			AddWidget(serviceNameLabel, ++row, 0);
			AddWidget(serviceNameTextBox, row, 1);

			AddWidget(descriptionLabel, ++row, 0);
			AddWidget(descriptionTextBox, row, 1);

			AddWidget(tagsLabel, ++row, 0);
			AddWidget(tagsTextBox, row, 1);

			AddWidget(new WhiteSpace(), ++row, 0);

			AddWidget(startLabel, ++row, 0, HorizontalAlignment.Left, VerticalAlignment.Top);
			AddWidget(startRadioButtonList, row, 1, HorizontalAlignment.Left, VerticalAlignment.Top);
			AddWidget(startDateTimePicker, ++row, 1);
			AddWidget(startTimePicker, ++row, 1);

			AddWidget(endLabel, ++row, 0, HorizontalAlignment.Left, VerticalAlignment.Top);
			AddWidget(endRadioButtonList, row, 1, HorizontalAlignment.Left, VerticalAlignment.Top);
			AddWidget(endDateTimePicker, ++row, 1);
			AddWidget(endTimePicker, ++row, 1);

			AddWidget(routeLabel, ++row, 0, HorizontalAlignment.Left, VerticalAlignment.Top);
			AddWidget(routeRadioButtonList, row, 1, HorizontalAlignment.Left, VerticalAlignment.Top);

			AddWidget(existingConnectionsText, ++row, 1);

			AddWidget(new WhiteSpace(), ++row, 0);

			AddWidget(CancelButton, ++row, 0);
			AddWidget(ConnectButton, row, 1, HorizontalAlignment.Right);
		}

		private void CheckCurrentSourcesAndDestinations(IEnumerable<object[]> connectionsTableData)
		{
			foreach (var rowData in connectionsTableData)
			{
				var key = Convert.ToString(rowData[NevionConnectionsTable.Idx.ServiceId]);
				if (key.EndsWith("-1") || key.EndsWith("-2"))
				{
					continue;
				}

				AddExistingConnection(rowData, key);
			}
		}

		private void AddExistingConnection(object[] rowData, string serviceId)
		{
			var sourceName = Convert.ToString(rowData[NevionConnectionsTable.Idx.SourceName]);
			var destName = Convert.ToString(rowData[NevionConnectionsTable.Idx.DestinationName]);

			existingConnections.Add(serviceId, $"{sourceName} → {destName}");
		}
	}
}