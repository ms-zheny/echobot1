using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using EchoBot1.Recognizers;
using EchoBot1.CognitiveModels;
using Newtonsoft.Json;
using System.IO;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using Azure.AI.Language.QuestionAnswering;
using EchoBot1.Models;

namespace EchoBot1.Dialogs
{
    public class GetSupportDialog : ComponentDialog
    {
        private readonly StateService _stateService;
        private readonly CsmSupportRecognizer _cluRecognizer;
        private readonly CsmSupportQnARecognizer _cqaARecognizer;
        private readonly IConfiguration _iconfiguration;

        public GetSupportDialog(
            string dialogId,
            StateService stateService,
            CsmSupportRecognizer cluRecognizer,
            CsmSupportQnARecognizer cqaRecognizer,
            IConfiguration configuration) : base(dialogId)
        {
            _stateService = stateService ?? throw new ArgumentException(nameof(stateService));
            _cluRecognizer = cluRecognizer;
            _cqaARecognizer = cqaRecognizer;
            _iconfiguration= configuration;

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            var waterfallSteps = new WaterfallStep[]
            {
               ValidationStepAsync, 
               ResponseStepAsync,
               FeedbackStepAsync,
               SummaryStepAsync,
               FinalStepAsync
            };

            AddDialog(new WaterfallDialog($"{nameof(GetSupportDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(GetSupportDialog)}.validate"));
            AddDialog(new ChoicePrompt($"{nameof(GetSupportDialog)}.response"));
            AddDialog(new ChoicePrompt($"{nameof(GetSupportDialog)}.feedback"));
            AddDialog(new ChoicePrompt($"{nameof(GetSupportDialog)}.summary"));
            
            InitialDialogId = $"{nameof(GetSupportDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> ValidationStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            if (!_cluRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text(
                        "NOTE: CLU is not configured. To enable all capabilities, add 'CluProjectName', 'CluDeploymentName', 'CluAPIKey' and 'CluAPIHostName' to the appsettings.json file.",
                        inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(stepContext.Context, cancellationToken);
            }

            return await stepContext.NextAsync(stepContext.Context, cancellationToken);
        }

        private async Task<DialogTurnResult> ResponseStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {

            stepContext.Values["question"] = stepContext.Context.Activity.Text;

            //Get entities from CLU
            var cluResult = await _cluRecognizer.RecognizeAsync<CsmSupport>(stepContext.Context, cancellationToken);
            var formQuestions = cluResult.Entities.Entities.Select(entity => entity.Text).ToList();
            var question = string.Join(" ", formQuestions);
            if (string.IsNullOrEmpty(question))
            {
                question = stepContext.Context.Activity.Text;
            }

            //Get answer from CQA
            var answerResult = _cqaARecognizer.AskQuestionAsync(question, stepContext.Context, cancellationToken);
            var answer = answerResult.Result.Answers.OrderByDescending(c => c.Confidence).ToList().FirstOrDefault()!.Answer;
            stepContext.Values["answer"] = answer;


            //If response is no answer found 
            if (answer == "No answer found")
            {
                var suggestions = new List<AdaptiveCardActionData>()
                {
                    new AdaptiveCardActionData
                    {
                        Text = "Gearup",
                        Link = "https://gearup.microsoft.com"
                    },
                    new AdaptiveCardActionData
                    {
                        Text = "Intranet",
                        Link = "https://microsoft.sharepoint.com/"
                    }
                };

                var answerCard = CreateAdaptiveCardAttachment("Sorry, we can't find any answer. We would like to suggest to check out the below links:", "EchoBot1.Cards.suggestionCard.json", suggestions);
                await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(answerCard, ssml: "Suggestion"), cancellationToken);
            }
            else
            {
                var answerCard = CreateAdaptiveCardAttachment(answer, "EchoBot1.Cards.adaptiveCard.json");
                await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(answerCard, ssml: "QnA"), cancellationToken);
            }
            

            return await stepContext.NextAsync(answerResult, cancellationToken);


            //switch (cluResult.GetTopIntent().intent)
            //{

            //    case CsmSupport.Intent.GetSupport:

            //        csmRequest = new CsmRequestDetails()
            //        {
            //            RequestType = cluResult.Entities.GetSupportCategory(),
            //            ResponseDetails = (string)stepContext.Values["description"]
            //        };

            //        return await stepContext.NextAsync(csmRequest, cancellationToken);

            //    default:
            //        var choice = new List<string> { "CSM Operation", "Azure Technical Question", "Microsoft Programs" };

            //        return await stepContext.PromptAsync($"{nameof(GetSupportDialog)}.request",
            //            new PromptOptions
            //            {
            //                Prompt = MessageFactory.Text("Sorry, I don't understand the query, Can you please select what you are asking about?"),
            //                Choices = ChoiceFactory.ToChoices(choice),
            //                Style = ListStyle.HeroCard
            //            },
            //            cancellationToken);

            //};
        }

        private async Task<DialogTurnResult> FeedbackStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var choice = new List<string> { "Yes, the answer is good.", "No, it's not what i'm asking." };
            return await stepContext.PromptAsync($"{nameof(GetSupportDialog)}.feedback", new PromptOptions
            {
                Prompt = MessageFactory.Text("We would like to receive your feedback, Is the answer useful?"),
                Choices = ChoiceFactory.ToChoices(choice),
                Style = ListStyle.HeroCard
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var message = $"Question:{(string)stepContext.Values["question"]} ,Answer:{stepContext.Values["answer"]}, Feedback: {((FoundChoice)stepContext.Result).Value}";
            await CsmFeedbackProvider.ProvideFeedbackAsync(message, stepContext.Context, cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thanks for for providing the feedback"), cancellationToken);

            var choice = new List<string> { "Yes", "No" };
            return await stepContext.PromptAsync($"{nameof(GetSupportDialog)}.summary", new PromptOptions
            {
                Prompt = MessageFactory.Text("Is there anything else I can help you?"),
                Choices = ChoiceFactory.ToChoices(choice),
                Style = ListStyle.SuggestedAction

            }, cancellationToken);
            
        }


        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            stepContext.Values["summary"] = ((FoundChoice)stepContext.Result).Value;

            if ((string)stepContext.Values["summary"] == "Yes")
            {
                return await stepContext.ReplaceDialogAsync($"{nameof(MainDialog)}.greeting", null, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Goodbye"), cancellationToken);

                await _stateService.UserState.ClearStateAsync(stepContext.Context, cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }



        private Attachment CreateAdaptiveCardAttachment(string message, string adaptiveCardTemplate, List<AdaptiveCardActionData> actions=null)
        {
            using (var stream = GetType().Assembly.GetManifestResourceStream(adaptiveCardTemplate))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    adaptiveCard = adaptiveCard.Replace("[#Message_TOKEN]", message);

                    if (actions != null)
                    {
                        var i = 1;
                        foreach (var action in actions)
                        {
                            adaptiveCard = adaptiveCard.Replace($"[#Action{i}_TOKEN]", action.Text);
                            adaptiveCard = adaptiveCard.Replace($"[#Action{i}Link_TOKEN]", action.Link);
                            i++;
                        }
                    }
                    
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }
    }
}