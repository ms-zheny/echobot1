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
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace EchoBot1.Dialogs
{
    public class BugReportDialog : ComponentDialog
    {
        private readonly StateService _stateService;
        public BugReportDialog(string dialogId, StateService stateService) : base(dialogId)
        {
            _stateService = stateService ?? throw new ArgumentException(nameof(stateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            var waterfallSteps = new WaterfallStep[]
            {
               DescriptionStepAsync,
               CallbackTimeStepAsync,
               PhoneNumberStepAsync,
               BugStepAsync,
               SummaryStepAsync
            };

            AddDialog(new WaterfallDialog($"{nameof(BugReportDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.description"));
            AddDialog(new DateTimePrompt($"{nameof(BugReportDialog)}.callbackTime", CallbackTimeValidatorAsync));
            AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.phoneNumber", PhoneNumberValidatorAsync));
            AddDialog(new ChoicePrompt($"{nameof(BugReportDialog)}.bug"));

            InitialDialogId = $"{nameof(BugReportDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> DescriptionStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {

            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.description", new PromptOptions
            {
                Prompt = MessageFactory.Text("Enter a description for your report")

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
            stepContext.Values["phoneNumber"] = (string)stepContext.Result;

            var choice = new List<string> { "security", "crash", "performance" };

            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.bug",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter the type of bug"),
                    Choices = ChoiceFactory.ToChoices(choice)
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            stepContext.Values["bug"] = ((FoundChoice)stepContext.Result).Value;

            UserProfile userProfile =
                await _stateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            BugReportData bugReportData =
                await _stateService.BugReportDataAccessor.GetAsync(stepContext.Context, () => new());


            bugReportData.Name = userProfile.Name;
            bugReportData.Bug = (string)stepContext.Values["bug"];
            bugReportData.Description = (string)stepContext.Values["description"];
            bugReportData.CallbackTime = (DateTime)stepContext.Values["callbackTime"];
            bugReportData.PhoneNumber = (string)stepContext.Values["phoneNumber"];


            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Here is a summary of your bug report:"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Reporter: {userProfile.Name}"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Description: {bugReportData.Description}"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Callback Time: {bugReportData.CallbackTime}"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Phone Number: {bugReportData.PhoneNumber}"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Bug: {bugReportData.Bug}"), cancellationToken);

            await _stateService.BugReportDataAccessor.SetAsync(stepContext.Context, bugReportData);


            return await stepContext.EndDialogAsync(null, cancellationToken);

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

            return Task.FromResult(valid) ;
        }
    }
}
