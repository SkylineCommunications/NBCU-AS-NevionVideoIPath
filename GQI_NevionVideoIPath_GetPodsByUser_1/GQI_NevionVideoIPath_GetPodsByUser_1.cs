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

	public OnInitOutputArgs OnInit(OnInitInputArgs args)
	{
		_dms = args.DMS;
		domHelper = new DomHelper(_dms.SendMessages, DomIds.Lca_Access.ModuleId);
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
	}
}