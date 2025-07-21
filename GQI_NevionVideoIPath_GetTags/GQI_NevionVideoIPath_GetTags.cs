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

[GQIMetaData(Name = "Nevion VideoIPath Get Tags")]
public class GQI_NevionVideoIPath_GetTags : IGQIDataSource, IGQIOnInit, IGQIInputArguments
{
	private GQIStringDropdownArgument typeArgument = new GQIStringDropdownArgument("Type", new[] { "Source", "Destination" }) { IsRequired = true };
	private string type;

	private GQIStringArgument nevionElementNameArgument = new GQIStringArgument("Element Name") { IsRequired = false };
	private string elementName;

	private GQIDMS _dms;
	private DomHelper domHelper;

	public OnInitOutputArgs OnInit(OnInitInputArgs args)
	{
		_dms = args.DMS;
		domHelper = new DomHelper(_dms.SendMessages, DomIds.Lca_Access.ModuleId);
		return new OnInitOutputArgs();
	}

	private enum Type
	{
		Source = 0,
		Destination = 1,
	}

	public GQIColumn[] GetColumns()
	{
		return new GQIColumn[]
		{
			new GQIStringColumn("Tag"),
		};
	}

	public GQIArgument[] GetInputArguments()
	{
		return new GQIArgument[] { typeArgument, nevionElementNameArgument };
	}

	public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
	{
		type = args.GetArgumentValue(typeArgument);
		elementName = Convert.ToString(args.GetArgumentValue(nevionElementNameArgument));
		return new OnArgumentsProcessedOutputArgs();
	}

	public GQIPage GetNextPage(GetNextPageInputArgs args)
	{
		List<GQIRow> rows = GetRows();

		return new GQIPage(rows.ToArray())
		{
			HasNextPage = false,
		};
	}

	private List<GQIRow> GetRows()
	{
		var valuesList = GetDomInstanceData();

		if (valuesList.Count == 0)
		{
			return new List<GQIRow>();
		}

		var nevionElementRequest = new GetLiteElementInfo
		{
			NameFilter = elementName,
			ProtocolVersion = "Production",
		};

		var nevionResponse = _dms.SendMessage(nevionElementRequest) as LiteElementInfoEvent;

		if (nevionResponse == null)
		{
			return new List<GQIRow>();
		}

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

		return new List<GQIRow>();
	}

	private List<string> GetTagsByType(LiteElementInfoEvent nevionResponse)
	{
		var tablePid = type == "Source" ? 1300 : 1400;
		var nevionTable = GQIUtils.GetTable(_dms, nevionResponse, tablePid, new[] { "forceFullTable=true" });

		var tagsList = new List<string>();
		for (int i = 0; i < nevionTable.Length; i++)
		{
			var row = nevionTable[i];

			var tag = Convert.ToString(row[3]);

			if (!tag.IsNullOrEmpty())
			{
				var tags = tag.Split(',').ToList();

				if (tags.Any())
				{
					tagsList.AddRange(tags);
				}
			}
		}

		tagsList = tagsList.Distinct().ToList();

		return tagsList;
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

	private List<GQIRow> AddRows(LiteElementInfoEvent nevionResponse, List<string> matchingTagsList)
	{
		if (nevionResponse.State != ElementState.Active)
		{
			return new List<GQIRow>();
		}

		var tags = GetTagsByType(nevionResponse);

		var rows = new List<GQIRow>();
		if (matchingTagsList.Contains("ALL"))
		{
			foreach (var tag in tags)
			{
				var cells = new[]
				{
					new GQICell { Value = tag }, // Tags
				};

				var row = new GQIRow(cells);

				rows.Add(row);
			}

			return rows;
		}

		var matchingTags = tags.Where(tag => matchingTagsList.Contains(tag.Trim()));
		if (matchingTags.Any())
		{
			foreach (var tag in matchingTags)
			{
				var cells = new[]
				{
					new GQICell { Value = tag }, // Tags
				};

				var row = new GQIRow(cells);
				rows.Add(row);
			}

			return rows;
		}

		return new List<GQIRow>();
	}

	private List<DomInstanceValues> GetDomInstanceData()
	{
		var instances = domHelper.DomInstances.Read(DomInstanceExposers.DomDefinitionId.Equal(Lca_Access.Definitions.Nevion_Control.Id));
		var valuesList = new List<DomInstanceValues>();
		foreach (var instance in instances)
		{
			var username = instance.GetFieldValue<string>(Lca_Access.Sections.BasicInformation.Id, Lca_Access.Sections.BasicInformation.Username)?.Value;
			var group = instance.GetFieldValue<string>(Lca_Access.Sections.BasicInformation.Id, Lca_Access.Sections.BasicInformation.Group)?.Value;
			var domTags = instance.GetFieldValue<string>(Lca_Access.Sections.NevionControl.Id, Lca_Access.Sections.NevionControl.Profiles)?.Value;

			valuesList.Add(new DomInstanceValues { Username = username, Group = group, Tags = domTags });
		}

		return valuesList;
	}

	public class DomInstanceValues
	{
		public string Username { get; set; }

		public string Group { get; set; }

		public string Tags { get; set; }
	}
}