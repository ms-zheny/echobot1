using EchoBot1.Services;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using EchoBot1.Recognizers;
using EchoBot1.CognitiveModels;

namespace EchoBot1.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly StateService _stateService;
        private readonly CsmSupportRecognizer _cluRecognizer;

        public MainDialog(StateService stateService, CsmSupportRecognizer cluRecognizer) : base(nameof(MainDialog))
        {
            _stateService = stateService ?? throw new ArgumentException(nameof(stateService));
            _cluRecognizer = cluRecognizer;

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
            AddDialog(new GreetingDialog($"{nameof(MainDialog)}.greeting", _stateService));
            AddDialog(new BugReportDialog($"{nameof(MainDialog)}.bugReport", _stateService, _cluRecognizer));

            AddDialog(new WaterfallDialog($"{nameof(MainDialog)}.mainFlow", waterfallSteps));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(MainDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var cluResult = await _cluRecognizer.RecognizeAsync<CsmSupport>(stepContext.Context, cancellationToken);

            switch (cluResult.GetTopIntent().intent)
            {
                case CsmSupport.Intent.Greeting:
                    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.greeting", null, cancellationToken);
                default:
                    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.bugReport", null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);

        }
    }
}
