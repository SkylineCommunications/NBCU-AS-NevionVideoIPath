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

		public LoggingHelper(IEngine engine)
		{
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
		}

		public void GenerateInformation(string message)
		{
			string userMessage = GetUserMessage();
			engine.GenerateInformation($"Action Requested: {message} ({userMessage})");
		}

		private string GetUserMessage()
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

			return userMessage;
		}
	}
}
