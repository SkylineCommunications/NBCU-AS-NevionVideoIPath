namespace NevionCommon_1
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using NevionSharedUtils;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS;
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS.InterApp.Messages;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Helper;

	public class TagUtils
	{
		public static string GetIdFromName(IDmsElement tagMcsElement, int tableId, string name)
		{
			var tagTable = tagMcsElement.GetTable(tableId);
			var tagDisplayKeys = tagTable.GetDisplayKeys();
			var matchingTagKey = tagDisplayKeys.FirstOrDefault(x => x.Equals(name));
			if (matchingTagKey.IsNullOrEmpty())
			{
				matchingTagKey = tagDisplayKeys.FirstOrDefault(x => x.Contains(name));
			}

			var matchingPrimaryKey = tagTable.GetPrimaryKey(matchingTagKey);
			return matchingPrimaryKey;
		}

		public static void ResetAudio(IEngine engine, TagMCS interappSender, string outputId)
		{
			var response = interappSender.SendMessage(new GetOutputConfigRequest(outputId, MessageIdentifier.ID), TimeSpan.FromMinutes(2));
			var outputResponse = response as GetOutputConfigResponse;
			if (outputResponse == null)
			{
				var interappResponse = response as InterAppResponse;
				engine.Log($"Get Output Message returned failure: {interappResponse.ResponseMessage}");
				return;
			}

			var output = outputResponse.Output;
			output.Processing.Audio[0].Mask = null;
			output.Input.Audio[0].Channel = null;
			output.Input.Audio[0].AudioPid = null;
			output.Input.Audio[0].AudioIndex = null;
			output.Processing.Muxing.Audio[0].Pid = null;

			var setResponse = interappSender.SendMessage(new SetOutputConfigRequest { Output = output }, TimeSpan.FromMinutes(2)) as InterAppResponse;
			if (setResponse == null)
			{
				engine.Log("No response on Set Output received");
				return;
			}

			if (!setResponse.Success)
			{
				engine.Log($"Set Output Message returned failure: {setResponse.ResponseMessage}");
			}
		}

		public static void ClearLayoutUmd(IEngine engine, IDmsElement idmsTag, TagMCS tagMcs, string layoutName)
		{
			try
			{
				string layoutId = GetIdFromName(idmsTag, TAGMCSIds.LayoutTable.TablePid, layoutName);
				var getLayoutRequest = new GetLayoutConfigRequest(layoutId, MessageIdentifier.ID);
				var layoutResponse = tagMcs.SendMessage(getLayoutRequest, TimeSpan.FromSeconds(30)) as GetLayoutConfigResponse;

				if (layoutResponse.Layout.Tiles[0].Umd == null)
				{
					layoutResponse.Layout.Tiles[0].Umd = new List<string> { String.Empty };
				}
				else
				{
					layoutResponse.Layout.Tiles[0].Umd[0] = String.Empty;
				}

				var setMessage = new SetLayoutConfigRequest
				{
					Layout = layoutResponse.Layout,
				};

				tagMcs.SendMessage(setMessage, TimeSpan.FromMinutes(2));
			}
			catch (Exception e)
			{
				engine.Log($"Script exception while updating the TAG layout. Please contact Skyline: {e}");
			}
		}
	}
}