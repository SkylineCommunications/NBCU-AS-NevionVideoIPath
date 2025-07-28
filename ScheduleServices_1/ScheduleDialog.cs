namespace ScheduleServices_1
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS;
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS.InterApp.Messages;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

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
		private readonly RadioButtonList endRadioButtonList = new RadioButtonList(new[] { "Never", "In x from Start", "Date/Time" }, "In x from Start");
		private readonly DateTimePicker endDateTimePicker = new DateTimePicker(DateTime.Now.AddHours(1)) { IsVisible = false };
		private readonly Time endTimePicker = new Time(TimeSpan.FromHours(4)) { IsVisible = true, HasSeconds = false, Minimum = TimeSpan.FromMinutes(5), Maximum = TimeSpan.FromHours(24) };

		private readonly Label routeLabel = new Label("Route");
		private readonly RadioButtonList routeRadioButtonList = new RadioButtonList(new[] { "Point-to-Point", "Point-to-Multipoint" }, "Point-to-Multipoint");

		private readonly Element nevionVideoIPathElement;

		private List<string> destinationNames;
		private string[] primaryKeysCurrentServices = new string[0];

		public ScheduleDialog(IEngine engine) : base(engine)
		{
			Title = "Connect Services";

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

			primaryKeysCurrentServices = nevionVideoIPathElement.GetTablePrimaryKeys(1500); // Used to check if new connection entries has been added after the ConnectServices.

			startRadioButtonList.Changed += (s, o) => HandleStartOptionChanged();
			endRadioButtonList.Changed += (s, o) => HandleEndOptionChanged();

			ConnectButton.Pressed += (s, o) =>
			{
				TriggerConnectOnElement();
				VerifyConnectService(); // Temproary untill real time updates are fully supported in the apps.

				// connect TAG
				ConnectTagMCS(engine);
			};

			GenerateUI();
		}

		private void ConnectTagMCS(IEngine engine)
		{
			var dms = Engine.GetDms();
			var tagMcsElement = dms.GetElement("TAG AWS QC MCS");

			var tagMcs = new TagMCS(engine.GetUserConnection(), tagMcsElement.AgentId, tagMcsElement.Id);

			var destinationName = DestinationNames[0];
			var layoutName = RemoveBracketPrefix(destinationName);

			// get the correct channel name from tag finding all configuration display keys containing destination name (only should be one)

			// var layoutType = tagMcsElement.GetTable(3600).GetColumn<string>(3604).GetDisplayValue(destinationName, Skyline.DataMiner.Core.DataMinerSystem.Common.KeyType.DisplayKey);
			// if (layoutType != "QC Channel_v1")
			// {
			// 	engine.ExitFail("MCS Layout is not the correct type");
			// }

			var layoutRequest = new SetChannelInLayoutRequest(layoutName, destinationName, 1, MessageIdentifier.Name);
			tagMcs.SendMessage(layoutRequest, TimeSpan.FromSeconds(10));
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
				ProfileName = "AVC-HD-SRT-CALL";
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

			AddWidget(new WhiteSpace(), ++row, 0);

			AddWidget(CancelButton, ++row, 0);
			AddWidget(ConnectButton, row, 1, HorizontalAlignment.Right);
		}
	}
}
