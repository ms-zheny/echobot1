using EchoBot1.Clu;

namespace EchoBot1.Cqa
{
    public class CqaOptions
    {
        public CqaOptions(CqaApplication app)
        {
            CqaApplication = app;
        }

        public CqaApplication CqaApplication { get; }

        public string Language { get; set; }
    }
}
