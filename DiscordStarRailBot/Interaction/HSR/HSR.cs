using Discord.Interactions;
using DiscordStarRailBot.DataBase;
using DiscordStarRailBot.DataBase.Table;
using DiscordStarRailBot.Interaction.Attribute;
using DiscordStarRailBot.Interaction.HSR.Service;

namespace DiscordStarRailBot.Interaction.HSR
{
    public class HSR : TopLevelModule<HSRService>
    {
        [SlashCommand("link-user-id", "綁定你的Discord及遊戲UID")]
        [CommandExample("800000000")]
        public async Task LinkUserId([Summary("UID", "留空則取消綁定")] string userId = "")
        {
            await DeferAsync(true);

            try
            {
                using var db = DBContext.GetDbContext();
                var playerIdLink = db.PlayerIds.FirstOrDefault((x) => x.UserId == Context.User.Id);

                if (string.IsNullOrEmpty(userId))
                {
                    if (playerIdLink != null)
                    {
                        db.PlayerIds.Remove(playerIdLink);
                        await db.SaveChangesAsync();

                        await Context.Interaction.SendConfirmAsync("已移除你綁定的UID", true);
                        return;
                    }

                    await Context.Interaction.SendErrorAsync("尚未綁定UID", true);
                    return;
                }

                var data = await _service.GetUserDataAsync(userId);
                if (data == null)
                {
                    await Context.Interaction.SendErrorAsync($"綁定UID失敗，請確認UID `{userId}` 是否正確\n" +
                        $"若正確則可能是因 API 問題導致無法查詢資料，請等待一段時間後重試", true);
                    return;
                }

                if (playerIdLink == null)
                    playerIdLink = new PlayerIdLink() { UserId = Context.User.Id, PlayerId = data.Player.Uid };
                else
                    playerIdLink.PlayerId = data.Player.Uid;

                db.PlayerIds.Update(playerIdLink);
                await db.SaveChangesAsync();

                await Context.Interaction.SendConfirmAsync($"綁定成功，玩家名稱: `{data.Player.Nickname}`", true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "HSR-LinkUserId");
                await Context.Interaction.SendErrorAsync("未知的錯誤", true);
            }
        }

        [SlashCommand("get-user-detail", "取得玩家資料，不輸入參數則使用自己綁定的UID")]
        public async Task GetUserDetail([Summary("使用者", "使用指定使用者綁定的UID，此為優先選項")] IUser? user = null, [Summary("UID", "指定UID")] string userId = "")
        {
            await DeferAsync(false);

            using var db = DBContext.GetDbContext();
            if (user != null)
            {
                var playerIdLink = db.PlayerIds.FirstOrDefault((x) => x.UserId == user.Id);
                if (playerIdLink != null)                
                    userId = playerIdLink.PlayerId;                
            }

            if (string.IsNullOrEmpty(userId))
            {
                var playerIdLink = db.PlayerIds.FirstOrDefault((x) => x.UserId == Context.User.Id);
                if (playerIdLink != null)
                {
                    userId = playerIdLink.PlayerId;
                }
                else
                {
                    await Context.Interaction.SendErrorAsync("未輸入 UID 且未綁定 UID，請輸入要取得的 UID 或是執行綁定指令", true);
                    return;
                }
            }

            var data = await _service.GetUserDataAsync(userId);
            if (data == null)
            {
                await Context.Interaction.SendErrorAsync($"獲取資料失敗，請確認UID `{userId}` 是否正確\n" +
                        $"若正確則可能是因 API 問題導致無法查詢資料，請等待一段時間後重試", true);
                return;
            }

            await Context.Interaction.FollowupAsync(embed: new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(data.Player.Nickname)
                    .WithThumbnailUrl($"https://raw.githubusercontent.com/Mar-7th/StarRailRes/master/{data.Player.Avatar.Icon}")
                    .WithDescription($"「{data.Player.Signature}」\n\n" +
                        $"**均衡等級**: {data.Player.WorldLevel}\n" +
                        $"**開拓等級**: {data.Player.Level}\n" +
                        $"**角色數量**: {data.Player.SpaceInfo.AvatarCount}\n" +
                        $"**光錐數量**: {data.Player.SpaceInfo.LightConeCount}\n" +
                        $"**成就數量**: {data.Player.SpaceInfo.AchievementCount}")
                    .WithFooter("玩家資料會快取半小時，可能會有資料上的落差", "https://raw.githubusercontent.com/Mar-7th/StarRailRes/master/icon/sign/SettingsAccount.png").Build(),
                components: new ComponentBuilder()
                    .WithButton("玩家資料", $"player_data:{data.Player.Uid}:{Context.User.Id}", disabled: true)
                    .WithButton("角色資料", $"player_char_data:{data.Player.Uid}:{Context.User.Id}", disabled: !data.Player.IsDisplay).Build());
        }
    }
}
