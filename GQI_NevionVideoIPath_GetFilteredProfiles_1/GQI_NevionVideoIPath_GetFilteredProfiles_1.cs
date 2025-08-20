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
			var valuesList = GQIUtils.GetDOMPermissions(domHelper, "Source");

			if (valuesList.Count == 0)
			{
				return new List<GQIRow>();
			}

			var nevionResponse = GQIUtils.GetElement(_dms, nevionElementId);

			if (nevionResponse != null)
			{
				GQIUtils.GetUserDestinationPermissions(_dms, valuesList, out var matchingTagList, out var matchingDestinationList);
				return AddRows(nevionResponse, matchingTagList.ToList());
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

		var profilesTable = GQIUtils.GetTable(_dms, nevionResponse, 2401, new[] { "forceFullTable=true" });

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

	public class DomInstanceValues
	{
		public string Username { get; set; }

		public string Group { get; set; }

		public string Tags { get; set; }
	}
}