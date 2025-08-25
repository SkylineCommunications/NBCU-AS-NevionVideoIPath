namespace NevionCommon_1
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;

	using System.Threading;

	using DomIds;
	using NevionSharedUtils;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS;
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS.InterApp.Messages;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;
	using static NevionSharedUtils.GQIUtils;

	public class Utils
	{
		public static bool Retry(Func<bool> func, TimeSpan timeout)
		{
			bool success;

			Stopwatch sw = new Stopwatch();
			sw.Start();

			do
			{
				success = func();
				if (!success)
				{
					Thread.Sleep(5000);
				}
			}
			while (!success && sw.Elapsed <= timeout);

			return success;
		}

		public static string GetOneDeserializedValue(string scriptParam)
		{
			if (scriptParam.Count() <= 2)
			{
				return scriptParam;
			}

			if (scriptParam.Contains("[") && scriptParam.Contains("]"))
			{
				return SecureNewtonsoftDeserialization.DeserializeObject<List<string>>(scriptParam)[0];
			}
			else
			{
				return scriptParam;
			}
		}

		public static string RemoveBracketPrefix(string input)
		{
			if (String.IsNullOrWhiteSpace(input))
			{
				return input;
			}

			int closingBracketIndex = input.IndexOf(']');
			if (input.StartsWith("[") && closingBracketIndex != -1)
			{
				return input.Substring(closingBracketIndex + 1).TrimStart();
			}

			return input;
		}

		public static NevionProfileDomValues GetDOMPermissionsByUser(DomHelper domHelper, string tagType, IEngine engine)
		{
			var instances = domHelper.DomInstances.Read(DomInstanceExposers.DomDefinitionId.Equal(Lca_Access.Definitions.Nevion_Control.Id));

			var tagsFieldDescriptor = tagType == "Source"
				? Lca_Access.Sections.NevionControl.SourceTags
				: Lca_Access.Sections.NevionControl.DestinationTags;

			var valuesList = new List<NevionProfileDomValues>();
			foreach (var instance in instances)
			{
				var username = instance.GetFieldValue<string>(Lca_Access.Sections.BasicInformation.Id, Lca_Access.Sections.BasicInformation.Username)?.Value;
				var group = instance.GetFieldValue<string>(Lca_Access.Sections.BasicInformation.Id, Lca_Access.Sections.BasicInformation.Group)?.Value;
				var tags = instance.GetFieldValue<string>(Lca_Access.Sections.NevionControl.Id, tagsFieldDescriptor)?.Value;
				var destinations = instance.GetFieldValue<string>(Lca_Access.Sections.NevionControl.Id, Lca_Access.Sections.NevionControl.DestinationNames)?.Value;

				valuesList.Add(new NevionProfileDomValues { Username = username, Group = group, Tags = tags, Destinations = destinations });
			}

			var systemUserName = engine.UserLoginName;
			var userInfo = engine.SendSLNetMessage(new GetInfoMessage(InfoType.SecurityInfo)).FirstOrDefault() as GetUserInfoResponseMessage;
			var userGroups = userInfo.FindGroupNamesByUserName(systemUserName);
			var matchingByUsername = valuesList.FirstOrDefault(instance => instance.Username == systemUserName);
			if (matchingByUsername == null)
			{
				var tags = string.Empty;
				var destinations = string.Empty;
				foreach (var group in userGroups)
				{
					var groupPermissions = valuesList.FirstOrDefault(instance => instance.Group == group);
					if (groupPermissions != null)
					{
						tags = $"{tags},{groupPermissions.Tags}";
						destinations = $"{destinations},{groupPermissions.Destinations}";
					}
				}

				tags = tags.Contains("ALL") ? "ALL" : String.Join(",", tags.Split(',').Distinct());
				destinations = destinations.Contains("ALL") ? "ALL" : String.Join(",", destinations.Split(',').Distinct());

				matchingByUsername = new NevionProfileDomValues { Tags = tags, Destinations = destinations };
			}

			return matchingByUsername;
		}

		public static string GetIdFromName(IDmsElement tagMcsElement, int tableId, string name)
		{
			var layoutTable = tagMcsElement.GetTable(tableId);
			var layoutKeys = layoutTable.GetDisplayKeys();
			var matchingLayout = layoutKeys.FirstOrDefault(x => x.Equals(name));
			if (matchingLayout.IsNullOrEmpty())
			{
				matchingLayout = layoutKeys.FirstOrDefault(x => x.Contains(name));
			}

			var layoutId = layoutTable.GetPrimaryKey(matchingLayout);
			return layoutId;
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
	}
}