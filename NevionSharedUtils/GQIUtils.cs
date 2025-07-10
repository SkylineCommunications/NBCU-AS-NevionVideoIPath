namespace NevionSharedUtils
{
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages;

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
	}
}
