using System;
using System.Collections.Generic;
using System.Linq;

using NevionSharedUtils;

using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using Skyline.DataMiner.Net.Helper;
using Skyline.DataMiner.Net.Messages;

[GQIMetaData(Name = "Nevion VideoIPath Get Tags")]
public class GQI_NevionVideoIPath_GetTags : IGQIDataSource, IGQIOnInit, IGQIInputArguments
{
	private GQIStringDropdownArgument typeArgument = new GQIStringDropdownArgument("Type", new[] { "Source", "Destination" }) { IsRequired = true };
	private string type;

	private GQIStringArgument nevionElementNameArgument = new GQIStringArgument("Element Name") { IsRequired = false };
	private string elementName;

	private IGQILogger _logger;

	private GQIDMS _dms;
	private DomHelper domHelper;

	public OnInitOutputArgs OnInit(OnInitInputArgs args)
	{
		_dms = args.DMS;
		_logger = args.Logger;
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
		var valuesList = GQIUtils.GetDOMTags(domHelper, type);
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
			var matchingTagList = String.IsNullOrEmpty(matchingByUsername.Tags) ? new List<string>() : matchingByUsername.Tags.Split(',').ToList();
			return AddRows(nevionResponse, matchingTagList);
		}

		// Group Data
		var securityResponse = responses?.OfType<GetUserInfoResponseMessage>().FirstOrDefault();

		var groupNames = securityResponse.FindGroupNamesByUserName(systemUserName).ToList();

		if (matchingByUsername == null && groupNames.Count > 0)
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
					tagsList.AddRange(tags.Select(x => x.Trim()));
				}
			}
		}

		tagsList = tagsList.Distinct().ToList();

		return tagsList;
	}

	private List<string> MatchingTagsByGroup(List<GQIUtils.DomInstanceValues> valuesList, List<string> groupNames)
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

		if (matchingTagsList == null || !matchingTagsList.Any())
		{
			return new List<GQIRow>();
		}

		var tags = GetTagsByType(nevionResponse);

		var rows = new List<GQIRow>();
		if (matchingTagsList.Contains("ALL"))
		{
			foreach (var tag in tags.Distinct())
			{
				var cells = new[]
				{
					new GQICell { Value = tag.Trim() }, // Tags
				};

				var row = new GQIRow(cells);

				rows.Add(row);
			}

			return rows;
		}

		var matchingTags = tags.Where(tag => matchingTagsList.Contains(tag.Trim()));
		if (matchingTags.Any())
		{
			foreach (var tag in matchingTags.Distinct())
			{
				var cells = new[]
				{
					new GQICell { Value = tag.Trim() }, // Tags
				};

				var row = new GQIRow(cells);
				rows.Add(row);
			}

			return rows;
		}

		return new List<GQIRow>();
	}
}