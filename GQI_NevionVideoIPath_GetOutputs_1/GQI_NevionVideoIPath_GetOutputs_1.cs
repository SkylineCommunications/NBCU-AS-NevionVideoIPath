/*
****************************************************************************
*  Copyright (c) 2025,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

dd/mm/2025	1.0.0.1		XXX, Skyline	Initial version
****************************************************************************
*/

using System;
using System.Collections.Generic;
using System.Linq;
using NevionSharedUtils;
using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using Skyline.DataMiner.Net.Helper;

[GQIMetaData(Name = "Nevion Video IPath Get Outputs")]
public class GQI_NevionVideoIPath_GetOutputs : IGQIDataSource, IGQIOnInit, IGQIInputArguments
{
	private GQIStringArgument OutputFilterArgument = new GQIStringArgument("Output Filter") { IsRequired = false };

	private string outputFilter;
	private string[] nevionId;
	private string[] tagId;
	private GQIDMS dms;
	private DomHelper domHelper;

	public GQIArgument[] GetInputArguments()
	{
		return new[] { OutputFilterArgument };
	}

	public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
	{
		outputFilter = args.GetArgumentValue(OutputFilterArgument);
		return default;
	}

	public OnInitOutputArgs OnInit(OnInitInputArgs args)
	{
		dms = args.DMS;
		domHelper = new DomHelper(dms.SendMessages, DomIds.Lca_Access.ModuleId);
		nevionId = GQIUtils.GetElementId(dms, "Nevion Video iPath").Split('/');
		tagId = GQIUtils.GetElementId(dms, "TAG Video Systems Media Control System (MCS)").Split('/');
		return default;
	}

	public GQIColumn[] GetColumns()
	{
		return new GQIColumn[]
		{
			new GQIStringColumn("Output ID"),
			new GQIStringColumn("Output Name"),
			new GQIStringColumn("Channel ID"),
			new GQIStringColumn("Channel Name"),
			new GQIStringColumn("PID"),
			new GQIStringColumn("Audio Channel"),
		};
	}

	public GQIPage GetNextPage(GetNextPageInputArgs args)
	{
		GQIUtils.GetUserDestinationPermissions(dms, domHelper, out var matchingTagList, out var matchingDestinationList);

		if (nevionId[0] == "-1" || nevionId[1] == "-1" || tagId[0] == "-1" || tagId[0] == "-1")
		{
			return new GQIPage(new GQIRow[0]);
		}

		if (!matchingTagList.Any() || !matchingDestinationList.Any())
		{
			return new GQIPage(new GQIRow[0]);
		}

		return new GQIPage(BuildRows(matchingTagList, matchingDestinationList).ToArray());
	}

	private List<GQIRow> BuildRows(HashSet<string> tagList, HashSet<string> destinationList)
	{
		var rows = new List<GQIRow>();

		var nevionDestinationTable = GQIUtils.GetTable(dms, Convert.ToInt32(nevionId[0]), Convert.ToInt32(nevionId[1]), NevionIds.NevionDestinationsTable.TableId, new[] { "forceFullTable=true" });

		var channelTableFilter = new[] { $"columns={TAGMCSIds.ChannelConfigTable.Pid.Label}" };
		var channelTable = GQIUtils.GetTable(dms, Convert.ToInt32(tagId[0]), Convert.ToInt32(tagId[1]), TAGMCSIds.ChannelConfigTable.TablePid, channelTableFilter);

		var outputAudioTableFilter = new[]
		{
			$"fullFilter=({TAGMCSIds.OutputAudiosTable.Pid.ChannelID}!=None) AND ({TAGMCSIds.OutputAudiosTable.Pid.OutputPID}==202)",
		};
		var outputAudiosTable = GQIUtils.GetTable(dms, Convert.ToInt32(tagId[0]), Convert.ToInt32(tagId[1]), TAGMCSIds.OutputAudiosTable.TablePid, outputAudioTableFilter);

		foreach (var row in nevionDestinationTable)
		{
			var destinationTags = Convert.ToString(row[NevionIds.NevionDestinationsTable.Idx.Tags]);
			var destinationLabel = Convert.ToString(row[NevionIds.NevionDestinationsTable.Idx.DescriptorLabel]);

			if (!tagList.Any(tag => destinationTags.Contains(tag) || tag.ToUpper() == "ALL"))
			{
				continue;
			}

			if (CheckOutput(destinationLabel, destinationList))
			{
				continue;
			}

			var channelRow = channelTable.FirstOrDefault(cRow =>
			{
				var label = Convert.ToString(cRow[TAGMCSIds.ChannelConfigTable.Idx.Label]).Split(new[] { "->" }, StringSplitOptions.None);
				if (label.Length != 2)
				{
					return false;
				}

				return label[1] == destinationLabel;
			});

			if (channelRow == null)
			{
				continue;
			}

			var channelId = Convert.ToString(channelRow[TAGMCSIds.ChannelConfigTable.Idx.Id]);
			var channelName = Convert.ToString(channelRow[TAGMCSIds.ChannelConfigTable.Idx.Label]);

			var outputAudioRow = outputAudiosTable.FirstOrDefault(audioRow => Convert.ToString(audioRow[TAGMCSIds.OutputAudiosTable.Idx.ChannelID]) == channelId);
			if (outputAudioRow == null)
			{
				continue;
			}

			rows.Add(new GQIRow(new[]
			{
				new GQICell { Value = Convert.ToString(outputAudioRow[TAGMCSIds.OutputAudiosTable.Idx.OutputID]) },
				new GQICell { Value = Convert.ToString(outputAudioRow[TAGMCSIds.OutputAudiosTable.Idx.Output]) },
				new GQICell { Value = channelId },
				new GQICell { Value = channelName },
				new GQICell { Value = Convert.ToString(outputAudioRow[TAGMCSIds.OutputAudiosTable.Idx.InputPID]) },
				new GQICell { Value = GQIUtils.ChannelMaskingMap.TryGetValue(Convert.ToInt32(outputAudioRow[TAGMCSIds.OutputAudiosTable.Idx.OutputMask]), out var value) ? value : "N/A" },
			}));
		}

		return rows;
	}

	private bool CheckOutput(string destinationLabel, HashSet<string> destinationList)
	{
		if (!destinationList.Any(label => label == destinationLabel || (label.ToUpper() == "ALL" && destinationLabel.Contains("Routable"))))
		{
			return true;
		}

		if (outputFilter.IsNotNullOrEmpty() && !destinationLabel.Contains(outputFilter))
		{
			return true;
		}

		return false;
	}
}