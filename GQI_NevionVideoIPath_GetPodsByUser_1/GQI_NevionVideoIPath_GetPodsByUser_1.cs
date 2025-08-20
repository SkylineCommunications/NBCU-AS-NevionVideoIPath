using System;
using System.Collections.Generic;
using System.Linq;

using NevionSharedUtils;

using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using Skyline.DataMiner.Net.LogHelpers;
using Skyline.DataMiner.Net.Messages;

[GQIMetaData(Name = "Nevion VideoIPath Get Pods by User")]
public class GQI_NevionVideoIPath_GetPodsByUser : IGQIDataSource, IGQIOnInit
{
	private GQIDMS _dms;
	private DomHelper domHelper;
	private int nevionDataminerId;
	private int nevionElementId;
	private int tagDataminerId;
	private int tagElementId;

	public OnInitOutputArgs OnInit(OnInitInputArgs args)
	{
		_dms = args.DMS;
		domHelper = new DomHelper(_dms.SendMessages, DomIds.Lca_Access.ModuleId);
		GQIUtils.GetNevionAndTagElement(_dms, out nevionDataminerId, out nevionElementId, out tagDataminerId, out tagElementId);

		return new OnInitOutputArgs();
	}

	public GQIColumn[] GetColumns()
	{
		return new GQIColumn[]
		{
			new GQIStringColumn("Pod"),
		};
	}

	public GQIPage GetNextPage(GetNextPageInputArgs args)
	{
		var permissions = GQIUtils.GetDOMPermissions(domHelper, "Destinations");

		if (permissions.Count == 0)
		{
			return new GQIPage(new GQIRow[0]);
		}

		var nevion = GQIUtils.GetElement(_dms, $"{nevionDataminerId}/{nevionElementId}");
		var tag = GQIUtils.GetElement(_dms, $"{tagDataminerId}/{tagElementId}");
		if (nevion == null || tag == null)
		{
			return new GQIPage(new GQIRow[0]);
		}

		var destinationTable = GQIUtils.GetTable(_dms, nevion, NevionIds.NevionDestinationsTable.TableId, new[] { "forceFullTable=true" });
		var channelTable = GQIUtils.GetTable(_dms, tag, TAGMCSIds.ChannelConfigTable.TablePid, new[] { "forceFullTable=true" });

		GQIUtils.GetUserDestinationPermissions(_dms, permissions, out var matchingTagList, out var matchingDestinationList);

		if (!matchingTagList.Any() && !matchingDestinationList.Any())
		{
			return new GQIPage(new GQIRow[0]);
		}

		var gqiRows = BuildRows(destinationTable, channelTable, matchingTagList, matchingDestinationList);

		return new GQIPage(gqiRows.ToArray());
	}

	public List<GQIRow> BuildRows(object[][] destinationTable, object[][] channelTable, HashSet<string> matchingTagList, HashSet<string> matchingDestinationList)
	{
		var pods = new List<string>();
		var gqiRows = new List<GQIRow>();
		foreach (var row in destinationTable)
		{
			var destinationTags = Convert.ToString(row[NevionIds.NevionDestinationsTable.Idx.Tags]);
			var destinationLabel = Convert.ToString(row[NevionIds.NevionDestinationsTable.Idx.DescriptorLabel]);
			if (!destinationLabel.StartsWith("[VIP RTP]") && !destinationLabel.StartsWith("[VIP SRT]"))
			{
				continue;
			}

			if (!channelTable.Any(channel => Convert.ToString(channel[TAGMCSIds.ChannelConfigTable.Idx.Label]).EndsWith(destinationLabel)))
			{
				continue;
			}

			var matchingTag = matchingTagList.Any(userTag => destinationTags.Contains(userTag) || userTag.Equals("ALL", StringComparison.OrdinalIgnoreCase));
			var matchingDestination = matchingDestinationList.Any(destination => destination == destinationLabel || destination.Equals("ALL", StringComparison.OrdinalIgnoreCase));
			if (matchingTag || matchingDestination)
			{
				var pod = destinationLabel.Replace("[VIP RTP]", string.Empty).Replace("[VIP SRT]", string.Empty).Trim();
				pods.Add(pod);
			}
		}

		var distinctPods = pods.Distinct();
		foreach (var pod in distinctPods)
		{
			gqiRows.Add(new GQIRow(new[]
			{
				new GQICell{ Value = pod, DisplayValue = pod },
			}));
		}

		return gqiRows;
	}
}