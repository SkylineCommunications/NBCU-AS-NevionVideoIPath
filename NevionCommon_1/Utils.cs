namespace NevionCommon_1
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
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

		public static string GetEnumDescription<T>(int? value) where T : struct, Enum
		{
			if (value == null)
			{
				return null;
			}

			if (Enum.IsDefined(typeof(T), value.Value))
			{
				var enumValue = (T)(object)value.Value;
				return GetEnumDescription((Enum)(object)enumValue);
			}

			return null;
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

		private static string GetEnumDescription(Enum value)
		{
			var field = value.GetType().GetField(value.ToString());
			var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
			return attribute == null ? value.ToString() : attribute.Description;
		}
	}
}