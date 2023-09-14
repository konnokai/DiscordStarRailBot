using static DiscordStarRailBot.DataBase.Table.NotifyConfig;

namespace DiscordStarRailBot.DataBase.Table
{
    public class CafeInviteTicketUpdateTime : DbEntity
    {
        public ulong UserId { get; set; }
        public RegionType RegionTypeId { get; set; }
        public DateTime NotifyDateTime { get; set; }
    }
}
