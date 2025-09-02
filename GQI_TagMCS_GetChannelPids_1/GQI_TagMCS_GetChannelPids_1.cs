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
using System.Text.RegularExpressions;
using NevionSharedUtils;
using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using Skyline.DataMiner.Net.Helper;

[GQIMetaData(Name = "TAG MCS Get Channel PIDs")]
public class GQI_TagMCS_GetChannelsByOutput : IGQIDataSource, IGQIOnInit, IGQIInputArguments
{
	private GQIStringArgument OutputIdArgument = new GQIStringArgument("Output ID") { IsRequired = false };
	private GQIStringArgument ChannelIdArgument = new GQIStringArgument("Channel ID") { IsRequired = false };
	private GQIBooleanArgument ChannelSelectedArgument = new GQIBooleanArgument("Channel Selected") { IsRequired = false };

	private string outputId;
	private string channelId;
	private bool channelSelected;
	private GQIDMS dms;
	private int dataminerId;
	private int elementId;

	public GQIArgument[] GetInputArguments()
	{
		return new GQIArgument[] { OutputIdArgument, ChannelIdArgument, ChannelSelectedArgument };
	}

	public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
	{
		outputId = args.GetArgumentValue(OutputIdArgument);
		channelId = args.GetArgumentValue(ChannelIdArgument);
		channelSelected = args.GetArgumentValue(ChannelSelectedArgument);
		return default;
	}

	public GQIColumn[] GetColumns()
	{
		return new GQIColumn[]
		{
			new GQIStringColumn("PID"),
			new GQIStringColumn("Index"),
			new GQIStringColumn("Content Type"),
			new GQIStringColumn("Language"),
			new GQIStringColumn("Audio Format"),
			new GQIBooleanColumn("Selected"),
		};
	}

	public OnInitOutputArgs OnInit(OnInitInputArgs args)
	{
		dms = args.DMS;
		var tagId = GQIUtils.GetElementId(dms, GQIUtils.TagElement).Split('/');
		dataminerId = Convert.ToInt32(tagId[0]);
		elementId = Convert.ToInt32(tagId[1]);
		return default;
	}

	public GQIPage GetNextPage(GetNextPageInputArgs args)
	{
		if (dataminerId == GQIUtils.NotFound || elementId == GQIUtils.NotFound || channelId.IsNullOrEmpty())
		{
			return new GQIPage(new GQIRow[0]);
		}

		return new GQIPage(BuildRows().ToArray());
	}

	private List<GQIRow> BuildRows()
	{
		var rows = new List<GQIRow>();

		var outputAudioTable = GQIUtils.GetTable(dms, dataminerId, elementId, TAGMCSIds.OutputAudiosTable.TablePid, new[] { $"fullFilter={TAGMCSIds.OutputAudiosTable.Pid.Index}=={outputId}/1" });
		var outputAudioRow = outputAudioTable.FirstOrDefault();
		var currentPid = string.Empty;
		if (outputAudioRow != null)
		{
			currentPid = Convert.ToString(outputAudioRow[TAGMCSIds.OutputAudiosTable.Idx.InputPID]);
		}

		var channelPidFilter = new[]
		{
			$"fullFilter={TAGMCSIds.ChannelPidsTable.Pid.ChannelId}=={channelId} AND " +
			$"({TAGMCSIds.ChannelPidsTable.Pid.Type}=={(int)TAGMCSIds.ChannelPidsTable.ChannelPidsType.Audio} OR " +
			$"{TAGMCSIds.ChannelPidsTable.Pid.Type}=={(int)TAGMCSIds.ChannelPidsTable.ChannelPidsType.AES67} OR " +
			$"{TAGMCSIds.ChannelPidsTable.Pid.Type}=={(int)TAGMCSIds.ChannelPidsTable.ChannelPidsType.AES3})",
		};
		var channelPidTable = GQIUtils.GetTable(dms, dataminerId, elementId, TAGMCSIds.ChannelPidsTable.TablePid, channelPidFilter);
		List<string> pids = new List<string>();
		foreach (var row in channelPidTable)
		{
			var language = Convert.ToString(row[TAGMCSIds.ChannelPidsTable.Idx.Language]);
			var pid = Convert.ToString(row[TAGMCSIds.ChannelPidsTable.Idx.PID]);
			var audioKey = Convert.ToString(row[TAGMCSIds.ChannelPidsTable.Idx.Index]);
			var match = Regex.Match(audioKey, @"(?:Audio|AES3|AES67)/(\d+)", RegexOptions.IgnoreCase);
			var audioId = match.Success ? match.Groups[1].Value : "N/A";

			var audioFormatCode = 0;
			if (row[TAGMCSIds.ChannelPidsTable.Idx.AudioChannels] != null)
			{
				audioFormatCode = Convert.ToInt32(row[TAGMCSIds.ChannelPidsTable.Idx.AudioChannels]);
			}

			string audioFormat;
			if (audioFormatCode == 6)
			{
				audioFormat = "Surround (5.1)";
			}
			else if (audioFormatCode == 2)
			{
				audioFormat = "Stereo (2.0)";
			}
			else
			{
				audioFormat = $"Don't Care";
			}

			var contentType = Convert.ToString(row[TAGMCSIds.ChannelPidsTable.Idx.Type]);

			rows.Add(new GQIRow(new[]
			{
				new GQICell{ Value = pid },
				new GQICell{ Value = audioId },
				new GQICell{ Value = GQIUtils.GetEnumDescription<TAGMCSIds.ChannelPidsTable.ChannelPidsType>(Convert.ToInt32(contentType)) },
				new GQICell{ Value = language },
				new GQICell{ Value = audioFormat },
				new GQICell{ Value = pid == currentPid && channelSelected },
			}));

			pids.Add(pid);
		}

		var channelStatusPidFilter = new[] {
			$"fullFilter={TAGMCSIds.ChannelStatusComponentsTable.Pid.ChannelID}=={channelId} AND " +
			$"({TAGMCSIds.ChannelStatusComponentsTable.Pid.ContentType}=={(int)TAGMCSIds.ChannelPidsTable.ChannelPidsType.Audio} OR " +
			$"{TAGMCSIds.ChannelStatusComponentsTable.Pid.ContentType}=={(int)TAGMCSIds.ChannelPidsTable.ChannelPidsType.AES67} OR " +
			$"{TAGMCSIds.ChannelStatusComponentsTable.Pid.ContentType}=={(int)TAGMCSIds.ChannelPidsTable.ChannelPidsType.AES3})",
		};
		var channelStatusPidTable = GQIUtils.GetTable(dms, dataminerId, elementId, TAGMCSIds.ChannelStatusComponentsTable.TablePid, channelStatusPidFilter);
		foreach (var row in channelStatusPidTable)
		{
			var pid = Convert.ToString(row[TAGMCSIds.ChannelStatusComponentsTable.Idx.PID]);
			var index = Convert.ToString(row[TAGMCSIds.ChannelStatusComponentsTable.Idx.Index]);
			var contentType = Convert.ToString(row[TAGMCSIds.ChannelStatusComponentsTable.Idx.ContentType]);

			if (pids.Contains(pid) || index == "-1")
			{
				continue;
			}

			rows.Add(new GQIRow(new[]
			{
				new GQICell { Value = pid },
				new GQICell { Value = index },
				new GQICell { Value = Enum.GetName(typeof(TAGMCSIds.ChannelPidsTable.ChannelPidsType), Convert.ToInt32(contentType)) },
				new GQICell { Value = "N/A" },
				new GQICell { Value = "N/A" },
				new GQICell { Value = pid == currentPid && channelSelected },
			}));
		}

		return rows;
	}
}