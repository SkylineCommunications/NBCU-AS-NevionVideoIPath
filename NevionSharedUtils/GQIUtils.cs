namespace NevionSharedUtils
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using DomIds;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;

	public class GQIUtils
	{
		public static object[][] GetTable(GQIDMS dms, LiteElementInfoEvent response, int tableId, string[] tableFilter)
		{
			var partialTableRequest = new GetPartialTableMessage
			{
				DataMinerID = response.DataMinerID,
				ElementID = response.ElementID,
				ParameterID = tableId,
			};

			if (tableFilter.IsNullOrEmpty())
			{
				partialTableRequest.Filters = new[] { "forceFullTable=true" };
			}
			else
			{
				partialTableRequest.Filters = tableFilter;
			}

			var messageResponse = dms.SendMessage(partialTableRequest) as ParameterChangeEventMessage;
			if (messageResponse.NewValue.ArrayValue != null && messageResponse.NewValue.ArrayValue.Length > 0)
			{
				return BuildRows(messageResponse.NewValue.ArrayValue);
			}
			else
			{
				return new object[0][];
			}
		}

		public static object[][] GetTable(GQIDMS dms, int dataminerId, int elementId, int tableId, string[] tableFilter)
		{
			var partialTableRequest = new GetPartialTableMessage
			{
				DataMinerID = dataminerId,
				ElementID = elementId,
				ParameterID = tableId,
			};

			if (tableFilter.IsNullOrEmpty())
			{
				partialTableRequest.Filters = new[] { "forceFullTable=true" };
			}
			else
			{
				partialTableRequest.Filters = tableFilter;
			}

			var messageResponse = dms.SendMessage(partialTableRequest) as ParameterChangeEventMessage;
			if (messageResponse.NewValue.ArrayValue != null && messageResponse.NewValue.ArrayValue.Length > 0)
			{
				return BuildRows(messageResponse.NewValue.ArrayValue);
			}
			else
			{
				return new object[0][];
			}
		}

		private static object[][] BuildRows(ParameterValue[] columns)
		{
			int length1 = columns.Length;
			int length2 = 0;
			if (length1 > 0)
				length2 = columns[0].ArrayValue.Length;
			object[][] objArray;
			if (length1 > 0 && length2 > 0)
			{
				objArray = new object[length2][];
				for (int index = 0; index < length2; ++index)
					objArray[index] = new object[length1];
			}
			else
			{
				objArray = new object[0][];
			}

			for (int index1 = 0; index1 < length1; ++index1)
			{
				ParameterValue[] arrayValue = columns[index1].ArrayValue;
				for (int index2 = 0; index2 < length2; ++index2)
					objArray[index2][index1] = arrayValue[index2].IsEmpty ? (object)null : arrayValue[index2].ArrayValue[0].InteropValue;
			}

			return objArray;
		}

		public static List<NevionProfileDomValues> GetDOMPermissions(DomHelper domHelper, string tagType)
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

			return valuesList;
		}

		public static void GetUserDestinationPermissions(GQIDMS dms, DomHelper domHelper, out HashSet<string> matchingTagList, out HashSet<string> matchingDestinationList)
		{
			var permissionList = GetDOMPermissions(domHelper, "Destination");
			var responses = dms.SendMessages(new GetUserFullNameMessage(), new GetInfoMessage(InfoType.SecurityInfo));
			var systemUserName = responses?.OfType<GetUserFullNameResponseMessage>().FirstOrDefault()?.User.Trim();
			var matchingByUsername = permissionList.FirstOrDefault(instance => instance.Username == systemUserName);

			matchingTagList = new HashSet<string>();
			matchingDestinationList = new HashSet<string>();
			if (matchingByUsername != null)
			{
				matchingTagList = String.IsNullOrEmpty(matchingByUsername.Tags) ? new HashSet<string>() : matchingByUsername.Tags.Split(',').ToHashSet();
				matchingDestinationList = String.IsNullOrEmpty(matchingByUsername.Tags) ? new HashSet<string>() : matchingByUsername.Destinations.Split(',').ToHashSet();
			}

			// Group Data
			var securityResponse = responses?.OfType<GetUserInfoResponseMessage>().FirstOrDefault();

			var groupNames = securityResponse.FindGroupNamesByUserName(systemUserName).ToList();
			if (matchingByUsername == null && groupNames.Count > 0)
			{
				matchingTagList = MatchingValuesByGroup(permissionList, groupNames, x => x.Tags).ToHashSet();
				matchingDestinationList = MatchingValuesByGroup(permissionList, groupNames, x => x.Destinations).ToHashSet();
			}
		}

		public static string GetElementId(GQIDMS dms, string protocol)
		{
			var dataminerId = -1;
			var elementId = -1;

			var infoMessage = new GetInfoMessage { Type = InfoType.ElementInfo };
			var infoMessageResponses = dms.SendMessages(infoMessage);
			foreach (var response in infoMessageResponses)
			{
				var elementInfoEventMessage = (ElementInfoEventMessage)response;
				if (elementInfoEventMessage == null)
				{
					continue;
				}

				if (elementInfoEventMessage?.Protocol == protocol && elementInfoEventMessage?.ProtocolVersion == "Production" && elementInfoEventMessage?.State == ElementState.Active)
				{
					dataminerId = elementInfoEventMessage.DataMinerID;
					elementId = elementInfoEventMessage.ElementID;
					break;
				}
			}

			return $"{dataminerId}/{elementId}";
		}

		public static LiteElementInfoEvent GetElement(GQIDMS _dms, string elementId)
		{
			var sElementId = elementId.Split('/');
			var nevionElementRequest = new GetLiteElementInfo
			{
				DataMinerID = Convert.ToInt32(sElementId[0]),
				ElementID = Convert.ToInt32(sElementId[1]),
			};

			var nevionResponse = _dms.SendMessage(nevionElementRequest) as LiteElementInfoEvent;

			return nevionResponse;
		}

		public static List<string> MatchingValuesByGroup(List<NevionProfileDomValues> valuesList, List<string> groupNames, Func<NevionProfileDomValues, string> selector)
		{
			var result = new List<string>();

			foreach (var group in groupNames)
			{
				var matchingGroup = valuesList.FirstOrDefault(x => x.Group == group);
				if (matchingGroup != null)
				{
					var rawValue = selector(matchingGroup);
					if (!string.IsNullOrWhiteSpace(rawValue))
					{
						result.AddRange(rawValue.Split(','));
					}
				}
			}

			return result.Distinct().ToList();
		}

		public class NevionProfileDomValues
		{
			public string Username { get; set; }

			public string Group { get; set; }

			public string Tags { get; set; }

			public string Destinations { get; set; }
		}
	}
}