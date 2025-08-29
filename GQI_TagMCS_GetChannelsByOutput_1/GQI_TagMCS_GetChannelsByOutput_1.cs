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
using System.ComponentModel;
using System.Linq;
using NevionSharedUtils;
using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using Skyline.DataMiner.Net.Helper;

[GQIMetaData(Name = "TAG MCS Get Channels By Output")]
public class GQI_TagMCS_GetChannelsByOutput : IGQIDataSource, IGQIOnInit, IGQIInputArguments
{
	private GQIStringArgument OutputIdArgument = new GQIStringArgument("Output ID") { IsRequired = false };

	private string outputId;
	private GQIDMS dms;
	private int dataminerId;
	private int elementId;

	public GQIArgument[] GetInputArguments()
	{
		return new[] { OutputIdArgument };
	}

	public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
	{
		outputId = args.GetArgumentValue(OutputIdArgument);
		return default;
	}

	public GQIColumn[] GetColumns()
	{
		return new GQIColumn[]
		{
			new GQIStringColumn("Channel ID"),
			new GQIStringColumn("Channel Name"),
			new GQIBooleanColumn("Selected"),
		};
	}

	public OnInitOutputArgs OnInit(OnInitInputArgs args)
	{
		dms = args.DMS;
		var tagId = GQIUtils.GetElementId(dms, "TAG Video Systems Media Control System (MCS)").Split('/');
		dataminerId = Convert.ToInt32(tagId[0]);
		elementId = Convert.ToInt32(tagId[1]);
		return default;
	}

	public GQIPage GetNextPage(GetNextPageInputArgs args)
	{
		if (dataminerId == -1 || elementId == -1 || outputId.IsNullOrEmpty())
		{
			return new GQIPage(new GQIRow[0]);
		}

		return new GQIPage(BuildRows().ToArray());
	}

	private List<GQIRow> BuildRows()
	{
		var rows = new List<GQIRow>();

		var outputLayoutTable = GQIUtils.GetTable(dms, dataminerId, elementId, TAGMCSIds.OutputsLayoutsTable.TablePid, new[] { $"fullFilter={TAGMCSIds.OutputsLayoutsTable.Pid.OutputID}=={outputId}" });
		var outputLayoutRow = outputLayoutTable.FirstOrDefault();
		if (outputLayoutRow == null)
		{
			return rows;
		}

		var layoutId = Convert.ToString(outputLayoutRow[TAGMCSIds.OutputsLayoutsTable.Idx.LayoutID]);
		var layoutTable = GQIUtils.GetTable(dms, dataminerId, elementId, TAGMCSIds.AllLayoutChannelsTable.TablePid, new[] { $"fullFilter={TAGMCSIds.AllLayoutChannelsTable.Pid.LayoutID}=={layoutId}" });

		var outputAudioTable = GQIUtils.GetTable(dms, dataminerId, elementId, TAGMCSIds.OutputAudiosTable.TablePid, new[] { $"fullFilter={TAGMCSIds.OutputAudiosTable.Pid.Index}=={outputId}/1" });
		var outputAudioRow = outputAudioTable.FirstOrDefault();
		var currentChannelId = string.Empty;
		if (outputAudioRow != null)
		{
			currentChannelId = Convert.ToString(outputAudioRow[TAGMCSIds.OutputAudiosTable.Idx.ChannelID]);
		}

		foreach (var row in layoutTable)
		{
			var channelId = Convert.ToString(row[TAGMCSIds.AllLayoutChannelsTable.Idx.ChannelSourceId]);
			var channelName = Convert.ToString(row[TAGMCSIds.AllLayoutChannelsTable.Idx.ChannelTitle]);

			if (channelId == "0" || channelName == "None" || channelName == "Reserved")
			{
				continue;
			}

			rows.Add(new GQIRow(new[]
			{
				new GQICell { Value = channelId },
				new GQICell { Value = channelName },
				new GQICell { Value = channelId == currentChannelId },
			}));
		}

		return rows;
	}
}