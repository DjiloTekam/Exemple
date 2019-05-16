using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Exemple
{
	public class WillkommenDialog : ComponentDialog
	{
		private const string InitialId = "mainDialog";
		private const string WillkommenKey = nameof(WillkommenDialog);
		private const string NamePrompt = "textPrompt";

		// Minimum and maximun length requirements for name
		private const int NameLengthMinValue = 2;
		private const int NameLengthMaxValue = 21;

		// You can start this from the parent using the ID assigned in the parent.

		public WillkommenDialog(string id) : base(id)
		{
			InitialDialogId = InitialId;

			// Define the prompts used in this conversation flow.
			
			AddDialog(new TextPrompt(NamePrompt, ValidateNameAsync));
			

			// Define the conversation flow using a waterfall model.
			WaterfallStep[] waterfallSteps = new WaterfallStep[]
			{
			
			NameStepAsync,
			FinalStepAsync,
			};
			AddDialog(new WaterfallDialog(InitialId, waterfallSteps));
		}

		private async Task<DialogTurnResult> NameStepAsync
			(WaterfallStepContext step,
			 CancellationToken cancellationToken = default(CancellationToken))
		{
			// Clear the guest information and prompt for the guest's name.
			step.Values[WillkommenKey] = new WillkommenInfo();
			return await step.PromptAsync(
				NamePrompt,
				new PromptOptions
				{
					Prompt = MessageFactory.Text("Geben Sie Bitte Ihren Namen ein."),
				},
				cancellationToken);
		}
		private async Task<DialogTurnResult> FinalStepAsync(
			WaterfallStepContext step, 
			CancellationToken cancellationToken = default(CancellationToken))
		{
			// Save the name and prompt for the room number.
			string name = step.Result as string;
			((WillkommenInfo)step.Values[WillkommenKey]).Name = name;

			var reply = step.Context.Activity.CreateReply();
			var card = new HeroCard();
			card.Title = $"Willkommen bei **{name}** bei pregobot services!\n \tWas kann ich dir helfen?";
			card.Subtitle = $"Erfahrung macht den Unterschied.";
			card.Images = new List<CardImage>() { new CardImage("https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTUQlysFMaenSmGF7eZo3MqlECTZIWbTqix9COGXnoDVXD4BOOq") };
			//card.Buttons = new List<CardAction> { new CardAction(ActionTypes.PostBack, title: "weiter", value: "frage stellen"),	
			//};
			reply.Attachments = new List<Attachment>() { card.ToAttachment() };
			await step.Context.SendActivityAsync(reply, cancellationToken: cancellationToken);
		
				// End the dialog, returning the guest info.
				return await step.EndDialogAsync(
				(WillkommenInfo)step.Values[WillkommenKey],
				cancellationToken);
		}


		private async Task<bool> ValidateNameAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
		{
			// Validate that the user entered a minimum length for their name.
			var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;
			if (value.Length > NameLengthMinValue && value.Length < NameLengthMaxValue)
			{
				var newValue = value[0].ToString().ToUpper() + value.Substring(1).ToLower();
				promptContext.Recognized.Value = newValue;
				return true;
			}
			else if (value.Length > NameLengthMaxValue)
			{
				await promptContext.Context.SendActivityAsync($"Der Name ist zu lang. Bitte geben Sie höchstens `{NameLengthMaxValue - 1}` Buchstaben.");
				return false;
			}
			else
				await promptContext.Context.SendActivityAsync($"Der Name ist zu kurz. Bitte geben Sie mindestens `{NameLengthMinValue + 1}` Buchstaben.");
			return false;
			
		}
	}
}
