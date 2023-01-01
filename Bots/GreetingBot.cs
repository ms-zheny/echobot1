using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EchoBot1.Models;
using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace EchoBot1.Bots
{
    public class GreetingBot: ActivityHandler
    {
        private readonly StateService _stateService;
        public GreetingBot(StateService stateService)
        {
            _stateService = stateService?? throw  new  ArgumentException(nameof(stateService));
        }

        private async Task GetName(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile =
                await _stateService.UserProfileAccessor.GetAsync(turnContext, () => new UserProfile());

            ConversationData conversationData = await _stateService.ConversationDataAccessor.GetAsync(turnContext, () => new ConversationData());

            var message = string.Empty;

            if (!string.IsNullOrEmpty(userProfile.Name))
            {
                 message = $"Hi {userProfile.Name}, How can I help you today?";
                await turnContext.SendActivityAsync(MessageFactory.Text(message, message), cancellationToken);
            }
            else
            {
                if (conversationData.PromptedUserForName)
                {
                    userProfile.Name = turnContext.Activity.Text.Trim();

                    message = $"Thanks {userProfile.Name}. How can I help you today?";

                    await turnContext.SendActivityAsync(MessageFactory.Text(message, message), cancellationToken);

                    conversationData.PromptedUserForName = false;
                }
                else
                {
                    message = "What's your name?";
                    
                    await turnContext.SendActivityAsync(MessageFactory.Text(message, message), cancellationToken);

                    conversationData.PromptedUserForName = true;

                }
            }

            await _stateService.UserProfileAccessor.SetAsync(turnContext, userProfile);
            await _stateService.ConversationDataAccessor.SetAsync(turnContext, conversationData);

            await _stateService.UserState.SaveChangesAsync(turnContext);
            await _stateService.ConversationState.SaveChangesAsync(turnContext);


        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await GetName(turnContext, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await GetName(turnContext, cancellationToken);
                }
            }
        }
    }
}
