using Azure;
using System;
using Azure.AI.Language.QuestionAnswering;
using Microsoft.Bot.Builder;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Builder.TraceExtensions;

namespace EchoBot1.Cqa
{
    public class CqaRecognizer
    {
        private const string CqaTraceLabel = "CQA Trace";

        private readonly QuestionAnsweringClient _conversationsClient;

        private readonly CqaOptions _options;

        public CqaRecognizer(CqaOptions options,QuestionAnsweringClient questionAnsweringClient = default)
        {

            _conversationsClient = questionAnsweringClient ?? new QuestionAnsweringClient(
                new Uri(options.CqaApplication.Endpoint),
                new AzureKeyCredential(options.CqaApplication.EndpointKey));
            _options = options;
        }

        public async Task<AnswersResult> AskQuestionAsync(string question, ITurnContext turnContext, CancellationToken cancellationToken)
        {

            var project = new QuestionAnsweringProject(_options.CqaApplication.ProjectName, _options.CqaApplication.DeploymentName);

            var answersResult = _conversationsClient.GetAnswers(question, project);

            var traceInfo = JObject.FromObject(
                new
                {
                    answersResult,
                });

            await turnContext.TraceActivityAsync("CQA Recognizer", traceInfo, nameof(CqaRecognizer), CqaTraceLabel, cancellationToken);

            return answersResult;
        }

        public async Task<T> AskQuestionAsync<T>(string question, ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var result = new T();
            result.Convert(await AskQuestionAsync(turnContext?.Activity?.AsMessageActivity()?.Text, turnContext, cancellationToken));
            return result;
        }
    }
}
