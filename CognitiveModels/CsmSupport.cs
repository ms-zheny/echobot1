using EchoBot1.Clu;
using Microsoft.Bot.Builder;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using EchoBot1.Recognizers;

namespace EchoBot1.CognitiveModels
{
    public class CsmSupport: IRecognizerConvert
    {

        public enum Intent
        {
            GetSupport,
            Greeting,
            Cancel,
            None
        }

        public string Text { get; set; }

        public string AlteredText { get; set; }

        public Dictionary<Intent, IntentScore> Intents { get; set; }

        public CluEntities Entities { get; set; }

        public IDictionary<string, object> Properties { get; set; }

        public void Convert(dynamic result)
        {
            var jsonResult = JsonConvert.SerializeObject(result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var app = JsonConvert.DeserializeObject<CsmSupport>(jsonResult);

            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        public class CluEntities
        {
            public CluEntity[] Entities;

            public CluEntity[] GetSupportCategoryList() => Entities.ToArray();

            public string GetSupportCategory() => GetSupportCategoryList().FirstOrDefault()?.Text;
        }

        public (Intent intent, double score) GetTopIntent()
        {
            var maxIntent = Intent.None;
            var max = 0.0;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }

            return (maxIntent, max);
        }
    }
}
