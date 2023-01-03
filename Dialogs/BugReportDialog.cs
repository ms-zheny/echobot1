using EchoBot1.Models;
using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using EchoBot1.CognitiveModels;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using EchoBot1.Recognizers;

namespace EchoBot1.Dialogs
{
    public class BugReportDialog : ComponentDialog
    {
        private readonly StateService _stateService;
        private readonly CsmSupportRecognizer _cluRecognizer;

        public BugReportDialog(string dialogId, StateService stateService, CsmSupportRecognizer cluRecognizer) : base(dialogId)
        {
            _stateService = stateService ?? throw new ArgumentException(nameof(stateService));
            _cluRecognizer = cluRecognizer;

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            var waterfallSteps = new WaterfallStep[]
            {
               DescriptionStepAsync,
               //CallbackTimeStepAsync,
               //PhoneNumberStepAsync,
               BugStepAsync,
               SummaryStepAsync,
               FinalStepAsync
            };

            AddDialog(new WaterfallDialog($"{nameof(BugReportDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.description"));
            //AddDialog(new DateTimePrompt($"{nameof(BugReportDialog)}.callbackTime", CallbackTimeValidatorAsync));
            //AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.phoneNumber", PhoneNumberValidatorAsync));
            AddDialog(new ChoicePrompt($"{nameof(BugReportDialog)}.bug"));
            AddDialog(new ChoicePrompt($"{nameof(BugReportDialog)}.summary"));

            InitialDialogId = $"{nameof(BugReportDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> DescriptionStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            if (!_cluRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: CLU is not configured. To enable all capabilities, add 'CluProjectName', 'CluDeploymentName', 'CluAPIKey' and 'CluAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }


            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.description", new PromptOptions
            {
                Prompt = MessageFactory.Text("Can you please provide me some details")

            }, cancellationToken);
        }

        private async Task<DialogTurnResult> CallbackTimeStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            stepContext.Values["description"] = (string)stepContext.Result;

            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.callbackTime", new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter in a callback time"),
                RetryPrompt = MessageFactory.Text("The value entered must be between the hours of 9am and 5pm.")

            }, cancellationToken);
        }

        private async Task<DialogTurnResult> PhoneNumberStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            stepContext.Values["callbackTime"] = Convert.ToDateTime(((List<DateTimeResolution>)stepContext.Result).FirstOrDefault().Value);

            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.phoneNumber", new PromptOptions
            {
                Prompt = MessageFactory.Text("Please provide your phone number"),
                RetryPrompt = MessageFactory.Text("Please enter a valid phone number")

            }, cancellationToken);
        }


        private async Task<DialogTurnResult> BugStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            //stepContext.Values["phoneNumber"] = (string)stepContext.Result;
            stepContext.Values["description"] = (string)stepContext.Result;

            var cluResult = await _cluRecognizer.RecognizeAsync<CsmSupport>(stepContext.Context, cancellationToken);
            CsmRequestDetails csmRequest;

            switch (cluResult.GetTopIntent().intent)
            {

                case CsmSupport.Intent.GetSupport:

                    csmRequest = new CsmRequestDetails()
                    {
                        RequestType = cluResult.Entities.GetSupportCategory(),
                        ResponseDetails = (string)stepContext.Values["description"]
                    };

                    return await stepContext.NextAsync(csmRequest, cancellationToken);

                default:
                    var choice = new List<string> { "security", "crash", "performance" };

                    return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.bug",
                        new PromptOptions
                        {
                            Prompt = MessageFactory.Text("Please enter the type of bug"),
                            Choices = ChoiceFactory.ToChoices(choice)
                        },
                        cancellationToken);

            };
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            if (stepContext.Result.GetType().Name == "FoundChoice")
            {
                stepContext.Values["bug"] = ((FoundChoice)stepContext.Result).Value;
            }
            else
            {
                stepContext.Values["bug"] = ((CsmRequestDetails)stepContext.Result).RequestType;
            }

            UserProfile userProfile =
                await _stateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            BugReportData bugReportData =
                await _stateService.BugReportDataAccessor.GetAsync(stepContext.Context, () => new());


            bugReportData.Name = userProfile.Name;
            bugReportData.Bug = (string)stepContext.Values["bug"];
            bugReportData.Description = (string)stepContext.Values["description"];
            //bugReportData.CallbackTime = (DateTime)stepContext.Values["callbackTime"];
            //bugReportData.PhoneNumber = (string)stepContext.Values["phoneNumber"];
            await _stateService.BugReportDataAccessor.SetAsync(stepContext.Context, bugReportData);

            var heroCard = new HeroCard
            {
                Title = "Response",
                Subtitle = "Find intention",
                Text = $"Thanks for asking about the {bugReportData.Bug}.  Here is what I found: --[response will be return from QnA maker]-- coming soon."
            };

            var summary = MessageFactory.Attachment(heroCard.ToAttachment());
            await stepContext.Context.SendActivityAsync(summary, cancellationToken);

            //await stepContext.Context.SendActivityAsync(MessageFactory.Text("Here is a summary of your bug report:"), cancellationToken);
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Reporter: {userProfile.Name}"), cancellationToken);
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Description: {bugReportData.Description}"), cancellationToken);
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Callback Time: {bugReportData.CallbackTime}"), cancellationToken);
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Phone Number: {bugReportData.PhoneNumber}"), cancellationToken);
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Bug: {bugReportData.Bug}"), cancellationToken);


            var choice = new List<string> { "Yes", "No" };

            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.summary", new PromptOptions
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

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        private Task<bool> CallbackTimeValidatorAsync(PromptValidatorContext<IList<DateTimeResolution>> promptContext,
            CancellationToken cancellationToken)
        {
            var valid = false;

            if (promptContext.Recognized.Succeeded)
            {
                var resolution = promptContext.Recognized.Value.First();
                DateTime selectedDate = Convert.ToDateTime(resolution.Value);
                TimeSpan start = new TimeSpan(9, 0, 0);
                TimeSpan end = new TimeSpan(17, 0, 0);

                if (selectedDate.TimeOfDay >= start && selectedDate.TimeOfDay <= end)
                {
                    valid = true;
                }
            }

            return Task.FromResult(valid);
        }

        private Task<bool> PhoneNumberValidatorAsync(PromptValidatorContext<string> promptContext,
            CancellationToken cancellationToken)
        {
            var valid = false;

            if (promptContext.Recognized.Succeeded)
            {
                var resolution = promptContext.Recognized.Value.ToString();
                valid = Regex.Match(resolution, @"^(?:(?:\(?(?:00|\+)([1-4]\d\d|[1-9]\d+)\)?)[\-\.\ \\\/]?)?((?:\(?\d{1,}\)?[\-\.\ \\\/]?)+)(?:[\-\.\ \\\/]?(?:#|ext\.?|extension|x)[\-\.\ \\\/]?(\d+))?$").Success;
            }

            return Task.FromResult(valid);
        }
    }
}
