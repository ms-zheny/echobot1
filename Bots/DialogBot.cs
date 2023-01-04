using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EchoBot1.Helpers;
using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EchoBot1.Bots
{
    public class DialogBot<T>: ActivityHandler where T: Dialog{
    
        protected readonly Dialog _dialog;
        protected readonly StateService _stateService;
        protected readonly ILogger _logger;
        protected readonly IConfiguration _configuration;

        public DialogBot(T dialog, StateService stateService, ILogger<DialogBot<T>> logger, IConfiguration configuration)
        {
            _dialog = dialog ?? throw new ArgumentException(nameof(dialog));
            _stateService = stateService ?? throw new ArgumentException(nameof(stateService));
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _configuration = configuration;
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

        //When members are invited into a conversation.
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var heroCard = new HeroCard
            {
                Title = "Welcome",
                Subtitle = "CSM personal assistant",
                Text = "Hello, my name is Eric your personal bot assistant.",
                Images = new List<CardImage> { new CardImage($"https://echobot1api.azurewebsites.net/assets/images/BOT.jpg") }
                //Images = new List<CardImage> { new CardImage($"https://csmbotstatestorage.blob.core.windows.net/botassets/BOT.jpg{_configuration["StorageSASToken"]}") }
            };

            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var message = MessageFactory.Attachment(heroCard.ToAttachment());
                    await turnContext.SendActivityAsync(message, cancellationToken);
                    //await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}
