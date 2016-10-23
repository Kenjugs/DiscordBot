using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot {
    class Authorization {
        private static string _twitch = "[Twitch Client-ID here]";
        private static string _discord = "[Discord App ID here]";

        public static Task AuthDiscordConnectAsync(Discord.DiscordClient client) {
            return client.Connect(_discord, Discord.TokenType.Bot);
        }

        public static System.Net.Http.HttpClient CreateTwitchRequest() {
            System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.twitchtv.v3+json"));
            httpClient.DefaultRequestHeaders.Add("Client-ID", _twitch);

            return httpClient;
        }
    }
}
