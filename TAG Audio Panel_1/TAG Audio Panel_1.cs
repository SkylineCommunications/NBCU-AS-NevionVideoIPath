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

namespace TAG_Audio_Panel_1
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;
	using NevionCommon_1;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS;
	using Skyline.DataMiner.ConnectorAPI.TAGVideoSystems.MCS.InterApp.Messages;
	using Skyline.DataMiner.Net.Helper;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	public class Script
	{
		/// <summary>
		/// The script entry point.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		public void Run(IEngine engine)
		{
			try
			{
				// engine.ShowUI();
				RunSafe(engine);
			}
			catch (ScriptAbortException)
			{
				// Catch normal abort exceptions (engine.ExitFail or engine.ExitSuccess)
				throw; // Comment if it should be treated as a normal exit of the script.
			}
			catch (ScriptForceAbortException)
			{
				// Catch forced abort exceptions, caused via external maintenance messages.
				throw;
			}
			catch (ScriptTimeoutException)
			{
				// Catch timeout exceptions for when a script has been running for too long.
				throw;
			}
			catch (InteractiveUserDetachedException)
			{
				// Catch a user detaching from the interactive script by closing the window.
				// Only applicable for interactive scripts, can be removed for non-interactive scripts.
				throw;
			}
			catch (Exception e)
			{
				engine.ExitFail("Run|Something went wrong: " + e);
			}
		}

		private void RunSafe(IEngine engine)
		{
			var outputId = Utils.GetOneDeserializedValue(engine.GetScriptParam("Output ID").Value);
			var channelId = Utils.GetOneDeserializedValue(engine.GetScriptParam("Channel ID").Value);
			var pid = Utils.GetOneDeserializedValue(engine.GetScriptParam("PID").Value);
			var index = Utils.GetOneDeserializedValue(engine.GetScriptParam("Index").Value);
			var audioChannel = Utils.GetOneDeserializedValue(engine.GetScriptParam("Audio Channel").Value);

			if (outputId.IsNullOrEmpty())
			{
				ErrorMessageDialog.ShowMessage(engine, $"Output ID cannot be null or empty");
				engine.ExitFail($"Output ID cannot be null or empty");
				return;
			}

			var tagElement = engine.FindElement("TAG AWS MCS");

			var tag = new TagMCS(engine.GetUserConnection(), tagElement.DmaId, tagElement.ElementId);
			var response = tag.SendMessage(new GetOutputConfigRequest(outputId, MessageIdentifier.ID), TimeSpan.FromMinutes(2));
			var outputResponse = response as GetOutputConfigResponse;
			if (outputResponse == null)
			{
				var interappResponse = response as InterAppResponse;
				ErrorMessageDialog.ShowMessage(engine, $"Error getting Output Config: {interappResponse.ResponseMessage}");
				engine.ExitFail($"Error getting Output Config: {interappResponse.ResponseMessage}");
				return;
			}

			var outputConfig = outputResponse.Output;
			outputConfig.Processing.Audio[0].Mask = audioChannel;
			outputConfig.Input.Audio[0].AudioIndex = index;
			outputConfig.Input.Audio[0].AudioPid = pid;
			outputConfig.Input.Audio[0].Channel = channelId;
			outputConfig.Processing.Muxing.Audio[0].Pid = "202";

			var setRequest = new SetOutputConfigRequest { Output = outputConfig };
			var setResponse = tag.SendMessage(setRequest, TimeSpan.FromMinutes(2)) as InterAppResponse;
			if (setResponse.Success)
			{
				InformationMessageDialog.ShowMessage(engine, "Audio successfully set.");
			}
			else
			{
				ErrorMessageDialog.ShowMessage(engine, $"Error occured during updating the output: {setResponse.ResponseMessage}");
				engine.ExitFail($"Error occured during updating the output: {setResponse.ResponseMessage}");
				return;
			}

			engine.ExitSuccess("Output successfully updated");
		}
	}
}