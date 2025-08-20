using System;
using System.Collections.Generic;
using System.Linq;

using NevionSharedUtils;

using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using Skyline.DataMiner.Net.Helper;
using Skyline.DataMiner.Net.Messages;

[GQIMetaData(Name = "Nevion VideoIPath Get Destinations By Tags")]
public class GQI_NevionVideoIPath_GetDestinationsByTags : IGQIDataSource, IGQIOnInit, IGQIInputArguments
{
	private readonly GQIStringArgument sourceTagsArgument = new GQIStringArgument("Source Tags") { IsRequired = false };
	private readonly GQIStringArgument podArgument = new GQIStringArgument("Pod") { IsRequired = false };

	private string sourceTags;
	private string pod;

	private GQIDMS dms;
	private int dataminerId;
	private int elementId;

	private IGQILogger _logger;
	private DomHelper domHelper;

	public OnInitOutputArgs OnInit(OnInitInputArgs args)
	{
		dms = args.DMS;
		domHelper = new DomHelper(dms.SendMessages, DomIds.Lca_Access.ModuleId);
		_logger = args.Logger;
		GQIUtils.GetNevionAndTagElement(dms, out dataminerId, out elementId, out _, out _);

		return new OnInitOutputArgs();
	}

	public GQIColumn[] GetColumns()
	{
		return new GQIColumn[]
		{
			new GQIStringColumn("Name"),
			new GQIStringColumn("Connected Source"),
			new GQIStringColumn("Tags"),
			new GQIStringColumn("ID"),
			new GQIBooleanColumn("Is Connected"),
		};
	}

	public GQIArgument[] GetInputArguments()
	{
		return new GQIArgument[] { sourceTagsArgument, podArgument };
	}

	public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
	{
		sourceTags = args.GetArgumentValue<string>(sourceTagsArgument);
		pod = args.GetArgumentValue<string>(podArgument);
		return new OnArgumentsProcessedOutputArgs();
	}

	public GQIPage GetNextPage(GetNextPageInputArgs args)
	{
		if (dataminerId == -1 || elementId == -1)
		{
			return new GQIPage(new GQIRow[0])
			{
				HasNextPage = false,
			};
		}

		List<GQIRow> rows = GetRows();

		return new GQIPage(rows.ToArray())
		{
			HasNextPage = false,
		};
	}

	private List<GQIRow> GetRows()
	{
		var permissionList = GQIUtils.GetDOMPermissions(domHelper, "Destination");
		var rows = new List<GQIRow>();
		if (permissionList.Count == 0)
		{
			return rows;
		}

		GQIUtils.GetUserDestinationPermissions(dms, permissionList, out var matchingTagList, out var matchingDestinationList);

		if (!matchingTagList.Any() && !matchingDestinationList.Any())
		{
			return rows;
		}

		return GetDestinationByTagRows(matchingTagList, matchingDestinationList);
	}

	private List<GQIRow> GetDestinationByTagRows(HashSet<string> tagFilter, HashSet<string> allowedDestinations)
	{
		var columns = GetDestinationTableColumns();
		if (!columns.Any())
		{
			return new List<GQIRow>();
		}

		var destinationIdSourceNameRelations = GetCurrentServicesDestinationIdToSourceNameRelationDictionary();

		return ProcessDestinationByTagTable(columns, destinationIdSourceNameRelations, tagFilter, allowedDestinations);
	}

	private ParameterValue[] GetDestinationTableColumns()
	{
		var getPartialTableMessage = new GetPartialTableMessage(dataminerId, elementId, 1400, new[] { "forceFullTable=true" });
		var parameterChangeEventMessage = (ParameterChangeEventMessage)dms.SendMessage(getPartialTableMessage);
		if (parameterChangeEventMessage.NewValue?.ArrayValue == null)
		{
			return new ParameterValue[0];
		}

		var columns = parameterChangeEventMessage.NewValue.ArrayValue;
		if (columns.Length < 6)
		{
			return new ParameterValue[0];
		}

		return columns;
	}

	private List<GQIRow> ProcessDestinationByTagTable(ParameterValue[] columns, Dictionary<string, string> destinationIdToSourceNameRelations, HashSet<string> tagFilter, HashSet<string> allowedDestinations)
	{
		var rows = new List<GQIRow>();
		bool allDestinations = allowedDestinations.Count == 1 && allowedDestinations.First().Equals("ALL", StringComparison.OrdinalIgnoreCase);
		bool allPods = pod.IsNullOrEmpty() || pod.Equals("ALL", StringComparison.OrdinalIgnoreCase);
		bool filterBySourceTags = !String.IsNullOrWhiteSpace(sourceTags);
		bool sourceIsSrt = filterBySourceTags && sourceTags.Contains("SRT");

		for (int i = 0; i < columns[0].ArrayValue.Length; i++)
		{
			var destinationRow = new DestinationByTagRow(columns, i, destinationIdToSourceNameRelations);
			if (!destinationRow.IsValid())
			{
				continue;
			}

			if (!destinationRow.MatchesTagFilter(tagFilter))
			{
				continue;
			}

			var destinationTags = destinationRow.Tags ?? Array.Empty<string>();
			if (filterBySourceTags && IsTagMatchRequired(sourceIsSrt, destinationTags))
			{
				continue;
			}

			if (!allDestinations && !allowedDestinations.Contains(destinationRow.DescriptorLabel))
			{
				continue;
			}

			var destinationPod = destinationRow.DescriptorLabel.Replace("[VIP RTP]", string.Empty).Replace("[VIP SRT]", string.Empty).Trim();
			if (!allPods && destinationPod != pod)
			{
				continue;
			}

			rows.Add(destinationRow.ToGqiRow());
		}

		return rows;
	}

	private static bool IsTagMatchRequired(bool sourceIsSrt, IReadOnlyCollection<string> destinationTags)
	{
		if (destinationTags == null)
		{
			return false;
		}

		bool destinationIsSrt = destinationTags.Any(x => x.Contains("SRT"));
		return (sourceIsSrt && !destinationIsSrt) || (!sourceIsSrt && destinationIsSrt);
	}

	private Dictionary<string, string> GetCurrentServicesDestinationIdToSourceNameRelationDictionary()
	{
		var columns = GetCurrentServicesTableColumns();
		if (!columns.Any())
		{
			return new Dictionary<string, string>();
		}

		var destinationIdToSourceNameDic = new Dictionary<string, string>();

		for (int i = 0; i < columns[7].ArrayValue.Length; i++)
		{
			var destinationIdCell = columns[7].ArrayValue[i];
			if (destinationIdCell.IsEmpty)
			{
				continue;
			}

			var sourceNameCell = columns[2].ArrayValue[i];
			if (sourceNameCell.IsEmpty)
			{
				continue;
			}

			if (!destinationIdToSourceNameDic.ContainsKey(destinationIdCell.CellValue.StringValue))
			{
				destinationIdToSourceNameDic.Add(destinationIdCell.CellValue.StringValue, sourceNameCell.CellValue.StringValue);
			}
		}

		return destinationIdToSourceNameDic;
	}

	private ParameterValue[] GetCurrentServicesTableColumns()
	{
		var tableId = 1500;
		var getPartialTableMessage = new GetPartialTableMessage(dataminerId, elementId, tableId, new[] { "forceFullTable=true" });
		var parameterChangeEventMessage = (ParameterChangeEventMessage)dms.SendMessage(getPartialTableMessage);
		if (parameterChangeEventMessage.NewValue?.ArrayValue == null)
		{
			return new ParameterValue[0];
		}

		var columns = parameterChangeEventMessage.NewValue.ArrayValue;
		if (columns.Length < 8)
		{
			return new ParameterValue[0];
		}

		return columns;
	}
}

public class DestinationByTagRow
{
	public DestinationByTagRow(ParameterValue[] columns, int row, Dictionary<string, string> destinationIdToSourceNameRelations)
	{
		var nameCell = columns[0].ArrayValue[row];
		Name = !nameCell.IsEmpty ? nameCell.CellValue.StringValue : String.Empty;

		var idCell = columns[1].ArrayValue[row];
		Id = !idCell.IsEmpty ? idCell.CellValue.StringValue : String.Empty;

		var tagsCell = columns[3].ArrayValue[row];
		if (!tagsCell.IsEmpty)
		{
			Tags = tagsCell.CellValue.StringValue
				.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(t => t.Trim())
				.ToArray();
		}

		var descriptionCell = columns[2].ArrayValue[row];
		Description = !descriptionCell.IsEmpty ? descriptionCell.CellValue.StringValue : String.Empty;

		var descriptorLabelCell = columns[4].ArrayValue[row];
		DescriptorLabel = !descriptorLabelCell.IsEmpty ? descriptorLabelCell.CellValue.StringValue : String.Empty;

		var fDescriptorLabelCell = columns[5].ArrayValue[row];
		FDescriptorLabel = !fDescriptorLabelCell.IsEmpty ? fDescriptorLabelCell.CellValue.StringValue : String.Empty;

		ConnectedSource = destinationIdToSourceNameRelations.TryGetValue(Id, out string sourceName) ? sourceName : string.Empty;
	}

	public string Name { get; private set; }

	public string Id { get; private set; }

	public string ConnectedSource { get; private set; }

	public string Description { get; private set; }

	public string[] Tags { get; private set; } = new string[0];

	public string DescriptorLabel { get; private set; }

	public string FDescriptorLabel { get; private set; }

	public bool IsValid()
	{
		if (String.IsNullOrEmpty(Name))
		{
			return false;
		}

		if (String.IsNullOrEmpty(Id))
		{
			return false;
		}

		if (String.IsNullOrEmpty(DescriptorLabel) && String.IsNullOrEmpty(FDescriptorLabel))
		{
			return false;
		}

		return true;
	}

	public bool MatchesTagFilter(HashSet<string> filter)
	{
		if (filter == null || !filter.Any() || filter.Contains("ALL"))
		{
			return true;
		}

		return Tags.Intersect(filter).Any();
	}

	public GQIRow ToGqiRow()
	{
		var descriptorLabel = !String.IsNullOrWhiteSpace(DescriptorLabel) ? DescriptorLabel : FDescriptorLabel;
		return new GQIRow(
			new[]
			{
				new GQICell { Value = descriptorLabel },
				new GQICell { Value = ConnectedSource.Trim() },
				new GQICell { Value = String.Join(",", Tags) },
				new GQICell { Value = Id },
				new GQICell { Value = !String.IsNullOrWhiteSpace(ConnectedSource) },
			});
	}
}