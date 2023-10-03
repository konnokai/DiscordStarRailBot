using LibGit2Sharp;
using Newtonsoft.Json.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using StackExchange.Redis;
using System.Diagnostics;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;

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
        private DrawingOptions _drawingOptions = new()
        {
            GraphicsOptions = new GraphicsOptions { BlendPercentage = .8F }
        };

        public HSRService(DiscordSocketClient client, HttpClient httpClient)
        {
            _client = client;
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"DiscordStartRailBot ver {Program.VERSION.Replace("/", "_").Replace(":", "_")}");

            _family = _fontCollection.Add(new MemoryStream(Properties.Resources.SDK_SC_Web));
            GameFont = _family.CreateFont(12, FontStyle.Regular);

            _refreshDataTimer = new Timer(async (obj) =>
            {
                try
                {
                    affixScoreJson = JObject.Parse(await _httpClient.GetStringAsync(AFFIX_SCORE_URL));
                    Log.Info("詞條評分資料已更新");

#if DEBUG_CHAR_DATA
                    var data = await GetUserDataAsync("800307542");
                    await GetCharacterEmbedAndImageAsync(data!.Characters[0]);
                    Log.Info("繪製完成");
                    return;
#endif

                    try
                    {
                        if (!Directory.Exists(Program.GetResFilePath("")))
                        {
                            Stopwatch sw = Stopwatch.StartNew();
                            Log.Info("開始從 https://github.com/Mar-7th/StarRailRes.git 複製儲存庫至 SRRes");
                            Repository.Clone("https://github.com/Mar-7th/StarRailRes.git", Program.GetResFilePath(""));
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
                            using var repo = new Repository(Program.GetResFilePath(""));
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
                        if (Directory.Exists(Program.GetResFilePath("")))
                            Directory.Delete(Program.GetResFilePath(""), true);
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
                string canUseCommandUserId = arg.Data.CustomId.Split(':')[2];
                if (canUseCommandUserId != arg.User.Id.ToString())
                {
                    await arg.SendErrorAsync("你不可使用本選項", true);
                    return;
                }

                string userId = arg.Data.CustomId.Split(':')[1];
                var data = await GetUserDataAsync(userId);
                if (data == null)
                {
                    await arg.SendErrorAsync($"獲取資料失敗，請確認UID `{userId}` 是否正確\n" +
                        $"若正確則可能是因 API 問題導致無法查詢資料，請等待一段時間後重試", true);
                    return;
                }

                await arg.ModifyOriginalResponseAsync((act) =>
                {
                    act.Embed = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(data.Player.Nickname)
                        .WithThumbnailUrl($"https://raw.githubusercontent.com/Mar-7th/StarRailRes/master/{data.Player.Avatar.Icon}")
                        .WithDescription($"「" + (string.IsNullOrEmpty(data.Player.Signature) ? "這個人很懶，甚麼都沒留下" : data.Player.Signature) + "」\n\n" +
                            $"**均衡等級**: {data.Player.WorldLevel}\n" +
                            $"**開拓等級**: {data.Player.Level}\n" +
                            $"**角色數量**: {data.Player.SpaceInfo.AvatarCount}\n" +
                            $"**光錐數量**: {data.Player.SpaceInfo.LightConeCount}\n" +
                            $"**成就數量**: {data.Player.SpaceInfo.AchievementCount}")
                        .WithFooter("玩家資料會快取半小時，可能會有資料上的落差", "https://raw.githubusercontent.com/Mar-7th/StarRailRes/master/icon/sign/SettingsAccount.png").Build();
                    act.Components = new ComponentBuilder()
                        .WithButton("玩家資料", $"player_data:{data.Player.Uid}:{arg.User.Id}", disabled: true)
                        .WithButton("角色資料", $"player_char_data:{data.Player.Uid}:{arg.User.Id}").Build();
                    act.Attachments = null;
                });
            }
            else if (arg.Data.CustomId.StartsWith("player_char_data"))
            {
                string canUseCommandUserId = arg.Data.CustomId.Split(':')[2];
                if (canUseCommandUserId != arg.User.Id.ToString())
                {
                    await arg.SendErrorAsync("你不可使用本選項", true);
                    return;
                }

                string userId = arg.Data.CustomId.Split(':')[1];
                var data = await GetUserDataAsync(userId);
                if (data == null)
                {
                    await arg.SendErrorAsync($"獲取資料失敗，請確認UID `{userId}` 是否正確\n" +
                        $"若正確則可能是因 API 問題導致無法查詢資料，請等待一段時間後重試", true);
                    return;
                }

                List<SelectMenuOptionBuilder> selectMenuOptionBuilders = new();
                int index = 0;
                foreach (var item in data.Characters)
                {
                    selectMenuOptionBuilders.Add(new SelectMenuOptionBuilder(item.Id.StartsWith("80") ? "開拓者" : item.Name, index.ToString(), isDefault: index == 0));
                    index++;
                }

                var result = await GetCharacterEmbedAndImageAsync(data.Characters[0]);

                await arg.ModifyOriginalResponseAsync((act) =>
                {
                    act.Embed = result.Embed ?? new EmbedBuilder()
                        .WithErrorColor()
                        .WithDescription("產生角色資料失敗，可能是尚未更新此角色的資料或是此角色無裝備遺器")
                        .Build();
                    act.Attachments = result.Image != null ? new List<FileAttachment>() { new FileAttachment(new MemoryStream(result.Image), "image.jpg") } : null;
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
                    await arg.SendErrorAsync($"獲取資料失敗，請確認UID `{userId}` 是否正確\n" +
                        $"若正確則可能是因 API 問題導致無法查詢資料，請等待一段時間後重試", true);
                    return;
                }

                await arg.SendConfirmAsync("繪製圖片中...", true, true);

                var selectIndex = int.Parse(arg.Data.Values.First());
                List<SelectMenuOptionBuilder> selectMenuOptionBuilders = new();
                int index = 0;
                foreach (var item in data.Characters)
                {
                    selectMenuOptionBuilders.Add(new SelectMenuOptionBuilder(item.Id.StartsWith("80") ? "開拓者" : item.Name, index.ToString(), isDefault: index == selectIndex));
                    index++;
                }

                var result = await GetCharacterEmbedAndImageAsync(data.Characters[selectIndex]);

                await arg.ModifyOriginalResponseAsync((act) =>
                {
                    act.Embed = result.Embed ?? new EmbedBuilder()
                        .WithErrorColor()
                        .WithDescription("產生角色資料失敗，可能是尚未更新此角色的資料或是此角色無裝備遺器")
                        .Build();
                    act.Attachments = result.Image != null ? new List<FileAttachment>() { new FileAttachment(new MemoryStream(result.Image), "image.jpg") } : null;
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

        private async Task<(Embed? Embed, byte[]? Image)> GetCharacterEmbedAndImageAsync(Character character)
        {
            if (affixScoreJson == null)
                return (null, null);

            if (!character.Relics.Any())
                return (null, null);

            var charAffixData = affixScoreJson[character.Id];

            EmbedBuilder eb = new EmbedBuilder()
                .WithColor(Convert.ToUInt32(character.Element.Color.TrimStart('#'), 16))
                .WithTitle((character.Id.StartsWith("80") ? "開拓者" : character.Name) + $" ({character.Level}等 {character.Promotion}階 {character.Rank}命)")
                .WithDescription($"{character.LightCone.Name} ({character.LightCone.Level}等 {character.LightCone.Promotion}階 {character.LightCone.Rank}疊影)" +
                    (charAffixData == null ? "\n注意: 該角色尚無遺器評分資料" : ""))
                .WithThumbnailUrl($"https://raw.githubusercontent.com/Mar-7th/StarRailRes/master/{character.Preview}")
                .WithImageUrl("attachment://image.jpg")
                .WithFooter("詞條評分參考 https://github.com/Mar-7th/StarRailScore ，採用 SRS-N 評分");

            var statisticImage = await DrawCharStatisticImageAsync(character);
            var statisticImageBytes = await DrawCharStatisticImageAsync(character);
            var relicImageBytes = await DrawRelicImageAsync(character, charAffixData);

            using var memoryStream = new MemoryStream();
            using (var image = new Image<Rgba32>(1010, 640, new Color(new Rgb24(79, 79, 79))))
            {
                using (var statisticImage = Image.Load(statisticImageBytes.AsSpan()))
                {
                    image.Mutate(act => act.DrawImage(statisticImage, new Point(0, image.Height - statisticImage.Height), 1f));
                }

                using (var relicImage = Image.Load(relicImageBytes.AsSpan()))
                {
                    image.Mutate(act => act.DrawImage(relicImage, new Point(image.Width - relicImage.Width, 0), 1f));
                }

#if DEBUG_CHAR_DATA
                await image.SaveAsBmpAsync(Program.GetDataFilePath("charImage.bmp"));
#endif
                await image.SaveAsJpegAsync(memoryStream);
            }

            return (eb.Build(), memoryStream.ToArray());
        }

            return (eb.Build(), relicImage.ToArray());
        }

        private async Task<byte[]> DrawCharStatisticImageAsync(Character character)
        {
            using var memoryStream = new MemoryStream();

            await Task.Run(async () =>
            {
                RichTextOptions textOptions = new(_family.CreateFont(24, FontStyle.Regular))
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    WrappingLength = 200,
                };

                RichTextOptions textOptions2 = new(GameFont)
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    WrappingLength = 200,
                };

                // 按順序增加能力值資料
                var attributes = new Dictionary<string, List<Attribute>>();
                foreach (var item in character.Attributes)
                {
                    attributes.Add(item.Field, new() { item });
                }

                // 整理能力值加總
                foreach (var item in character.Additions)
                {
                    if (attributes.ContainsKey(item.Field))
                        attributes[item.Field].Add(item);
                    else
                        attributes.Add(item.Field, new() { item });
                }

                using (var image = new Image<Rgba32>(380, 490, new Color(new Rgb24(79, 79, 79))))
                {
                    int index = 1;
                    foreach (var item in attributes.Take(12))
                    {
                        int x = 10;
                        int y = 10 + 40 * (index - 1);

                        // 能力值背景 (偵錯用)
                        //image.Mutate(act => act.Fill(Color.Gray, new RectangleF(x, y, 360, 30)));

                        // 能力值名稱
                        textOptions.Origin = new PointF(x, y + 16);
                        textOptions.HorizontalAlignment = HorizontalAlignment.Left;
                        image.Mutate((act) => act.DrawText(textOptions, item.Value.First().Name, Color.White));

                        // 能力值加總數值
                        if (item.Value.Count > 1)
                        {
                            textOptions2.Origin = new PointF(x + 300, y + 6);
                            image.Mutate((act) => act.DrawText(textOptions2, $"+{FormatValue(item.Value.Last().Value, item.Value.Last().Percent)}", Color.LightBlue));
                        }

                        // 能力值最終數值
                        textOptions.Origin = new PointF(x + 290, y - 2);
                        textOptions.HorizontalAlignment = HorizontalAlignment.Right;
                        image.Mutate((act) => act.DrawText(textOptions, $"{FormatValue(item.Value.Sum((x) => x.Value), item.Value.First().Percent)}", Color.White));

                        index++;
                    }

                    // 裁切
                    image.Mutate(act => act.Crop(380, 10 + 40 * attributes.Count));                    

#if DEBUG_CHAR_DATA
                    await image.SaveAsBmpAsync(Program.GetDataFilePath("statusImage.bmp"));
#endif
                    await image.SaveAsJpegAsync(memoryStream);
                }
            });

            return memoryStream.ToArray();
        }

        private string FormatValue(double value, bool isPercent = false)
            => isPercent ? $"{Math.Floor(value * 1000) / 10d}%" : $"{Math.Floor(value)}";

        private async Task<byte[]> DrawRelicImageAsync(Character character, JToken? charAffixData)
        {
            using var memoryStream = new MemoryStream();

            await Task.Run(async () =>
            {
                RichTextOptions textOptions = new(GameFont)
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    WrappingLength = 120,
                };

                using (var image = new Image<Rgba32>(630, 640, new Color(new Rgb24(79, 79, 79))))
                {
                    int index = 1;
                    foreach (var relic in character.Relics)
                    {
                        int x = 10 + 310 * ((index - 1) % 2);
                        int y = 10 + 210 * ((index - 1) / 2);

                        // 遺器背景
                        image.Mutate((act) => act.Fill(_drawingOptions, new Color(new Rgba32(28, 28, 28)), new RectangleF(x, y, 300, 200)));

                        // 遺器圖片
                        using (var relicImg = Image.Load(Program.GetResFilePath(relic.Icon)))
                        {
                            relicImg.Mutate(act => act.Resize(96, 96));
                            image.Mutate(act => act.DrawImage(relicImg, new Point(x + 10, y + 25), 1f));
                        }

                        // 星級圖片
                        using (var rarityImg = Image.Load(Program.GetResFilePath($"icon/deco/Rarity{relic.Rarity}.png")))
                        {
                            rarityImg.Mutate(act => act.Resize(128, 32));
                            image.Mutate(act => act.DrawImage(rarityImg, new Point(x - 5, y + 116), 1f));
                        }

                        // 遺器等級
                        textOptions.Origin = new PointF(x + 5 + 96 / 2, y + 96 + 25 + 25);
                        textOptions.HorizontalAlignment = HorizontalAlignment.Center;
                        textOptions.VerticalAlignment = VerticalAlignment.Center;
                        textOptions.WrappingLength = 80;
                        image.Mutate(act => act.DrawText(textOptions, $"+{relic.Level}", Color.White));

                        decimal mainAffixScore = 0;
                        if (charAffixData != null)
                        {
                            // 主詞條分數計算
                            decimal mainAffixWeight = decimal.Parse(charAffixData["main"]![relic.Id.Last().ToString()]![relic.MainAffix.Type]!.ToString());
                            mainAffixScore = mainAffixWeight == 0 ? 0 : Math.Round((relic.Level + 1) / 16m * mainAffixWeight, 2);
                        }

                        // 主詞條圖片及文字繪製
                        using (var mainAffixImg = Image.Load(Program.GetResFilePath(relic.MainAffix.Icon)))
                        {
                            int affixX = x + 20 + 96, affixY = y + 10;
                            mainAffixImg.Mutate(act => act.Resize(32, 32));
                            image.Mutate(act => act.DrawImage(mainAffixImg, new Point(affixX, affixY), 1f));

                            // 詞條名稱
                            textOptions.Origin = new PointF(affixX + 32, affixY + 16);
                            textOptions.HorizontalAlignment = HorizontalAlignment.Left;
                            image.Mutate(act => act.DrawText(textOptions, relic.MainAffix.Name, Color.Goldenrod));

                            // 詞條數值及評分
                            textOptions.Origin = new PointF(affixX + 174, affixY + 7);
                            textOptions.HorizontalAlignment = HorizontalAlignment.Right;
                            image.Mutate(act => act.DrawText(textOptions, $"{relic.MainAffix.Display}" + (mainAffixScore != 0 ? $" ({mainAffixScore})" : ""), Color.Goldenrod));
                        }

                        decimal totalSubAffixScore = 0;
                        for (int i = 0; i < relic.SubAffix.Count; i++)
                        {
                            var subAffix = relic.SubAffix[i];
                            decimal subAffixScore = 0;
                            if (charAffixData != null)
                            {
                                decimal subAffixWeight = decimal.Parse(charAffixData["weight"]![subAffix.Type]!.ToString());
                                subAffixScore = subAffixWeight == 0 ? 0 : (decimal)(subAffix.Count + (subAffix.Step * 0.1)) * subAffixWeight;
                                totalSubAffixScore += subAffixScore;
                            }

                            // 主詞條圖片及文字繪製
                            using (var subAffixImg = Image.Load(Program.GetResFilePath(subAffix.Icon)))
                            {
                                int affixX = x + 20 + 96, affixY = y + 10 + 37 * (i + 1);
                                subAffixImg.Mutate(act => act.Resize(32, 32));
                                image.Mutate(act => act.DrawImage(subAffixImg, new Point(affixX, affixY), 1f));

                                // 詞條名稱
                                textOptions.Origin = new PointF(affixX + 32, affixY + 16);
                                textOptions.HorizontalAlignment = HorizontalAlignment.Left;
                                image.Mutate(act => act.DrawText(textOptions, subAffix.Name, Color.White));

                                // 詞條數值及評分
                                textOptions.Origin = new PointF(affixX + 174, affixY + 7);
                                textOptions.HorizontalAlignment = HorizontalAlignment.Right;
                                image.Mutate(act => act.DrawText(textOptions, $"{subAffix.Display}" + (subAffixScore != 0 ? $" ({subAffixScore})" : ""), Color.White));
                            }
                        }

                        decimal totalScore = 0;
                        if (charAffixData != null)
                        {
                            totalSubAffixScore /= decimal.Parse(charAffixData["max"]!.ToString());
                            totalScore = Math.Round((mainAffixScore / 2 + totalSubAffixScore / 2) * 100);
                        }

                        // 繪製總分及評價
                        textOptions.Origin = new PointF(x + 5 + 96 / 2, y + 96 + 25 + 25 + 20);
                        textOptions.HorizontalAlignment = HorizontalAlignment.Center;
                        image.Mutate(act => act.DrawText(textOptions, totalScore != 0 ? $"{totalScore}% - {GetRank(totalScore)}" : "", GetRankColor(totalScore)));

                        index++;
                    }

                    // 裁切
                    int row = character.Relics.Count / 2 + character.Relics.Count % 1;
                    if (row < 3)
                    {
                        image.Mutate(act => act.Crop(630, 20 + 200 * row + 10 * (row - 1)));
                    }

#if DEBUG_CHAR_DATA
                    await image.SaveAsBmpAsync(Program.GetDataFilePath("relicImage.bmp"));
#endif
                    await image.SaveAsJpegAsync(memoryStream);
                }
            });

            return memoryStream.ToArray();
        }

        private string GetRank(decimal rank)
        {
            return rank switch
            {
                >= 90 => "ACE",
                < 90 and >= 85 => "SSS",
                < 85 and >= 80 => "SS",
                < 80 and >= 70 => "S",
                < 70 and >= 60 => "A",
                < 60 and >= 50 => "B",
                < 50 and >= 40 => "C",
                _ => "D",
            };
        }

        private Color GetRankColor(decimal rank)
        {
            return rank switch
            {
                >= 90 => new Rgba32(255, 0, 63),
                < 90 and >= 85 => new Rgba32(255, 115, 0),
                < 85 and >= 80 => new Rgba32(255, 185, 15),
                < 80 and >= 70 => new Rgba32(255, 255, 0),
                < 70 and >= 60 => new Rgba32(72, 118, 255),
                < 60 and >= 50 => new Rgba32(135, 206, 235),
                < 50 and >= 40 => new Rgba32(255, 128, 153),
                _ => new Rgba32(190, 190, 190),
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
