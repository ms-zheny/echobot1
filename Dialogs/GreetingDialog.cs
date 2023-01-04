using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EchoBot1.Models;
using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace EchoBot1.Dialogs
{
    public class GreetingDialog : ComponentDialog
    {
        private readonly StateService _stateService;
        private readonly IConfiguration _configuration;

        public GreetingDialog(string dialogId, StateService stateService, IConfiguration configuration) : base(dialogId)
        {
            _stateService = stateService ?? throw new ArgumentException(nameof(stateService));
            _configuration = configuration;

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            var waterfallSteps = new WaterfallStep[]
            {
                //InitialStepAsync,
                ChooseTypeStepAsync,
                FinalStepAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(GreetingDialog)}.mainFlow", waterfallSteps));
            //AddDialog(new TextPrompt($"{nameof(GreetingDialog)}.name"));
            AddDialog(new ChoicePrompt($"{nameof(GreetingDialog)}.choosetype"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(GreetingDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            UserProfile userProfile =
                await _stateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            if (string.IsNullOrEmpty(userProfile.Name))
            {
                return await stepContext.PromptAsync($"{nameof(GreetingDialog)}.name", new PromptOptions
                {
                    Prompt = MessageFactory.Text("What's your name?")

                }, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ChooseTypeStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            UserProfile userProfile =
                await _stateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            if (string.IsNullOrEmpty(userProfile.Name))
            {
                userProfile.Name = (string)stepContext.Context.Activity.From.Name;
                await _stateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
            }

            var choice = new List<string> { "Looking for support", "Report an Issue" };

            return await stepContext.PromptAsync($"{nameof(GreetingDialog)}.choosetype",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text($"Hi {userProfile.Name}, How can I help you today?"),
                    Choices = ChoiceFactory.ToChoices(choice),
                    Style = ListStyle.HeroCard

                },
                cancellationToken);
        }


        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            stepContext.Values["choosetype"] = ((FoundChoice)stepContext.Result).Value;

            if (stepContext.Values["choosetype"].ToString() == "Looking for support")
            {
                await stepContext.EndDialogAsync(null, cancellationToken);

                return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.bugReport", null, cancellationToken);
            }

            var comingSoonCard = CreateAdaptiveCardAttachment();
            var response = MessageFactory.Attachment(comingSoonCard, ssml: "Coming Soon!");
            await stepContext.Context.SendActivityAsync(response, cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Goodbye"), cancellationToken);



            return await stepContext.CancelAllDialogsAsync(cancellationToken);
        }


        private Attachment CreateAdaptiveCardAttachment()
        {
            var cardResourcePath = "EchoBot1.Cards.comingSoonCard.json";

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    adaptiveCard = adaptiveCard.Replace("[#StorageSASToken#]", _configuration["StorageSASToken"]);
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
