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
               DescriptionStepAsync,
               SupportStepAsync,
               SummaryStepAsync,
               FinalStepAsync
            };

            AddDialog(new WaterfallDialog($"{nameof(GetSupportDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(GetSupportDialog)}.description"));
            AddDialog(new ChoicePrompt($"{nameof(GetSupportDialog)}.request"));
            AddDialog(new ChoicePrompt($"{nameof(GetSupportDialog)}.summary"));

            InitialDialogId = $"{nameof(GetSupportDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> DescriptionStepAsync(WaterfallStepContext stepContext,
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

            return await stepContext.PromptAsync($"{nameof(GetSupportDialog)}.description",
                new PromptOptions
                {
                    Prompt =MessageFactory.Text("Can you please provide me some details")

                }, cancellationToken);
        }

        private async Task<DialogTurnResult> SupportStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {

            var cluResult = await _cluRecognizer.RecognizeAsync<CsmSupport>(stepContext.Context, cancellationToken);

            var formQuestions = new List<string>();

            foreach (var entity in cluResult.Entities.Entities)
            {
                formQuestions.Add(entity.Text);
            }

            var question = string.Join(" ", formQuestions);

            if (string.IsNullOrEmpty(question))
            {
                question = (string)stepContext.Result;
            }

            var answerResult = _cqaARecognizer.AskQuestionAsync(question, stepContext.Context, cancellationToken);

            var answer = answerResult.Result.Answers.OrderByDescending(c => c.Confidence).ToList().FirstOrDefault()!.Answer;
            var answerCard = CreateAdaptiveCardAttachment(answer);
            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(answerCard, ssml: "QnA"), cancellationToken);

            //foreach (KnowledgeBaseAnswer answer in answerResult.Result.Answers)
            //{
            //    await stepContext.Context.SendActivityAsync(MessageFactory.Text(answer.Answer), cancellationToken);
            //}

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
        

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var choice = new List<string> { "Yes", "No" };
            return await stepContext.PromptAsync($"{nameof(GetSupportDialog)}.summary", new PromptOptions
            {
                Prompt = MessageFactory.Text("Is there anything else I can help you?"),
                Choices = ChoiceFactory.ToChoices(choice)

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

        private Attachment CreateAdaptiveCardAttachment(string message)
        {
            var cardResourcePath = "EchoBot1.Cards.adaptiveCard.json";

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    adaptiveCard = adaptiveCard.Replace("[#Message_TOKEN]", message);
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
