using System;
using System.Threading;
using System.Threading.Tasks;
using EchoBot1.Models;
using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace EchoBot1.Dialogs
{
    public class GreetingDialog: ComponentDialog
    {
        private readonly StateService _stateService;

        public GreetingDialog(string dialogId, StateService stateService) : base(dialogId)
        {
            _stateService = stateService ?? throw new ArgumentException(nameof(stateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            var waterfallSteps = new WaterfallStep[]
            {
                InitialStepAsync,
                FinalStepAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(GreetingDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(GreetingDialog)}.name"));

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

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            UserProfile userProfile =
                await _stateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            if (string.IsNullOrEmpty(userProfile.Name))
            {
                userProfile.Name = (string)stepContext.Result;
                await _stateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
            }

            var message = $"Hi {userProfile.Name}, How can I help you today?";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(message, message), cancellationToken);

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
