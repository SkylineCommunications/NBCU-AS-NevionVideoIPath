namespace Edit_Profiles_1
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Utils.DOM;
	using Skyline.DataMiner.Utils.DOM.Extensions;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class ProfileDialog : Dialog
	{
		private readonly IEngine engine;
		private readonly IDmsElement nevionElement;
		private readonly DomHelper domHelper;
		private readonly DomCache domCache;

		private List<string> nevionProfiles;
		private List<string> availableProfiles = new List<string>();
		private List<string> selectedProfiles = new List<string>();

		private DomInstance domInstance;

		public ProfileDialog(IEngine engine, string nevionElementId, string instanceId) : base(engine)
		{
			this.engine = engine;
			var dms = engine.GetDms();
			domHelper = new DomHelper(engine.SendSLNetMessages, "lca_access");
			domCache = new DomCache(domHelper);

			if (!Guid.TryParse(instanceId, out Guid instance))
			{
				engine.ExitFail($"Dom Instance ID is not in its valid format.");
				return;
			}

			nevionElement = dms.GetElement(new DmsElementId(nevionElementId));

			if (nevionElement.State != ElementState.Active)
			{
				engine.ExitFail($"Element {nevionElementId} is not active. Please check the element state.");
			}

			InitWidgets();
			InitData(instance);
		}

		public enum SelectType
		{
			SelectAllLeft,
			SelectAllRight,
			Add,
			Remove,
		}

		private Label PermissionInfoLabel { get; set; }

		private Label UsernameLabel { get; set; }

		private Label GroupLabel { get; set; }

		private Label SelectProfilesLabel { get; set; }

		private Button SelectAll { get; set; }

		private Button Add { get; set; }

		private Button Remove { get; set; }

		private Button DeselectAll { get; set; }

		private CheckBoxList ProfilesCheckBoxList { get; set; }

		private CheckBoxList SelectedProfilesCheckBoxList { get; set; }

		private Button SaveButton { get; set; }

		private Button CancelButton { get; set; }

		public void InitializeEventHandlers(InteractiveController controller)
		{
			SelectAll.Pressed += (sender, args) => CheckOptions(SelectType.SelectAllLeft);
			DeselectAll.Pressed += (sender, args) => CheckOptions(SelectType.SelectAllRight);
			Add.Pressed += (sender, args) => CheckOptions(SelectType.Add);
			Remove.Pressed += (sender, args) => CheckOptions(SelectType.Remove);
			SaveButton.Pressed += (sender, args) => SaveToDom();
			CancelButton.Pressed += (sender, args) => engine.ExitSuccess("User cancelled.");
		}

		private void InitWidgets()
		{
			Title = "Edit Profiles";
			var buttonWidth = 120;

			PermissionInfoLabel = new Label("Selected Permission Information");
			UsernameLabel = new Label($"Username: ");
			GroupLabel = new Label($"Group: ");

			SelectProfilesLabel = new Label("Select the profiles you want to add");

			SelectAll = new Button("Select All") { Width = buttonWidth };
			Add = new Button("Add >>") { Width = buttonWidth };
			Remove = new Button("<< Remove") { Width = buttonWidth };
			DeselectAll = new Button("Select All") { Width = buttonWidth };

			ProfilesCheckBoxList = new CheckBoxList(availableProfiles) { IsSorted = true };
			SelectedProfilesCheckBoxList = new CheckBoxList(selectedProfiles) { IsSorted = true };

			SaveButton = new Button("Save") { Style = ButtonStyle.CallToAction, Width = buttonWidth };
			CancelButton = new Button("Cancel") { Width = buttonWidth };

			// Add widgets to the dialog
			var layout = 0;
			var emptyColumn = 2;
			AddWidget(PermissionInfoLabel, layout, 0);
			AddWidget(UsernameLabel, ++layout, 0);
			AddWidget(GroupLabel, ++layout, 0);
			AddWidget(SelectProfilesLabel, ++layout, 0);

			AddWidget(SelectAll, ++layout, 0);
			AddWidget(Add, layout, 1);

			// Add a white space widget to separate buttons
			AddWidget(new WhiteSpace(), layout, emptyColumn);

			AddWidget(Remove, layout, 3);
			AddWidget(DeselectAll, layout, 4);

			AddWidget(ProfilesCheckBoxList, ++layout, 0, 1, 2, HorizontalAlignment.Left, VerticalAlignment.Top);
			AddWidget(SelectedProfilesCheckBoxList, layout, 3, 1, 2, HorizontalAlignment.Left, VerticalAlignment.Top);

			AddWidget(CancelButton, ++layout, 0);
			AddWidget(SaveButton, layout, 4);

			var columns = ColumnCount;

			for (var i = 0; i < columns; i++)
			{
				if (i == emptyColumn)
				{
					SetColumnWidth(i, 150);
				}

				SetColumnWidth(i, 200);
			}
		}

		private void InitData(Guid instance)
		{
			domInstance = domCache.GetInstanceById(instance);
			var savedProfiles = domInstance.GetFieldValue<string>("Nevion Control", "Profiles", domCache);
			var username = domInstance.GetFieldValue<string>("Basic Information", "Username", domCache);
			var group = domInstance.GetFieldValue<string>("Basic Information", "Group", domCache);

			UsernameLabel.Text += username.IsNullOrEmpty() ? "N/A" : username;
			GroupLabel.Text += group.IsNullOrEmpty() ? "N/A" : group;

			nevionProfiles = GetNevionProfiles();

			if (savedProfiles == "ALL")
			{
				selectedProfiles = nevionProfiles;
			}
			else if (!savedProfiles.IsNullOrEmpty())
			{
				selectedProfiles = savedProfiles.Split(',').ToList();
			}
			else
			{
				// No action
			}

			availableProfiles = nevionProfiles.Except(selectedProfiles).ToList();

			ProfilesCheckBoxList.SetOptions(availableProfiles);
			SelectedProfilesCheckBoxList.SetOptions(selectedProfiles);
		}

		private List<string> GetNevionProfiles()
		{
			var profilesTable = nevionElement.GetTable(2400);

			var tableData = profilesTable.GetData();

			var profiles = tableData.Values.Select(x => Convert.ToString(x[1])).ToList();

			return profiles;
		}

		private void CheckOptions(SelectType selection)
		{
			if (selection == SelectType.SelectAllLeft)
			{
				if (ProfilesCheckBoxList.Checked.Count() == ProfilesCheckBoxList.Options.Count())
				{
					ProfilesCheckBoxList.UncheckAll();
					return;
				}

				ProfilesCheckBoxList.CheckAll();
			}
			else if (selection == SelectType.SelectAllRight)
			{
				if (SelectedProfilesCheckBoxList.Checked.Count() == SelectedProfilesCheckBoxList.Options.Count())
				{
					SelectedProfilesCheckBoxList.UncheckAll();
					return;
				}

				SelectedProfilesCheckBoxList.CheckAll();
			}
			else if (selection == SelectType.Add)
			{
				var checkedProfiles = ProfilesCheckBoxList.Checked.ToList();

				if (checkedProfiles.Count == 0)
				{
					return;
				}

				// Remove options from available profiles
				var newAvailableProfiles = availableProfiles.Except(checkedProfiles).ToList();
				availableProfiles = newAvailableProfiles;
				ProfilesCheckBoxList.SetOptions(availableProfiles);

				// Add options to Selected Profiles
				selectedProfiles.AddRange(checkedProfiles);
				SelectedProfilesCheckBoxList.SetOptions(selectedProfiles);
			}
			else if (selection == SelectType.Remove)
			{
				var checkedSelectedProfiles = SelectedProfilesCheckBoxList.Checked.ToList();

				if (checkedSelectedProfiles.Count == 0)
				{
					return;
				}

				// Remove options from available profiles
				var newSelectedProfiles = selectedProfiles.Except(checkedSelectedProfiles).ToList();
				selectedProfiles = newSelectedProfiles;
				SelectedProfilesCheckBoxList.SetOptions(selectedProfiles);

				// Add options to Selected Profiles
				availableProfiles.AddRange(checkedSelectedProfiles);
				ProfilesCheckBoxList.SetOptions(availableProfiles);
			}
			else
			{
				// No action
			}
		}

		private void SaveToDom()
		{
			var newProfilesValue = string.Empty;
			if (selectedProfiles.Count == nevionProfiles.Count)
			{
				newProfilesValue = "ALL";
			}
			else if (selectedProfiles.Count > 0)
			{
				newProfilesValue = string.Join(",", selectedProfiles);
			}
			else
			{
				// No Action
			}

			domInstance.SetFieldValue("Nevion Control", "Profiles", newProfilesValue, domCache);
			domHelper.DomInstances.Update(domInstance);

			engine.ExitSuccess("Profiles saved successfully.");
		}
	}
}
