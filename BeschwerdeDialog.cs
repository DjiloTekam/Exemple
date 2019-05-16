using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;

public class BeschwerdeDialog : ComponentDialog
{
	private const string InitialId = "mainDialog";
	//private const string NamePrompt = "textPrompt";
	private const string ConfirmPrompt = "confirmPrompt";
	private const string EmailPrompt = "textPrompt";
	private const string AnklagePrompt = "textPrompt";


	// Minimum an maximum length requirements for anklage
	private const int AnklageLengthMinValue = 15;
	private const int AnklageLengthMaxValue = 50;

	private const string EmailKey = "email";

	// You can start this from the parent using the ID assigned in the parent.

	public BeschwerdeDialog(string id) : base(id)
	{
		InitialDialogId = InitialId;

		// Define the prompts used in this conversation flow.
		AddDialog(new ConfirmPrompt("confirm", defaultLocale: Culture.German));
		AddDialog(new TextPrompt(EmailPrompt, EmailPromptValidatorAsync));
		AddDialog(new TextPrompt("anklage", ValidateAnklageAsync));


		// Define the conversation flow using a waterfall model.
		WaterfallStep[] waterfallSteps = new WaterfallStep[]
		{
			ConfirmStepAsync,
			EmailStepAsync,
			EmailConfirmStepAsync,
			FinalStepAsync,
		};
		AddDialog(new WaterfallDialog(InitialId, waterfallSteps));
	}

	private static async Task<DialogTurnResult> ConfirmStepAsync
		(WaterfallStepContext step,
		CancellationToken cancellationToken = default(CancellationToken))
	{
		string greeting = step.Options is WillkommenInfo guest
				&& !string.IsNullOrWhiteSpace(guest?.Name)
				? $"Hallo **{guest.Name}**" : "Hallo";

		string prompt = $"{greeting}. Möchten Sie eine Beschwerde schreiben?";
		return await step.PromptAsync(
			"confirm",
			new PromptOptions { Prompt = MessageFactory.Text(prompt) },
			cancellationToken);
	}



	private static async Task<DialogTurnResult> EmailStepAsync(
		WaterfallStepContext step,
		CancellationToken cancellationToken = default(CancellationToken))
	{

		if ((bool)step.Result)
		{
			 return await step.PromptAsync(
				 EmailPrompt,
				 new PromptOptions
				 {
					 Prompt = MessageFactory.Text("Geben Sie Bitte Ihre E-Mail-Adresse ein.\n" +
	  "Beispiel: abc@uv.xz")
				 }, 
				 cancellationToken);
			
		}
		else
		{
			await step.Context.SendActivityAsync(MessageFactory.Text("Ich danke dir. Auf Wiedersehen."),
				cancellationToken);
			return await step.EndDialogAsync(cancellationToken: cancellationToken);

		}

		
	}

	private static async Task<DialogTurnResult> EmailConfirmStepAsync(
		WaterfallStepContext step,
		CancellationToken cancellationToken = default(CancellationToken))
	{
		// Save the name and prompt for the email.

		string email = step.Result as string;
		step.Values[EmailKey] = email;
		//((BeschwerdeInfo)step.Values[BeschwerdeKey]).Email = email;
		return await step.PromptAsync(
			"anklage",
			new PromptOptions
			{
				Prompt = MessageFactory.Text($"Geben Sie Bitte Ihre Beschwerde ein."),
			},
			cancellationToken);

	}

	private static async Task<DialogTurnResult> FinalStepAsync(
		WaterfallStepContext step,
		CancellationToken cancellationToken = default(CancellationToken))
	{
		string greeting = step.Options is WillkommenInfo guest
				&& !string.IsNullOrWhiteSpace(guest?.Name)
				? $" **{guest.Name}**" : " ";

		// Save the Beschwerde and "sign off".
		string anklage = step.Result as string;
		var email = (string)step.Values[EmailKey];
		//((BeschwerdeInfo)step.Values[BeschwerdeKey]).Anklage = anklage;

		var reply = step.Context.Activity.CreateReply();
		var card = new HeroCard();
		card.Title = $"BESTÄTIGUNG DER BESCHWERDE\n\n";
		card.Subtitle = $"\nName:  {greeting}\n\n E-Mail-Adresse: **{email}**\n\n Beschwerdeinhalt: **{anklage}**\n\n";
		card.Images = new List<CardImage>() { new CardImage("https://schoenbloed.files.wordpress.com/2016/12/155_beschweren.jpg") };
		card.Buttons = new List<CardAction> { //new CardAction(ActionTypes.PostBack, title: "Frage stellen", value: "frage stellen"),
												 // new CardAction(ActionTypes.PostBack, title: "Terminanfrage", value: "terminanfrage"),
												  //new CardAction(ActionTypes.PostBack, title: "Beschwerde", value: "beschwerde"),
												 // new CardAction(ActionTypes.PostBack, title: "Beenden", value: "beenden")
												  new CardAction(ActionTypes.OpenUrl, "mehr erfahren", value: "https://www.prego-services.de/kontaktformular?mainKat=feedback")
		};
		reply.Attachments = new List<Attachment>() { card.ToAttachment() };
		await step.Context.SendActivityAsync(reply, cancellationToken: cancellationToken);
		//await step.Context.SendActivityAsync(
		//	$"Danke für Ihre Beschwerde: **{anklage}**\n " +
		//	$"Wir werden sie berücksichtigen.",
		//	cancellationToken: cancellationToken);

		// End the dialog, returning the guest info.
		return await step.EndDialogAsync(
			//(BeschwerdeInfo)step.Values[BeschwerdeKey],
			new BeschwerdeInfo { },
			cancellationToken);
	}

	private Task<bool> EmailPromptValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
	{
		var result = promptContext.Recognized.Value;

		// This condition is our validation rule.
		if (IsValidEmail(result))
		{
			// Success is indicated by passing back the value the Prompt has collected. You must pass back a value even if you haven't changed it.
			return Task.FromResult(true);
		}

		// Not calling End indicates validation failure. This will trigger a RetryPrompt if one has been defined.
		return Task.FromResult(false);
	}

	private bool IsValidEmail(string source)
	{
		return new EmailAddressAttribute().IsValid(source);
	}

	private async Task<bool> ValidateAnklageAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
	{
		// Validate that the user entered a minimum length for their name.
		var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;
		if (value.Length > AnklageLengthMinValue && value.Length < AnklageLengthMaxValue)
		{
			var newValue = value[0].ToString().ToUpper() + value.Substring(1).ToLower();
			promptContext.Recognized.Value = newValue;
			return true;
		}
		else
		{
			await promptContext.Context.SendActivityAsync($"Die Beschwerde besteht aus Zeichen zwischen `{AnklageLengthMinValue}` und `{AnklageLengthMaxValue}`.");
			return false;
		}
	}
}
