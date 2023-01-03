using System.Threading;
using System.Threading.Tasks;
using EchoBot1.Clu;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Configuration;

namespace EchoBot1.Recognizers
{
    public class CsmSupportRecognizer
    {
        private readonly CluRecognizer _recognizer;

        public CsmSupportRecognizer(IConfiguration configuration)
        {
            var cluIsConfigured = !string.IsNullOrEmpty(configuration["CluProjectName"]) &&
                                  !string.IsNullOrEmpty(configuration["CluDeploymentName"]) &&
                                  !string.IsNullOrEmpty(configuration["CluAPIKey"]) &&
                                  !string.IsNullOrEmpty(configuration["CluAPIHostName"]);
            if (cluIsConfigured)
            {
                var cluApplication = new CluApplication(
                    configuration["CluProjectName"],
                    configuration["CluDeploymentName"],
                    configuration["CluAPIKey"],
                    "https://" + configuration["CluAPIHostName"]);
                var recognizerOptions = new CluOptions(cluApplication) { Language = "en" };

                _recognizer = new CluRecognizer(recognizerOptions);
            }
        }

        public virtual bool IsConfigured => _recognizer != null;

        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
            => await _recognizer.RecognizeAsync(turnContext, cancellationToken);

        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
            => await _recognizer.RecognizeAsync<T>(turnContext, cancellationToken);
    }
}
