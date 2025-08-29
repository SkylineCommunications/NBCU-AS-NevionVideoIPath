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
using System.Runtime.Remoting.Messaging;
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

		var outputConfigTableFilter = new[]
		{
			$"columns={TAGMCSIds.OutputConfigTable.Pid.Label}",
		};
		var outputConfigTable = GQIUtils.GetTable(dms, Convert.ToInt32(tagId[0]), Convert.ToInt32(tagId[1]), TAGMCSIds.OutputConfigTable.TablePid, outputConfigTableFilter);

		foreach (var row in channelTable)
		{
			var channelLabel = Convert.ToString(row[TAGMCSIds.ChannelConfigTable.Idx.Label]);
			var channelName = channelLabel.Split(new[] { "->" }, StringSplitOptions.None).Last();
			if (CheckOutput(channelName, destinationList))
			{
				continue;
			}

			var nevionRow = nevionDestinationTable.FirstOrDefault(x => Convert.ToString(x[NevionIds.NevionDestinationsTable.Idx.DescriptorLabel]) == channelName);
			if (nevionRow == null)
			{
				continue;
			}

			var destinationTags = Convert.ToString(nevionRow[NevionIds.NevionDestinationsTable.Idx.Tags]);
			if (!tagList.Any(tag => destinationTags.Contains(tag) || tag.ToUpper() == "ALL"))
			{
				continue;
			}

			var outputName = GQIUtils.RemoveBracketPrefix(channelName);
			var outputConfigRow = outputConfigTable.FirstOrDefault(x => Convert.ToString(x[TAGMCSIds.OutputConfigTable.Idx.Label]) == outputName);
			if (outputConfigRow == null)
			{
				continue;
			}

			var outputId = Convert.ToString(outputConfigRow[TAGMCSIds.OutputConfigTable.Idx.Index]);

			var gqiRow = rows.FirstOrDefault(r => Convert.ToString(r.Cells[0].Value) == outputId);
			if (gqiRow == null)
			{
				gqiRow = new GQIRow(new GQICell[6]);
			}
			else
			{
				rows.Remove(gqiRow);
			}

			gqiRow = BuildRow(outputAudiosTable, gqiRow, outputId, outputName);

			rows.Add(gqiRow);
		}

		return rows;
	}

	private bool CheckOutput(string destinationLabel, HashSet<string> destinationList)
	{
		if (!destinationList.Any(label => label == destinationLabel || (label.ToUpper() == "ALL" && destinationLabel.Contains("Routable"))))
		{
			return true;
		}

		if (outputFilter.IsNotNullOrEmpty() && !destinationLabel.ToLower().Contains(outputFilter.ToLower()))
		{
			return true;
		}

		return false;
	}

	private GQIRow BuildRow(object[][] outputAudiosTable, GQIRow gqiRow, string outputId, string outputName)
	{
		var outputAudioId = $"{outputId}/1";
		var outputAudioRow = outputAudiosTable.FirstOrDefault(r => Convert.ToString(r[TAGMCSIds.OutputAudiosTable.Idx.Index]) == outputAudioId);
		if (outputAudioRow == null)
		{
			gqiRow.Cells[0] = new GQICell { Value = outputId };
			gqiRow.Cells[1] = new GQICell { Value = outputName };
			gqiRow.Cells[2] = new GQICell { Value = "N/A" };
			gqiRow.Cells[3] = new GQICell { Value = "N/A" };
			gqiRow.Cells[4] = new GQICell { Value = "N/A" };
			gqiRow.Cells[5] = new GQICell { Value = "N/A" };
			return gqiRow;
		}

		var currentChannelId = Convert.ToString(outputAudioRow[TAGMCSIds.OutputAudiosTable.Idx.ChannelID]);
		var currentChannel = Convert.ToString(outputAudioRow[TAGMCSIds.OutputAudiosTable.Idx.Channel]);
		var currentPid = Convert.ToString(outputAudioRow[TAGMCSIds.OutputAudiosTable.Idx.InputPID]);

		gqiRow.Cells[0] = new GQICell { Value = outputId };
		gqiRow.Cells[1] = new GQICell { Value = outputName };
		gqiRow.Cells[2] = new GQICell { Value = currentChannelId.IsNullOrEmpty() || currentChannelId == "None" ? "N/A" : currentChannelId };
		gqiRow.Cells[3] = new GQICell { Value = currentChannel.IsNullOrEmpty() || currentChannel == "Not Set" ? "N/A" : currentChannel };
		gqiRow.Cells[4] = new GQICell { Value = currentPid.IsNullOrEmpty() || currentPid == "-1" ? "N/A" : currentPid };
		var outputMask = Convert.ToInt32(outputAudioRow[TAGMCSIds.OutputAudiosTable.Idx.OutputMask]);
		gqiRow.Cells[5] = new GQICell { Value = GQIUtils.ChannelMaskingMap.TryGetValue(outputMask, out var value) ? value : "N/A" };

		return gqiRow;
	}
}