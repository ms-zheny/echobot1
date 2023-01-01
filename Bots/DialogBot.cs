using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EchoBot1.Helpers;
using EchoBot1.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace EchoBot1.Bots
{
    public class DialogBot<T>: ActivityHandler where T: Dialog{
    
        protected readonly Dialog _dialog;
        protected readonly StateService _stateService;
        protected readonly ILogger _logger;

        public DialogBot(T dialog, StateService stateService, ILogger<DialogBot<T>> logger)
        {
            _dialog = dialog ?? throw new ArgumentException(nameof(dialog));
            _stateService = stateService ?? throw new ArgumentException(nameof(stateService));
            _logger = logger ?? throw new ArgumentException(nameof(logger));
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            await _stateService.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _stateService.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running dialog with Message Activity.");

            await _dialog.Run(turnContext, _stateService.DialogStateAccessor, cancellationToken);

        }
    }
}
