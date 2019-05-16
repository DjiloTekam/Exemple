using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Exemple
{
	public class ExempleBot : IBot
	{
		public const string WelcomeText = @"Ich beantworte Ihre Fragen, schlage Termine vor und berücksichtige Ihre täglichen Beschwerden. Tippen Sie Bitte etwas um zu beginnen";

		// Define the IDs for the dialogs in the bot's dialog set.
		private const string MainDialogId = "mainDialog";
		private const string WillkommenDialogId = "willkommenDialog";
		private const string BeschwerdeDialogId = "beschwerdeDialog";
		private const string FrageDialogId = "frageDialog";
		private const string TerminDialogId = "terminDialog";

		// Define the dialog set for the bot.
		private readonly DialogSet _dialogs;

		// Define the state accessors and the logger for the bot.
		private readonly BotAccessors _accessors;
		private readonly ILogger _logger;
		//private const string name = WillkommenInfo.Name;

		//public static string Name => name;

		/// <summary>
		/// Initializes a new instance of the <see cref="ExempleBot"/> class.
		/// </summary>
		/// <param name="accessors">Contains the objects to use to manage state.</param>
		/// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
		public ExempleBot(BotAccessors accessors, ILoggerFactory loggerFactory)
		{
			if (loggerFactory == null)
			{
				throw new System.ArgumentNullException(nameof(loggerFactory));
			}

			_logger = loggerFactory.CreateLogger<ExempleBot>();
			_logger.LogTrace($"{nameof(ExempleBot)} turn start.");
			_accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));

			// Define the steps of the main dialog.
			WaterfallStep[] steps = new WaterfallStep[]
			{
			MenuStepAsync,
			HandleChoiceAsync,
			LoopBackAsync,
			};

			// Create our bot's dialog set, adding a main dialog and the three component dialogs.
			_dialogs = new DialogSet(_accessors.DialogStateAccessor)
				.Add(new WaterfallDialog(MainDialogId, steps))
				.Add(new WillkommenDialog(WillkommenDialogId))
				.Add(new BeschwerdeDialog(BeschwerdeDialogId))
				.Add(new FrageDialog(FrageDialogId))
				.Add(new TerminDialog(TerminDialogId));
		}

		private static async Task<DialogTurnResult> MenuStepAsync(
			WaterfallStepContext stepContext,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			List<string> menu = new List<string> {  "Frage stellen", "Terminanfrage", "Beschwerde", "Beenden" };
			await stepContext.Context.SendActivityAsync(
			MessageFactory.SuggestedActions(menu, "Wie kann ich dir helfen?"),
			
			cancellationToken: cancellationToken);
			return Dialog.EndOfTurn;
		}

		private async Task<DialogTurnResult> HandleChoiceAsync(
			WaterfallStepContext stepContext, 
			CancellationToken cancellationToken = default(CancellationToken))
		{
			// Get the user's info. (Since the type factory is null, this will throw if state does not yet have a value for user info.)
			UserInfo userInfo = await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, null, cancellationToken);

			// Check the user's input and decide which dialog to start.
			// Pass in the guest info when starting either of the child dialogs.
			string choice = (stepContext.Result as string)?.Trim()?.ToLowerInvariant();
			//string greeting = step.Options is WillkommenInfo guest
			//	&& !string.IsNullOrWhiteSpace(guest?.Name)
			//	? $"Hallo **{guest.Name}**" : "Hallo";
			switch (choice)
			{
				case "frage stellen":
					return await stepContext.BeginDialogAsync(FrageDialogId, userInfo.Willkommen, cancellationToken);
				case "terminanfrage":
					return await stepContext.BeginDialogAsync(TerminDialogId, userInfo.Willkommen, cancellationToken);
				case "beschwerde":
					return await stepContext.BeginDialogAsync(BeschwerdeDialogId, userInfo.Willkommen, cancellationToken);
				case "schließen":
					await stepContext.Context.SendActivityAsync($"Danke sehr für Ihre Teilnahme\n" +
						$"Auf wiedersehen!");

					return await stepContext.EndDialogAsync(null, cancellationToken);

				default:
					// If we don't recognize the user's intent, start again from the beginning.
					await stepContext.Context.SendActivityAsync(
						"Tut mir leid, ich verstehe diesen Befehl nicht. Bitte wählen Sie eine Option aus der Liste aus.");
					return await stepContext.ReplaceDialogAsync(MainDialogId, null, cancellationToken);
			}
		}
		private  async Task<DialogTurnResult> LoopBackAsync(
			WaterfallStepContext stepContext, 
			CancellationToken cancellationToken = default(CancellationToken))
		{
			// Get the user's info. (Because the type factory is null, this will throw if state does not yet have a value for user info.)
			UserInfo userInfo = await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, null, cancellationToken);

			// Process the return value from the child dialog.
			
			switch (stepContext.Result)
			{
				case FrageInfo frage:
					// Store the results of the frage dialog.
					userInfo.Frage = frage;
					await _accessors.UserInfoAccessor.SetAsync(stepContext.Context, userInfo, cancellationToken);
					break;
				case TerminInfo termin:
					// Store the results of the termin dialog.
					userInfo.Termin = termin;
					await _accessors.UserInfoAccessor.SetAsync(stepContext.Context, userInfo, cancellationToken);
					break;
				case BeschwerdeInfo beschwerde:
					// Store the results of beschwerde dialog.
					userInfo.Beschwerde = beschwerde;
					await _accessors.UserInfoAccessor.SetAsync(stepContext.Context, userInfo, cancellationToken);
					break;
				default:
					// We shouldn't get here, since these are no other branches that get this far.
					break;
			}

			// Restart the main menu dialog.
			return await stepContext.ReplaceDialogAsync(FrageDialogId, null, cancellationToken);
		}
		public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (turnContext.Activity.Type == ActivityTypes.Message)
			{
				// Establish dialog state from the conversation state.
				DialogContext dc = await _dialogs.CreateContextAsync(turnContext, cancellationToken);

				// Get the user's info.
				UserInfo userInfo = await _accessors.UserInfoAccessor.GetAsync(turnContext, () => new UserInfo(), cancellationToken);

				// Continue any current dialog.
				DialogTurnResult dialogTurnResult = await dc.ContinueDialogAsync();

				// Process the result of any complete dialog.
				if (dialogTurnResult.Status is DialogTurnStatus.Complete)
				{
					switch (dialogTurnResult.Result)
					{
						//case WillkommenInfo guestInfo:
						//	// Store the results of the check-in dialog.
						//	userInfo.Willkommen = guestInfo;
						//	await _accessors.UserInfoAccessor.SetAsync(turnContext, userInfo, cancellationToken);
						//	break;
						//default:
						//	// We shouldn't get here, since the main dialog is designed to loop.
						//	break;
						case FrageDialogId:
							await dc.BeginDialogAsync(FrageDialogId, null, cancellationToken);
							break;


						case TerminDialogId:

							await dc.BeginDialogAsync(TerminDialogId, null, cancellationToken);
							break;
						case BeschwerdeDialogId:

							await dc.BeginDialogAsync(BeschwerdeDialogId, null, cancellationToken);
							break;

						default:

							break;
					}
				}

				// Every dialog step sends a response, so if no response was sent,
				// then no dialog is currently active.
				else if (!turnContext.Responded)
				{
					//if (string.IsNullOrEmpty(userInfo.Willkommen?.Name))
					////{
					//switch(dialogTurnResult.Result)
					//{
					//	case FrageDialogId:
					//		await dc.BeginDialogAsync(FrageDialogId, null, cancellationToken);
					//		break;


					//	case TerminDialogId:

					//		await dc.BeginDialogAsync(TerminDialogId, null, cancellationToken);
					//		break;
					//	case BeschwerdeDialogId:

					//		await dc.BeginDialogAsync(BeschwerdeDialogId, null, cancellationToken);
					//		break;

					//	default:
							
					//		break;

					
					
					//}
					await dc.BeginDialogAsync(FrageDialogId, null, cancellationToken);
				}

				// Save the new turn count into the conversation state.
				await _accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
				await _accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
			}
			else
			{
				//await turnContext.SendActivityAsync(WelcomeText, cancellationToken: cancellationToken);
				await SendWelcomeMessageAsync(turnContext, cancellationToken);
			}
		}

		private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
		{
			foreach (var member in turnContext.Activity.MembersAdded)
			{
				if (member.Id != turnContext.Activity.Recipient.Id)
				{
					var reply = turnContext.Activity.CreateReply();
					var card = new HeroCard();
					card.Title = $"Lieber Kunde Willkommen bei pregobot services!\n \tWas kann ich dir helfen?";
					card.Subtitle = $"Erfahrung macht den Unterschied.";
					card.Images = new List<CardImage>() { new CardImage("C:/Users/PR900162/Pictures/Saved Pictures/prego-pfalzwerke_insys_560x180.jpg") };
					card.Buttons = new List<CardAction> { //new CardAction(ActionTypes.PostBack, title: "weiter", value: "frage stellen"),
				new CardAction(ActionTypes.PostBack, title: "Frage stellen", value: "frage stellen"),
				//new CardAction(ActionTypes.PostBack, title: "Terminanfrage", value: "terminanfrage"),
				//new CardAction(ActionTypes.PostBack, title: "Beschwerde", value: "beschwerde"), 
			    };
					reply.Attachments = new List<Attachment>() { card.ToAttachment() };
					await turnContext.SendActivityAsync(reply, cancellationToken);	
				}
			}
		}
	}

}
