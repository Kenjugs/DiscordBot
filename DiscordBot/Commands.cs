using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace DiscordBot {
    public class Commands : ModuleBase {
        [Command("twitch"), Summary("Displays streams with over 500 viewers.")]
        public async Task Twitch([Summary("The (optional) game to search for.")] string gameName = "Street Fighter V") {
            // Define HttpClient request.
            HttpClient httpClient = Authorization.CreateTwitchRequest();

            // Format input to be properly URL-encoded.
            if (Regex.IsMatch(gameName, @"[^a-zA-Z0-9_\s]+|^\W*$")) {
                gameName = "Street Fighter V";
            } else {
                gameName = Regex.Replace(gameName, @"\s+", " ");
                gameName = HttpUtility.UrlEncode(gameName);
            }

            string apiGet = $"https://api.twitch.tv/kraken/search/streams?query={gameName}&limit=100";
            string twitchResponse;

            try {
                twitchResponse = await httpClient.GetStringAsync(apiGet);
            } catch (Exception) {
                await ReplyAsync("Something went wrong. Try again.");
                return;
            }

            JObject twitchObject = JsonConvert.DeserializeObject<JObject>(twitchResponse);

            while (twitchObject["streams"].HasValues) {
                var streams = twitchObject["streams"];
                foreach (var item in streams) {
                    if (item.Value<int>("viewers") > 500) {
                        await ReplyAsync(item["channel"].Value<string>("status"));
                        await ReplyAsync(item["channel"].Value<string>("url"));

                        // Current rate limit seems to be 5 messages / 5 seconds
                        // .NET wrapper has no way to report rate limit.
                        System.Threading.Thread.Sleep(2500);
                    }
                }

                apiGet = twitchObject["_links"].Value<string>("next");
                twitchResponse = await httpClient.GetStringAsync(apiGet);
                twitchObject = JsonConvert.DeserializeObject<JObject>(twitchResponse);
            }

            await ReplyAsync("**End Stream List**");
        }

        [Command("roll"), Summary("Roll a dice.")]
        public async Task Roll([Summary("The (optional) inclusive maximum to roll to.")] int maxRoll = 20) {
            Random r = new Random();
            int ret = 1;

            try {
                ret = r.Next(0, maxRoll) + 1;
            } catch (ArgumentOutOfRangeException) {
                ret = r.Next(0, 20) + 1;
            }

            await ReplyAsync(ret.ToString());
        }
    }
}