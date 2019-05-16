using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Exemple;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;

public class FrageDialog : ComponentDialog
{
	private const string InitialId = "mainDialog";
	//private const string OrtPrompt = "ort";
	//private const string PostcodePrompt = "postcode";
	private const string QuestionPrompt = "question";


	//private const string OrtKey = "ort";
	//private const string PostcodeKey = "postcode";
	// Minimum and maximun length requirements for question
	//private const int OrtLengthMinValue = 3;
	//private const int OrtLengthMaxValue = 21;
	//private const int PostcodeLengthValue = 5;
	private const int QuestionLengthMinValue = 1;
	private const int QuestionLengthMaxValue = 801;

	JToken besteFrage;
	JObject data = JObject.Parse(File.ReadAllText("frage.json"));
	public string question;
	int nbre;

	public FrageDialog(string id) : base(id)
	{
		InitialDialogId = InitialId;
		//	AddDialog(new TextPrompt("ort", ValidateOrtAsync));
		//  AddDialog(new TextPrompt("postcode", ValidatePostcodeAsync));
		AddDialog(new TextPrompt("question", QuestionValidateAsync));

		// Define the conversation flow using a waterfall model.
		WaterfallStep[] waterfallSteps = new WaterfallStep[]
		{
					//OrtStepAsync,
					//PostcodeStepAsync,
					QuestionStepAsync,
					FinalStepAsync,


		};

		AddDialog(new WaterfallDialog(InitialId, waterfallSteps));
		
	}

	//private async Task<DialogTurnResult> OrtStepAsync
	//	(WaterfallStepContext step,
	//	CancellationToken cancellationToken = default(CancellationToken))
	//{
	//	string greeting = step.Options is WillkommenInfo guest
	//		&& !string.IsNullOrWhiteSpace(guest?.Name)
	//		? $"Hallo **{guest.Name}**" : "Hallo";

	//	//string prompt = $"{greeting}, In welcher Stadt leben Sie ? ";
	//	string prompt = "In welcher Stadt leben Sie ? ";

	//	return await step.PromptAsync(
	//		"ort",
	//		new PromptOptions
	//		{ Prompt = MessageFactory.Text(prompt) },
	//		cancellationToken);
	//}

	//private async Task<DialogTurnResult> PostcodeStepAsync(
	//	WaterfallStepContext step,
	//	CancellationToken cancellationToken = default(CancellationToken))
	//{
	//	string greeting = step.Options is WillkommenInfo guest
	//		&& !string.IsNullOrWhiteSpace(guest?.Name)
	//		? $"Gut **{guest.Name}**" : "Gut";
	//	// Save the Ansprechpartner name and prompt for the email.
	//	string ort = step.Result as string;
	//	step.Values[OrtKey] = ort;
	//	//string prompt = $"{greeting} von **{ort}** was ist Ihre Postleitzahl?";
	//	string prompt = $"was ist Ihre Postleitzahl?";
	//	// Prompt for the location.
	//	return await step.PromptAsync(
	//		"postcode",
	//		new PromptOptions
	//		{ Prompt = MessageFactory.Text(prompt) },
	//		cancellationToken);
	//}

	private static async Task<DialogTurnResult> QuestionStepAsync(
		WaterfallStepContext step,
		CancellationToken cancellationToken = default(CancellationToken))
	{
		//string greeting = step.Options is WillkommenInfo guest
		//	&& !string.IsNullOrWhiteSpace(guest?.Name)
		//	? $"Tool **{guest.Name}**" : "Tool";
		//string postcode = step.Result as string;
		//step.Values[PostcodeKey] = postcode;
		//var ort = (string)step.Values[OrtKey];
		//var code = (string)step.Values[PostcodeKey];

		//string prompt = $"{greeting} von Verbrauchstelle **{code}** **{ort}** was sind Ihre Fragen?";
		//string prompt = $"Kunden von **{code}** **{ort}** was sind Ihre Fragen?";
		string prompt = $"Was ist Ihre Frage?";
		return await step.PromptAsync(
			QuestionPrompt,
			new PromptOptions
			{
				Prompt = MessageFactory.Text(prompt),
				RetryPrompt = MessageFactory.Text("Geben Sie Bitte Ihre Frage ein."),
			},
			cancellationToken);
		//string prompt = "Wie kann ich dir helfen?";
		//List<string> menu = new List<string> { "Schließen" };
		//await step.Context.SendActivityAsync(

		//		MessageFactory.SuggestedActions(menu, "Geben Sie Bitte Ihre Frage ein?"),
		//		cancellationToken: cancellationToken);
		//return Dialog.EndOfTurn;
		
	}
	
			

	private async Task<DialogTurnResult> FinalStepAsync(
		WaterfallStepContext step,
		CancellationToken cancellationToken = default(CancellationToken))
	{
		
		question = (step.Result as string)?.Trim()?.ToLowerInvariant();

		if (question.Equals("schließen", StringComparison.InvariantCultureIgnoreCase) || question.Equals("beenden", StringComparison.InvariantCultureIgnoreCase))
		{
			await step.Context.SendActivityAsync($"Vielen Dank für die Fragen. Bei uns ist der Kunde König und die Zufriedenheit bleibt unsere Priorität");

			return Dialog.EndOfTurn;
			
		}
		else if (question.Equals("hilfe", StringComparison.InvariantCultureIgnoreCase) || question.Equals("help", StringComparison.InvariantCultureIgnoreCase) || question.Equals("mehr Info", StringComparison.InvariantCultureIgnoreCase))
		{

			string message = $"Geben Sie Ihre Fragen über Zähler- Rechnung und Vertrag von **Strom-Wasser-Gas** ein und bekommen Sie Ihre Antworten in Echtzeit.\nUm die Komponente zu verlassen, Geben Sie **schließen** oder **beenden**.";
			await step.Context.SendActivityAsync(
				message,
				cancellationToken: cancellationToken);
			return await step.ReplaceDialogAsync(InitialId); 
		}
		else
		{
			bool found = false;
			//int maxcnt = 0;
			//var counter = 0;
			bool first = false;

			// Annulle les points d'interrogation, exclammation, virgule et autre a la fin de la phrase 
			//question = question.Replace("?", "").Replace(".", "").Replace(";", "").Replace("!", "").Replace(",", "").Replace(":", "");
			var testArray = question.Split(" ").ToList();


			int maxcnt = 0;
			
			
				foreach (var frage in data.SelectToken("frage"))
				{
					int cnt = 0;
					nbre++;
				foreach (var frageStichwort in frage.SelectToken("stichwort"))
				{
					if (question.Contains(frageStichwort.ToString().ToLower()) && testArray.Count != 1)
					{
						cnt++;
					}
					else if (testArray.Count == 1 && question.Equals(frageStichwort.ToString().ToLower()))
					{
						found = true;
						var reply = step.Context.Activity.CreateReply();
						
							var card = new HeroCard
							{
								Buttons = new List<CardAction>() {	 
								new CardAction(ActionTypes.ImBack, title: frage.SelectToken("frage").ToString(), value: frage.SelectToken("frage").ToString())
								},
						
                       };
						reply.Attachments = new List<Attachment>() { card.ToAttachment() };
						if (!first)
						{
							await step.Context.SendActivityAsync($"Ich habe die folgenden Vorschläge in Bezug auf Ihr Wort **{question}**.");
							first = true;
						} 
						await step.Context.SendActivityAsync(reply);
					}
				}

					if (cnt > maxcnt)
					{
						maxcnt = cnt;
						besteFrage = frage;
					}
					else if (nbre == data.SelectToken("frage").Count())
					{

						break;
					}


					else if (!question.Contains(frage.SelectToken("stichwort").ToString().ToLower()) && nbre == data.SelectToken("frage").Count())
					{

						found = false;
						break;
					}
				}
				

				if (found == false && nbre == data.SelectToken("frage").Count() && maxcnt < 1)
				{
					await step.Context.SendActivityAsync("es tut mir leid, ich kann diese Frage nicht beantworten.");
				}

				else if (besteFrage.SelectToken("antwort.text").ToString().Length != 0 && (besteFrage.SelectToken("antwort.bild").ToString()).Length != 0)
				{
					await step.Context.SendActivityAsync(besteFrage.SelectToken("antwort.text").ToString());
					var reply = step.Context.Activity.CreateReply();
					var attachment = new Attachment
					{
						ContentUrl = besteFrage.SelectToken("antwort.bild").ToString(),

						ContentType = "image/jpg",
					};
					reply.Attachments = new List<Attachment>() { attachment };
					await step.Context.SendActivityAsync(reply);

				}

			else if ((besteFrage.SelectToken("antwort.text").ToString()).Length == 0 && (besteFrage.SelectToken("antwort.bild").ToString()).Length != 0)
			{
				var reply = step.Context.Activity.CreateReply();
				var attachment = new Attachment
				{
					ContentUrl = besteFrage.SelectToken("antwort.bild").ToString(),

					ContentType = "image/jpg",
				};
				reply.Attachments = new List<Attachment>() { attachment };
				await step.Context.SendActivityAsync(reply);

			}
			else if ((besteFrage.SelectToken("antwort.text").ToString()).Length != 0 && (besteFrage.SelectToken("antwort.bild").ToString()).Length == 0)
			{
				await step.Context.SendActivityAsync(besteFrage.SelectToken("antwort.text").ToString());

			}
			else
			{
					var reply = step.Context.Activity.CreateReply();
					reply.Attachments = new List<Attachment>();
					reply.Attachments.Add(GetHeroCard().ToAttachment());
					await step.Context.SendActivityAsync(reply);

			}

			
			
		}
		return await step.ReplaceDialogAsync(InitialId);

	}	

	private static HeroCard GetHeroCard()
	{
		var heroCard = new HeroCard
		{
			Title = "Frage ohne Antwort\n\n",
			Subtitle = "relevante Frage, die schwer zu beantworten ist",
			Images = new List<CardImage> { new CardImage("C:/Users/PR900162/Pictures/Saved Pictures/Ich weiß nicht!.jpg") },
		};

		return heroCard;
	}

	private async Task<bool> QuestionValidateAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
	{
		// Validate that the user entered a minimum length for their name.
		var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;
		if (value.Length > QuestionLengthMinValue && value.Length < QuestionLengthMaxValue)
		{
			promptContext.Recognized.Value = value;
			return true;
		}
		else if (value.Length > QuestionLengthMaxValue)
		{
			await promptContext.Context.SendActivityAsync($"Die Frage ist zu lang. Geben Sie bitte höchstens `{QuestionLengthMaxValue - 1}` Buchstaben.");
			return false;
		}
		else
			await promptContext.Context.SendActivityAsync($"Die Frage ist zu kurz. Geben Sie bitte mindestens `{QuestionLengthMinValue + 1}` Buchstaben.");
		return false;
	}

	//private async Task<bool> ValidatePostcodeAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
	//{
	//	var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;
	//	if (value.Length == PostcodeLengthValue)
	//	{
	//		promptContext.Recognized.Value = value;
	//		return true;
	//	}
	//	else
	//	{
	//		await promptContext.Context.SendActivityAsync($"Postleitzahl muss genau `{PostcodeLengthValue}`-stellig enthalten sein");
	//		return false;
	//	}
	//}

	//private async Task<bool> ValidateOrtAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
	//{
	//	// Validate that the user entered a minimum lenght for their name
	//	var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;
	//	if (value.Length > OrtLengthMinValue && value.Length < OrtLengthMaxValue)
	//	{
	//		var newValue = value[0].ToString().ToUpper() + value.Substring(1).ToLower();
	//		promptContext.Recognized.Value = newValue;
	//		return true;
	//	}
	//	else if (value.Length > OrtLengthMaxValue)
	//	{
	//		await promptContext.Context.SendActivityAsync($"Der Stadtname ist zu lang. Geben Sie bitte höchstens `{OrtLengthMaxValue - 1}` Buchstaben.");
	//		return false;
	//	}
	//	else
	//		await promptContext.Context.SendActivityAsync($"Die Stadtname ist zu kurz. Geben Sie bitte mindestens `{OrtLengthMinValue + 1}` Buchtaben.");
	//	return false;
	//}
}