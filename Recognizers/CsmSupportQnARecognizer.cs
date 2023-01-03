using System.Threading;
using Azure.AI.Language.QuestionAnswering;
using EchoBot1.Cqa;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace EchoBot1.Recognizers
{
    public class CsmSupportQnARecognizer
    {
        private readonly CqaRecognizer _recognizer;


        public CsmSupportQnARecognizer(IConfiguration configuration)
        {
            var cqaIsConfigured = !string.IsNullOrEmpty(configuration["CqaProjectName"]) &&
                                  !string.IsNullOrEmpty(configuration["CqaDeploymentName"]) &&
                                  !string.IsNullOrEmpty(configuration["CqaAPIKey"]) &&
                                  !string.IsNullOrEmpty(configuration["CqaAPIHostName"]);
            if (cqaIsConfigured)
            {
                var cqaApplication = new CqaApplication(
                    configuration["CqaProjectName"],
                    configuration["CqaDeploymentName"],
                    configuration["CqaAPIKey"],
                    "https://" + configuration["CqaAPIHostName"]);
                var recognizerOptions = new CqaOptions(cqaApplication) { Language = "en" };

                _recognizer = new CqaRecognizer(recognizerOptions);
            }
        }
        public virtual bool IsConfigured => _recognizer != null;

        public virtual async Task<AnswersResult> AskQuestionAsync(string question, ITurnContext turnContext, CancellationToken cancellationToken)
            => await _recognizer.AskQuestionAsync(question, turnContext, cancellationToken);

        public virtual async Task<T> RecognizeAsync<T>(string question, ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
            => await _recognizer.AskQuestionAsync<T>(question, turnContext, cancellationToken);

    }
}
