namespace NevionCommon_1
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;

	public class LoggingHelper
	{
		private readonly IEngine engine;
		private readonly ScriptType scriptType;

		public LoggingHelper(IEngine engine, ScriptType scriptType)
		{
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.scriptType = scriptType;
		}

		public enum ScriptType
		{
			[Description("Schedule Service")]
			ScheduleService,
			[Description("Disconnection")]
			Disconnection,
		}

		public void GenerateInformation(string message)
		{
			var responses = engine.SendSLNetMessages(new DMSMessage[] { new GetUserFullNameMessage(), new GetInfoMessage(InfoType.SecurityInfo) });
			var tfaydUser = responses?.OfType<GetUserFullNameResponseMessage>().FirstOrDefault()?.User.Trim();

			string fullUsername;

			var userMessage = "by Unknown User";
			if (!String.IsNullOrEmpty(tfaydUser))
			{
				var securityResponse = responses?.OfType<GetUserInfoResponseMessage>().FirstOrDefault();
				fullUsername = securityResponse?.Users?.FirstOrDefault(u => u.Name == tfaydUser)?.FullName;
				userMessage = $"by {fullUsername} ({tfaydUser})";
			}

			var endTime = DateTime.Now.AddHours(4);

			var scriptTypeDescription = Utils.GetEnumDescription<ScriptType>((int)scriptType);

			engine.GenerateInformation($"[{scriptTypeDescription}] Action Requested: {message}, End at: {endTime} ({userMessage})");
		}
	}
}
