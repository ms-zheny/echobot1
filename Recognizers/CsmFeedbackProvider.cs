using EchoBot1.Clu;
using Microsoft.Bot.Builder;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Schema;

namespace EchoBot1.Recognizers
{
    public static class CsmFeedbackProvider
    {
        public static async Task<ResourceResponse> ProvideFeedbackAsync(string utterance, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var traceInfo = JObject.FromObject(
                new
                {
                    response = utterance,
                });

            return await turnContext.TraceActivityAsync("Response Feedback", traceInfo, nameof(CluRecognizer), "Feedbacks", cancellationToken);
        }
    }
}
