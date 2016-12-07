using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace directlinedemo
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            await Conversation.SendAsync(activity, () => new EchoDialog());

            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }
    }
    
    [Serializable]
    public class EchoDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(GetWeather);
        }

        public async Task GetWeather(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var inboundMessage = await argument;
            var outboundMessage = context.MakeMessage();

            // get the weather forecase from yahoo weather and return it
            var client = new HttpClient() { BaseAddress = new Uri("https://query.yahooapis.com") };
            var result = client.GetStringAsync($"/v1/public/yql?q=select%20*%20from%20weather.forecast%20where%20woeid%20in%20(select%20woeid%20from%20geo.places(1)%20where%20text%3D%22{inboundMessage.Text}%22)&format=json&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys").Result;
            var data = ((dynamic)JObject.Parse(result)).query.results.channel.item;
            if (data != null)
            {
                var sb = new StringBuilder();

                sb.AppendLine($"{data.title}");
                var forecasts = data.forecast;
                foreach (var forecast in forecasts)
                {
                    var summary = $"* {forecast.date} is {forecast.text} with temperatures between {forecast.high}°f and {forecast.low}°f";
                    sb.AppendLine(summary);
                }

                await context.PostAsync(sb.ToString());
            }

            context.Wait(GetWeather);
        }
    }
}