using System;
using System.Collections.Generic;
using System.Linq;

using DomIds;

using NevionSharedUtils;

using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using Skyline.DataMiner.Net.Helper;
using Skyline.DataMiner.Net.Messages;
using Skyline.DataMiner.Net.Messages.SLDataGateway;
using Skyline.DataMiner.Net.Sections;

using SLDataGateway.API.Types.Connections;

[GQIMetaData(Name = "Nevion VideoIPath Get Filtered Profiles")]
public class GetFilteredProfiles : IGQIDataSource, IGQIOnInit, IGQIInputArguments
{
	private GQIStringArgument elementIdArgument = new GQIStringArgument("Nevion Element ID") { IsRequired = true };

	private IGQILogger _logger;
	private GQIDMS _dms;
	private DomHelper domHelper;

	private string nevionElementId;

	public OnInitOutputArgs OnInit(OnInitInputArgs args)
	{
		_dms = args.DMS;
		domHelper = new DomHelper(_dms.SendMessages, DomIds.Lca_Access.ModuleId);
		_logger = args.Logger;
		return new OnInitOutputArgs();
	}

	public GQIArgument[] GetInputArguments()
	{
		return new GQIArgument[] { elementIdArgument };
	}

	public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
	{
		nevionElementId = args.GetArgumentValue<string>(elementIdArgument);
		return new OnArgumentsProcessedOutputArgs();
	}

	public GQIColumn[] GetColumns()
	{
		return new GQIColumn[]
		{
			new GQIStringColumn("Profile ID"),
			new GQIStringColumn("Profile Name"),
			new GQIStringColumn("Tags"),
		};
	}

	public GQIPage GetNextPage(GetNextPageInputArgs args)
	{
		List<GQIRow> rows = BuildRows();

		return new GQIPage(rows.ToArray())
		{
			HasNextPage = false,
		};
	}

	private List<GQIRow> BuildRows()
	{
		try
		{
			var instances = domHelper.DomInstances.Read(DomInstanceExposers.DomDefinitionId.Equal(Lca_Access.Definitions.Nevion_Control.Id));
			var valuesList = new List<DomInstanceValues>();
			foreach (var instance in instances)
			{
				var username = instance.GetFieldValue<string>(Lca_Access.Sections.BasicInformation.Id, Lca_Access.Sections.BasicInformation.Username)?.Value;
				var group = instance.GetFieldValue<string>(Lca_Access.Sections.BasicInformation.Id, Lca_Access.Sections.BasicInformation.Group)?.Value;
				var tags = instance.GetFieldValue<string>(Lca_Access.Sections.NevionControl.Id, Lca_Access.Sections.NevionControl.Profiles)?.Value;

				valuesList.Add(new DomInstanceValues { Username = username, Group = group, Tags = tags });
			}

			if (valuesList.Count == 0)
			{
				return new List<GQIRow>();
			}

			var sElementId = nevionElementId.Split('/');
			var nevionElementRequest = new GetLiteElementInfo
			{
				DataMinerID = Convert.ToInt32(sElementId[0]),
				ElementID = Convert.ToInt32(sElementId[1]),
			};

			var nevionResponse = _dms.SendMessage(nevionElementRequest) as LiteElementInfoEvent;

			if (nevionResponse != null)
			{
				var responses = _dms.SendMessages(new GetUserFullNameMessage(), new GetInfoMessage(InfoType.SecurityInfo));
				var systemUserName = responses?.OfType<GetUserFullNameResponseMessage>().FirstOrDefault()?.User.Trim();
				var matchingByUsername = valuesList.FirstOrDefault(instance => instance.Username == systemUserName);
				if (matchingByUsername != null)
				{
					var matchingTagList = matchingByUsername.Tags.Split(',').ToList();
					return AddRows(nevionResponse, matchingTagList);
				}

				// Group Data
				var securityResponse = responses?.OfType<GetUserInfoResponseMessage>().FirstOrDefault();

				var groupNames = securityResponse.FindGroupNamesByUserName(systemUserName).ToList();

				if (groupNames.Count > 0)
				{
					var matchingTagsByGroup = MatchingTagsByGroup(valuesList, groupNames);
					return AddRows(nevionResponse, matchingTagsByGroup);
				}
			}

			return new List<GQIRow>();
		}
		catch (Exception e)
		{
			return new List<GQIRow>();
		}
	}

	private List<GQIRow> AddRows(LiteElementInfoEvent nevionResponse, List<string> matchingTagsList)
	{
		if (nevionResponse.State != ElementState.Active)
		{
			return new List<GQIRow>();
		}

		var profilesTable = GQIUtils.GetTable(_dms, nevionResponse, 2401 , new[] { "forceFullTable=true" });

		var rows = new List<GQIRow>();
		for (int i = 0; i < profilesTable.Length; i++)
		{
			var profileRow = profilesTable[i];

			var tags = Convert.ToString(profileRow[3]);

			if (tags.IsNullOrEmpty() && !matchingTagsList.Contains("ALL"))
			{
				continue;
			}

			var profileId = Convert.ToString(profileRow[0]);
			var profileName = Convert.ToString(profileRow[1]);

			if (matchingTagsList.Contains("ALL"))
			{
				var cells = new[]
				{
					new GQICell { Value = profileId }, // Profile Id
					new GQICell { Value = profileName}, // Profile Name
					new GQICell { Value = tags }, // Tags
				};

				var row = new GQIRow(profileId, cells);

				rows.Add(row);
				continue;
			}

			var tagList = tags.Split(',');
			var hasMatch = tagList.Any(tag => matchingTagsList.Contains(tag.Trim()));
			if (hasMatch)
			{
				var cells = new[]
				{
					new GQICell { Value = profileId }, // Profile Id
					new GQICell { Value = profileName}, // Profile Name
					new GQICell { Value = tags }, // Tags
				};
				var row = new GQIRow(profileId, cells);
				rows.Add(row);
			}
		}

		return rows;
	}

	private List<string> MatchingTagsByGroup(List<DomInstanceValues> valuesList, List<string> groupNames)
	{
		var tagList = new List<string>();
		foreach (var group in groupNames)
		{
			var matchingGroup = valuesList.FirstOrDefault(x => x.Group == group);
			if (matchingGroup != null)
			{
				if (!matchingGroup.Tags.IsNullOrEmpty())
				{
					var domTags = matchingGroup.Tags.Split(',').ToList();
					tagList.AddRange(domTags);
				}
			}
		}

		return tagList.Distinct().ToList();
	}

	public class DomInstanceValues
	{
		public string Username { get; set; }

		public string Group { get; set; }

		public string Tags { get; set; }
	}
}
