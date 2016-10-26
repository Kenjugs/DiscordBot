using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace DiscordBot {
    class Authorization {
        private static string _twitch = "[Twitch Client-ID here]";
        private static string _discord = "[Discord App ID here]";

        public static Task AuthLoginAsync(DiscordSocketClient client) {
            return client.LoginAsync(TokenType.Bot, _discord);
        }

        public static System.Net.Http.HttpClient CreateTwitchRequest() {
            System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.twitchtv.v3+json"));
            httpClient.DefaultRequestHeaders.Add("Client-ID", _twitch);

            return httpClient;
        }
    }
}
