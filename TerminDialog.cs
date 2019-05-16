using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

public class TerminDialog : ComponentDialog
{
	private const string InitialId = "mainDialog";
	private const string AnsprechpartnerPrompt = "textPrompt";
	private const string LocationPrompt = "choicePrompt";
	private const string ReservationDatePrompt = "dateTimePrompt";

	// Minimum and maximun length requirements for name
	private const int AnsprechpartnerLengthMinValue = 2;
	private const int AnsprechpartnerLengthMaxValue = 21;

	private const string AnsprechpartnerKey = "ansprechpartner";
	private const string LocationKey = "location";

	public TerminDialog(string id) : base(id)
	{
		InitialDialogId = InitialId;

		// Define the prompts used in this conversation flow.
		AddDialog(new TextPrompt("ansprechpartner", AnsprechpartnerValidateAsync));
		AddDialog(new ChoicePrompt("location"));
		AddDialog(new DateTimePrompt("reservationDate", DateValidatorAsync));


		// Define the conversation flow using a waterfall model.
		WaterfallStep[] waterfallSteps = new WaterfallStep[]
		{
		AnsprechpartnerStepAsync,
		LocationStepAsync,
		ReservationDateStepAsync,
		FinalStepAsync,
		};
		AddDialog(new WaterfallDialog(InitialId, waterfallSteps));
	}

	private static async Task<DialogTurnResult> AnsprechpartnerStepAsync(
		WaterfallStepContext step,
		CancellationToken cancellationToken = default(CancellationToken))
	{
		//string greeting = step.Options is WillkommenInfo guest
		//		&& !string.IsNullOrWhiteSpace(guest?.Name)
		//		? $"Hallo **{guest.Name}**" : "Hallo";

		//string prompt = $"{greeting}. Geben Sie Bitte den Namen Ihres Ansprechpartners ein.";
		string prompt = $"Geben Sie Bitte den Namen Ihres Ansprechpartners ein.";

		return await step.PromptAsync(
			"ansprechpartner",
			new PromptOptions { Prompt = MessageFactory.Text(prompt) },
			cancellationToken);
	}

	private static async Task<DialogTurnResult> LocationStepAsync(
		WaterfallStepContext step, 
		CancellationToken cancellationToken = default(CancellationToken))
	{
		// Save the Ansprechpartner name and prompt for the email.
		string ansprechpartner = step.Result as string;
		step.Values[AnsprechpartnerKey] = ansprechpartner;

		// Prompt for the location.
		return await step.PromptAsync(
			"location",
			new PromptOptions
			{
				Prompt = MessageFactory.Text("Bitte wählen Sie einen Standort aus (1 oder 2.)"),
				RetryPrompt = MessageFactory.Text("Bitte wählen Sie einen Standort aus der Liste aus."),
				Choices = ChoiceFactory.ToChoices(new List<string> { "Neugrabenweg 4, 66123 Saarbrücken", "Franz-Zang-Straße 2, 67059 Ludwigshafen am Rhein" }),
			},
			cancellationToken);
	}

	private static async Task<DialogTurnResult> ReservationDateStepAsync(
		WaterfallStepContext step,
		CancellationToken cancellationToken = default(CancellationToken))
	{
		var location = (step.Result as FoundChoice).Value;
		step.Values[LocationKey] = location;

		// Prompt for the reservation date.s
		return await step.PromptAsync(
		"reservationDate",
			new PromptOptions
			{
				Prompt = MessageFactory.Text("Geben Sie Bitte Ihr Terminsdatum ein.\n" +
											"\tBeispiel: 10:30 04-21-2019/Monday."),
				RetryPrompt = MessageFactory.Text("Zu welcher Zeit sollen wir den Termin vornehmen?"),
			},
			cancellationToken);
	}

	private static async Task<DialogTurnResult> FinalStepAsync(
		WaterfallStepContext step, 
		CancellationToken cancellationToken = default(CancellationToken))
	{
		//string greeting = step.Options is WillkommenInfo guest
		//		&& !string.IsNullOrWhiteSpace(guest?.Name)
		//		? $" **{guest.Name}**" : " ";
		// Retrieve the reservation date.
		var resolution = (step.Result as IList<DateTimeResolution>)?.FirstOrDefault();
		// Time ranges have a start and no value.
		var time = resolution.Value ?? resolution.Start;
		var ansprechpartner = (string)step.Values[AnsprechpartnerKey];
		var location = (string)step.Values[LocationKey];
		// Send a confirmation message.

		var reply = step.Context.Activity.CreateReply();
		var card = new ThumbnailCard();
		card.Title = $"BESTÄTIGUNG DES TERMINS\n\n";
		//card.Subtitle = $"\nName:  {greeting}\n\n" + $"Ansprechpartner: **{ansprechpartner}**\n\n"+ $"Uhrzeit: **{time}**\n\n" + $"Standort: **{location}**";
		card.Subtitle = $"Ansprechpartner: **{ansprechpartner}**\n\n" + $"Uhrzeit: **{time}**\n\n" + $"Standort: **{location}**";
		card.Images = new List<CardImage>() { new CardImage("https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRSqIr69WO_hzI2gwo7pQ1EywaCSPqk6CbfIf-zXd-51fdw_am48A") };
		card.Buttons = new List<CardAction> { new CardAction(ActionTypes.OpenUrl, "mehr erfahren", value: "https://www.allesprofis.de/bildung/prego-services-gmbh,166098")
		};
		reply.Attachments = new List<Attachment>() { card.ToAttachment() };
		await step.Context.SendActivityAsync(reply, cancellationToken: cancellationToken);


		// Return the collected information to the parent context.
		return await step.EndDialogAsync(
			new TerminInfo { Date = time,
							Ansprechpartner = ansprechpartner,
							Location = location
			},
			cancellationToken);
		
	}

	private static async Task<bool> DateValidatorAsync(
		PromptValidatorContext<IList<DateTimeResolution>> promptContext, 
		CancellationToken cancellationToken)
	{
		// Check whether the input could be recognized as an integer.
		if (!promptContext.Recognized.Succeeded)
		{
			await promptContext.Context.SendActivityAsync(
				"Es tut mir leid, ich verstehe nicht. Bitte geben Sie das Datum oder die Uhrzeit für Ihren Termin ein.",
				cancellationToken: cancellationToken);

			return false;
		}

		// Check whether any of the recognized date-times are appropriate,
		// and if so, return the first appropriate date-time.
		var earliest = DateTime.Now.AddHours(1.0);
		var value = promptContext.Recognized.Value.FirstOrDefault(v =>
			DateTime.TryParse(v.Value ?? v.Start, out var time) && DateTime.Compare(earliest, time) <= 0);

		if (value != null)
		{
			promptContext.Recognized.Value.Clear();
			promptContext.Recognized.Value.Add(value);
			return true;
		}

		await promptContext.Context.SendActivityAsync(
				"Es tut mir leid, wir können keinen Termin früher als in einer Stunde vornehmen.",
				cancellationToken: cancellationToken);

		return false;
	}

	private async Task<bool> AnsprechpartnerValidateAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
	{
		// Validate that the user entered a minimum length for their name.
		var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;
		if (value.Length > AnsprechpartnerLengthMinValue && value.Length < AnsprechpartnerLengthMaxValue)
		{
			var newValue = value[0].ToString().ToUpper() + value.Substring(1).ToLower();
			promptContext.Recognized.Value = newValue;
			return true;
		}
		else if (value.Length > AnsprechpartnerLengthMaxValue)
		{
			await promptContext.Context.SendActivityAsync($"Der Anprechpartnername ist zu lang. Geben Sie bitte höchstens `{AnsprechpartnerLengthMaxValue - 1}` Buchstaben.");
			return false;
		}
		else
			await promptContext.Context.SendActivityAsync($"Die Ansprechpartnername ist zu kurz. Geben Sie bitte mindestens `{AnsprechpartnerLengthMinValue + 1}` Buchtaben.");
		return false;
		
	}

}
