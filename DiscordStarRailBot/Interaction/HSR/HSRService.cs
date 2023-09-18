using LibGit2Sharp;
using Newtonsoft.Json.Linq;
using SixLabors.Fonts;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text;

namespace DiscordStarRailBot.Interaction.HSR.Service
{
    public class HSRService : IInteractionService
    {
        public Font GameFont { get; private set; }

        private const string AFFIX_SCORE_URL = "https://raw.githubusercontent.com/Mar-7th/StarRailScore/master/score.json";

        private JObject? affixScoreJson = null;
        private FontCollection _fontCollection = new();
        private FontFamily _family;

        private readonly DiscordSocketClient _client;
        private readonly HttpClient _httpClient;
        private readonly Timer _refreshDataTimer;

        public HSRService(DiscordSocketClient client, HttpClient httpClient)
        {
            _client = client;
            _httpClient = httpClient;

            _family = _fontCollection.Add(new MemoryStream(Properties.Resources.SDK_SC_Web));
            GameFont = _family.CreateFont(24, FontStyle.Regular);

            _refreshDataTimer = new Timer(async (obj) =>
            {
                try
                {
                    affixScoreJson = JObject.Parse(await _httpClient.GetStringAsync(AFFIX_SCORE_URL));
                    Log.Info("詞條評分資料已更新");

                    try
                    {
                        if (!Directory.Exists(Program.GetDataFilePath("SRRes")))
                        {
                            Stopwatch sw = Stopwatch.StartNew();
                            Log.Info("開始從 https://github.com/Mar-7th/StarRailRes.git 複製儲存庫至 SRRes");
                            Repository.Clone("https://github.com/Mar-7th/StarRailRes.git", Program.GetDataFilePath("SRRes"));
                            sw.Stop();
                            Log.Info($"複製完成，執行了 {sw.Elapsed:hh\\:mm\\:ss}");
                        }
                        else
                        {
                            Log.Info("開始從 https://github.com/Mar-7th/StarRailRes.git 拉取儲存庫至 SRRes");

                            // Credential information to fetch
                            PullOptions options = new()
                            {
                                FetchOptions = new FetchOptions()
                            };

                            // User information to create a merge commit
                            var signature = new Signature(
                                new Identity("Local", "local@local.host"), DateTimeOffset.Now);

                            // Pull
                            using var repo = new Repository(Program.GetDataFilePath("SRRes"));
                            var result = Commands.Pull(repo, signature, options);
                            if (result.Status == MergeStatus.UpToDate)
                                Log.Info($"沒有需要拉取的");
                            else
                                Log.Info($"拉取完成，新 Commit 訊息: {result.Commit.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Git");
                        if (Directory.Exists(Program.GetDataFilePath("SRRes")))
                            Directory.Delete(Program.GetDataFilePath("SRRes"), true);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "RefreshData");
                }
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromHours(6));

            _client.ButtonExecuted += _client_ButtonExecuted;
            _client.SelectMenuExecuted += _client_SelectMenuExecuted;
        }

        private async Task _client_ButtonExecuted(SocketMessageComponent arg)
        {
            if (arg.HasResponded)
                return;

            await arg.DeferAsync();

            if (arg.Data.CustomId.StartsWith("player_data"))
            {
                string canUseCommandserId = arg.Data.CustomId.Split(':')[2];
                if (canUseCommandserId != arg.User.Id.ToString())
                {
                    await arg.SendErrorAsync("你不可使用本選項", true);
                    return;
                }

                string userId = arg.Data.CustomId.Split(':')[1];
                var data = await GetUserDataAsync(userId);
                if (data == null)
                {
                    await arg.SendErrorAsync($"獲取資料失敗，請確認UID `{userId}` 是否正確", true);
                    return;
                }

                await arg.ModifyOriginalResponseAsync((act) =>
                {
                    act.Embed = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(data.Player.Nickname)
                        .WithThumbnailUrl($"https://raw.githubusercontent.com/Mar-7th/StarRailRes/master/{data.Player.Avatar.Icon}")
                        .WithDescription($"「{data.Player.Signature}」\n\n" +
                            $"**均衡等級**: {data.Player.WorldLevel}\n" +
                            $"**開拓等級**: {data.Player.Level}\n" +
                            $"**角色數量**: {data.Player.SpaceInfo.AvatarCount}\n" +
                            $"**光錐數量**: {data.Player.SpaceInfo.LightConeCount}\n" +
                            $"**成就數量**: {data.Player.SpaceInfo.AchievementCount}")
                        .WithFooter("玩家資料會快取半小時，可能會有資料上的落差", "https://raw.githubusercontent.com/Mar-7th/StarRailRes/master/icon/sign/SettingsAccount.png").Build();
                    act.Components = new ComponentBuilder()
                        .WithButton("玩家資料", $"player_data:{data.Player.Uid}:{arg.User.Id}", disabled: true)
                        .WithButton("角色資料", $"player_char_data:{data.Player.Uid}:{arg.User.Id}").Build();
                });
            }
            else if (arg.Data.CustomId.StartsWith("player_char_data"))
            {
                string canUseCommandserId = arg.Data.CustomId.Split(':')[2];
                if (canUseCommandserId != arg.User.Id.ToString())
                {
                    await arg.SendErrorAsync("你不可使用本選項", true);
                    return;
                }

                string userId = arg.Data.CustomId.Split(':')[1];
                var data = await GetUserDataAsync(userId);
                if (data == null)
                {
                    await arg.SendErrorAsync($"獲取資料失敗，請確認UID `{userId}` 是否正確", true);
                    return;
                }

                List<SelectMenuOptionBuilder> selectMenuOptionBuilders = new();
                int index = 0;
                foreach (var item in data.Characters)
                {
                    selectMenuOptionBuilders.Add(new SelectMenuOptionBuilder(item.Name, index.ToString(), isDefault: index == 0));
                    index++;
                }

                await arg.ModifyOriginalResponseAsync((act) =>
                {
                    act.Embed = GetCharacterDataEmbed(data.Characters[0]);
                    act.Components = new ComponentBuilder()
                        .WithButton("玩家資料", $"player_data:{data.Player.Uid}:{arg.User.Id}")
                        .WithButton("角色資料", $"player_char_data:{data.Player.Uid}:{arg.User.Id}", disabled: true)
                        .WithSelectMenu($"player_char_data_select:{data.Player.Uid}:{arg.User.Id}", selectMenuOptionBuilders)
                        .Build();
                });
            }
        }

        private async Task _client_SelectMenuExecuted(SocketMessageComponent arg)
        {
            try
            {
                if (arg.HasResponded)
                    return;

                if (!arg.Data.CustomId.StartsWith("player_char_data_select"))
                    return;

                await arg.DeferAsync();

                string canUseCommandserId = arg.Data.CustomId.Split(':')[2];
                if (canUseCommandserId != arg.User.Id.ToString())
                {
                    await arg.SendErrorAsync("你不可使用本選項", true);
                    return;
                }

                string userId = arg.Data.CustomId.Split(':')[1];
                var data = await GetUserDataAsync(userId);
                if (data == null)
                {
                    await arg.SendErrorAsync($"獲取資料失敗，請確認UID `{userId}` 是否正確", true);
                    return;
                }

                var selectIndex = int.Parse(arg.Data.Values.First());
                List<SelectMenuOptionBuilder> selectMenuOptionBuilders = new();
                int index = 0;
                foreach (var item in data.Characters)
                {
                    selectMenuOptionBuilders.Add(new SelectMenuOptionBuilder(item.Name, index.ToString(), isDefault: index == selectIndex));
                    index++;
                }

                await arg.ModifyOriginalResponseAsync((act) =>
                {
                    act.Embed = GetCharacterDataEmbed(data.Characters[selectIndex]) ?? new EmbedBuilder()
                        .WithErrorColor()
                        .WithDescription("產生角色資料失敗，可能是尚未更新此角色的資料")
                        .Build();
                    act.Components = new ComponentBuilder()
                        .WithButton("玩家資料", $"player_data:{data.Player.Uid}:{arg.User.Id}")
                        .WithButton("角色資料", $"player_char_data:{data.Player.Uid}:{arg.User.Id}", disabled: true)
                        .WithSelectMenu($"player_char_data_select:{data.Player.Uid}:{arg.User.Id}", selectMenuOptionBuilders)
                        .Build();
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "_client_SelectMenuExecuted");
            }
        }

        private Embed? GetCharacterDataEmbed(Character character)
        {
            if (affixScoreJson == null)
                return null;

            var charAffixData = affixScoreJson[character.Id];
            if (charAffixData == null)
                return null;

            EmbedBuilder eb = new EmbedBuilder()
                .WithColor(Convert.ToUInt32(character.Element.Color.TrimStart('#'), 16))
                .WithTitle($"{character.Name} ({character.Level}等 {character.Promotion}階 {character.Rank}命)")
                .WithDescription($"{character.LightCone.Name} ({character.LightCone.Level}等 {character.LightCone.Promotion}階 {character.LightCone.Rank}疊影)")
                .WithThumbnailUrl($"https://raw.githubusercontent.com/Mar-7th/StarRailRes/master/{character.Preview}")
                .WithFooter("詞條評分參考 https://github.com/Mar-7th/StarRailScore ，採用 SRS-N 評分");

            int index = 1;
            foreach (var relic in character.Relics)
            {
                StringBuilder sb = new();

                decimal mainAffixWeight = decimal.Parse(charAffixData["main"]![index.ToString()]![relic.MainAffix.Type]!.ToString());
                decimal mainAffixScore = mainAffixWeight == 0 ? 0 : (relic.Level + 1) / 16 * mainAffixWeight;

                sb.AppendLine($"**{relic.MainAffix.Name}** __{relic.MainAffix.Display}__ ({mainAffixScore})");

                decimal totalSubAffixScore = 0;
                foreach (var subAffix in relic.SubAffix)
                {
                    decimal subAffixWeight = decimal.Parse(charAffixData["weight"]![subAffix.Type]!.ToString());
                    decimal subAffixScore = subAffixWeight == 0 ? 0 : (decimal)(subAffix.Count + (subAffix.Step * 0.1)) * subAffixWeight;
                    totalSubAffixScore += subAffixScore;

                    sb.AppendLine($"{subAffix.Name} __{subAffix.Display}__ ({subAffixScore})");
                }
                totalSubAffixScore /= decimal.Parse(charAffixData["max"]!.ToString());

                decimal totalScore = Math.Round((mainAffixScore / 2 + totalSubAffixScore / 2) * 100);

                eb.AddField($"{relic.Name} +{relic.Level} ({totalScore}% - {GetRank(totalScore)})", sb.ToString());
                index++;
            }

            return eb.Build();
        }

        private string GetRank(decimal rank)
        {
            return rank switch
            {
                90 => "ACE",
                < 90 and >= 85 => "SSS",
                < 85 and >= 80 => "SS",
                < 80 and >= 70 => "S",
                < 70 and >= 60 => "A",
                < 60 and >= 50 => "B",
                < 50 and >= 40 => "C",
                _ => "D",
            };
        }

        internal async Task<SRInfoJson?> GetUserDataAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new NullReferenceException(nameof(userId));

            try
            {
                string json = (await Program.RedisDb.StringGetAsync($"hsr:{userId}")).ToString();

                if (string.IsNullOrEmpty(json))
                {
                    json = await _httpClient.GetStringAsync($"https://api.mihomo.me/sr_info_parsed/{userId}?lang=cht");
                    if (json == "{\"detail\":\"Invalid uid\"}")
                        return null;

                    await Program.RedisDb.StringSetAsync(new RedisKey($"hsr:{userId}"), json, TimeSpan.FromMinutes(30));
                }

                SRInfoJson? userInfo = JsonConvert.DeserializeObject<SRInfoJson>(json);
                return userInfo;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "HSR-GetUserData");
                return null;
            }
        }
    }
}
