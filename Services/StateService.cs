using System;
using EchoBot1.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace EchoBot1.Services
{
    public class StateService
    {
        public StateService(UserState userState, ConversationState conversatoState)
        {
            UserState = userState ?? throw new ArgumentException(nameof(UserState));
            ConversationState = conversatoState ?? throw new ArgumentException(nameof(ConversationState));

            InitializeAccessors();
        }


        public UserState UserState { get;  }

        public ConversationState ConversationState { get; set; }
        
        public IStatePropertyAccessor<UserProfile> UserProfileAccessor { get; set; }
        public IStatePropertyAccessor<ConversationData> ConversationDataAccessor { get; set; }
        public IStatePropertyAccessor<DialogState> DialogStateAccessor { get; set; }
        public IStatePropertyAccessor<BugReportData> BugReportDataAccessor { get; set; }

        //state property name
        private static string UserProfileId { get; } = $"{nameof(StateService)}.UserProfile";

        //state property name
        private static string ConversationDataId { get; } = $"{nameof(StateService)}.ConversationData";
        
        private static string DialogStateId { get; } = $"{nameof(StateService)}.ConversationData";

        private static string BugReportDataId { get; }=$"{nameof(StateService)}.BugReportData";



        public void InitializeAccessors()
        {
            //add property into user state
            UserProfileAccessor = UserState.CreateProperty<UserProfile>(UserProfileId);

            //add property into conversationData state
            ConversationDataAccessor = UserState.CreateProperty<ConversationData>(ConversationDataId);

            DialogStateAccessor = ConversationState.CreateProperty<DialogState>(DialogStateId);

            BugReportDataAccessor = ConversationState.CreateProperty<BugReportData>(BugReportDataId);

        }
    }
}
