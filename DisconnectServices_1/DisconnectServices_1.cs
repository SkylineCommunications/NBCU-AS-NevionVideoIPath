/*
****************************************************************************
*  Copyright (c) 2023,  Skyline Communications NV  All Rights Reserved.    *
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

dd/mm/2023	1.0.0.1		XXX, Skyline	Initial version
****************************************************************************
*/

namespace DisconnectServices_1
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS;
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS.InterApp.Messages;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Time;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	public class Script
	{
		private const int NevionCurrentServicesTable = 1500;
		private const int NevionCurrentServicesDestinationId = 1508;
		private const int NevionCurrentServicesCancelButton = 1515;
		private const int NevionCurrentServicesIndexIdx = 0;
		private const int NevionCurrentServicesDestinationNamesIdx = 3;
		private const int TagChannelTable = 2100;

		private static string[] primaryKeysCurrentServices = new string[0];
		private static Element nevionVideoIPathElement;
		private static Element tagElement;

		/// <summary>
		/// The script entry point.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		public static void Run(IEngine engine)
		{
			engine.SetFlag(RunTimeFlags.NoKeyCaching);

			try
			{
				var destinationIdsParameter = engine.GetScriptParam("DestinationIds").Value;
				if (!TryGetIdsFromInput(destinationIdsParameter, out List<string> destinationIds))
				{
					engine.ExitFail("Invalid destinations!");
					return;
				}

				nevionVideoIPathElement = engine.FindElementsByProtocol("Nevion Video iPath", "Production").FirstOrDefault();
				if (nevionVideoIPathElement == null)
				{
					engine.ExitFail("Nevion Video iPath element not found!");
					return;
				}

				primaryKeysCurrentServices = nevionVideoIPathElement.GetTablePrimaryKeys(NevionCurrentServicesTable); // Used to check if new connection entries has been added after the ConnectServices.

				var disconnectedServices = DisconnectDestinations(engine, destinationIds, out var destinationNames);
				VerifyDisconnectService(engine, disconnectedServices);
				DisconnectTAGConnections(engine, destinationNames);
				VerifyDisconnectChannel(engine, destinationNames);
			}
			catch (Exception e)
			{
				engine.Log($"Disconnect failed: {e}");
				engine.ExitFail("Disconnect failed due to unknown exception!");
			}
		}

		private static List<string> DisconnectDestinations(IEngine engine, List<string> destinationIds, out List<string> destinationNames)
		{
			var nevionVideoIPathElement = engine.FindElementsByProtocol("Nevion Video iPath", "Production").FirstOrDefault();
			destinationNames = new List<string>();
			if (nevionVideoIPathElement == null)
			{
				engine.ExitFail("Nevion Video iPath element not found!");
				return new List<string>();
			}

			if (!nevionVideoIPathElement.IsActive)
			{
				engine.ExitFail("Nevion Video iPath element not active!");
				return new List<string>();
			}

			var dms = engine.GetDms();

			var nevionVideoIPathDmsElement = dms.GetElement(nevionVideoIPathElement.ElementName);
			var currentServicesTable = nevionVideoIPathDmsElement.GetTable(NevionCurrentServicesTable);

			var servicesToCancel = new List<string>();
			foreach (var destinationId in destinationIds)
			{
				var rows = currentServicesTable.QueryData(new[] { new ColumnFilter { Pid = NevionCurrentServicesDestinationId, ComparisonOperator = ComparisonOperator.Equal, Value = destinationId } });
				if (rows.Any())
				{
					servicesToCancel.AddRange(rows.Select(r => Convert.ToString(r[NevionCurrentServicesIndexIdx])));
					destinationNames.AddRange(rows.Select(r => Convert.ToString(r[NevionCurrentServicesDestinationNamesIdx])));
				}
			}

			foreach (var serviceId in servicesToCancel)
			{
				nevionVideoIPathElement.SetParameterByPrimaryKey(NevionCurrentServicesCancelButton, serviceId, 1);
			}

			destinationNames = destinationNames.Distinct().ToList();
			return servicesToCancel;
		}

		private static void DisconnectTAGConnections(IEngine engine, List<string> destinationNames)
		{
			tagElement = engine.FindElement("TAG AWS MCS");
			if (tagElement == null)
			{
				engine.ExitFail("TAG Element not found");
				return;
			}

			if (!tagElement.IsActive)
			{
				engine.ExitFail("TAG Element not active");
				return;
			}

			var channelNames = tagElement.GetTableDisplayKeys(TagChannelTable);
			var channelsToDisconnect = channelNames.Where(c =>
			{
				var arrowIndex = c.IndexOf("->");
				if (arrowIndex == -1)
				{
					return false;
				}

				var destination = c.Substring(arrowIndex + 2);
				return destinationNames.Contains(destination);
			});

			var tagInterAppSender = new TagMCS(engine.GetUserConnection(), tagElement.DmaId, tagElement.ElementId);
			foreach (var channelName in channelsToDisconnect)
			{
				var response = tagInterAppSender.SendMessage(new GetChannelConfigRequest(channelName, MessageIdentifier.Name), TimeSpan.FromMinutes(2));
				var channelResponse = response as GetChannelConfigResponse;
				if (channelResponse == null)
				{
					var interappResponse = response as InterAppResponse;
					engine.Log($"Get Channel Message returned failure: {interappResponse.ResponseMessage}");
					continue;
				}

				var newChannelName = channelName.Substring(channelName.IndexOf("->") + 2);
				var channel = channelResponse.Channel;
				channel.Label = newChannelName;

				var setResponse = tagInterAppSender.SendMessage(new SetChannelConfigRequest { Channel = channel }, TimeSpan.FromMinutes(2)) as InterAppResponse;
				if (setResponse == null)
				{
					engine.Log("No response on Set Channel received");
					continue;
				}

				if (!setResponse.Success)
				{
					engine.Log($"Set Channel Message returned failure: {setResponse.ResponseMessage}");
				}
			}
		}

		private static void VerifyDisconnectChannel(IEngine engine, List<string> destinationNames)
		{
			int retries = 0;
			var destinationsFound = false;
			while (!destinationsFound && retries < 100)
			{
				engine.Sleep(60);

				var displayKeys = tagElement.GetTableDisplayKeys(TagChannelTable);
				destinationsFound = destinationNames.All(x => displayKeys.Contains(x));

				retries++;
			}
		}

		private static bool TryGetIdsFromInput(string input, out List<string> ids)
		{
			ids = new List<string>();

			try
			{
				ids = JsonConvert.DeserializeObject<List<string>>(input);

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private static void VerifyDisconnectService(IEngine engine, List<string> disconnectedServices)
		{
			int retries = 0;
			bool allEntriesFound = false;
			while (!allEntriesFound && retries < 100)
			{
				engine.Sleep(60);

				var allPrimaryKeys = nevionVideoIPathElement.GetTablePrimaryKeys(NevionCurrentServicesTable);

				allEntriesFound = allPrimaryKeys.Any(key => disconnectedServices.Contains(key));

				retries++;
			}
		}
	}
}