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
		nevionId = GQIUtils.GetElementId(dms, GQIUtils.NevionElement).Split('/');
		tagId = GQIUtils.GetElementId(dms, GQIUtils.TagElement).Split('/');
		return default;
	}

	public GQIColumn[] GetColumns()
	{
		return new GQIColumn[]
		{
			new GQIStringColumn("Output ID"),
			new GQIStringColumn("Output Name"),
		};
	}

	public GQIPage GetNextPage(GetNextPageInputArgs args)
	{
		GQIUtils.GetUserDestinationPermissions(dms, domHelper, out var matchingTagList, out var matchingDestinationList);

		if (nevionId[0] == GQIUtils.NotFound.ToString() || nevionId[1] == GQIUtils.NotFound.ToString() || tagId[0] == GQIUtils.NotFound.ToString() || tagId[0] == GQIUtils.NotFound.ToString())
		{
			return new GQIPage(new GQIRow[0]);
		}

		if (!matchingTagList.Any() || !matchingDestinationList.Any())
		{
			return new GQIPage(new GQIRow[0]);
		}

		return new GQIPage(BuildRows(matchingTagList, matchingDestinationList).ToArray());
	}

	private static bool CheckOutput(string destinationLabel, IEnumerable<string> destinationList)
	{
		if (!destinationList.Any(label => label == destinationLabel || (label.ToUpper() == "ALL" && destinationLabel.Contains("Routable"))))
		{
			return true;
		}

		return false;
	}

	private List<GQIRow> BuildRows(HashSet<string> tagList, HashSet<string> destinationList)
	{
		var rows = new List<GQIRow>();

		var outputsPermitted = destinationList.Select(x => GQIUtils.RemoveBracketPrefix(x).Trim()).Distinct();

		var outputConfigTableFilter = new[]
		{
			$"columns={TAGMCSIds.OutputConfigTable.Pid.Label}",
		};
		var outputConfigTable = GQIUtils.GetTable(dms, Convert.ToInt32(tagId[0]), Convert.ToInt32(tagId[1]), TAGMCSIds.OutputConfigTable.TablePid, outputConfigTableFilter);

		foreach (var row in outputConfigTable)
		{
			var outputLabel = Convert.ToString(row[TAGMCSIds.OutputConfigTable.Idx.Label]);
			if (CheckOutput(outputLabel, outputsPermitted))
			{
				continue;
			}

			var outputId = Convert.ToString(row[TAGMCSIds.OutputConfigTable.Idx.Index]);

			var gqiRow = rows.FirstOrDefault(r => Convert.ToString(r.Cells[0].Value) == outputId);
			if (gqiRow == null)
			{
				rows.Add(new GQIRow(new[]
				{
					new GQICell { Value = outputId },
					new GQICell { Value = outputLabel },
				}));
			}
		}

		if (outputFilter.IsNotNullOrEmpty())
		{
			return rows.OrderByDescending(r => Convert.ToString(r.Cells[1].Value).ToLower().Contains(outputFilter.ToLower())).ThenBy(r => Convert.ToString(r.Cells[1].Value)).ToList();
		}
		else
		{
			return rows.OrderBy(r => Convert.ToString(r.Cells[1].Value)).ToList();
		}
	}
}