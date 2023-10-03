#nullable disable

namespace DiscordStarRailBot.Interaction.HSR
{
    public class SRInfoJson
    {
        [JsonProperty("player")]
        public Player Player { get; set; }

        [JsonProperty("characters")]
        public List<Character> Characters { get; set; }

        [JsonProperty("detail")]
        public string Detail { get; set; }
    }

    public class Addition
    {
        [JsonProperty("field")]
        public string Field { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("display")]
        public string Display { get; set; }

        [JsonProperty("percent")]
        public bool Percent { get; set; }
    }

    public class Attribute
    {
        [JsonProperty("field")]
        public string Field { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("display")]
        public string Display { get; set; }

        [JsonProperty("percent")]
        public bool Percent { get; set; }
    }

    public class Avatar
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }
    }

    public class ChallengeData
    {
        [JsonProperty("maze_group_id")]
        public int MazeGroupId { get; set; }

        [JsonProperty("maze_group_index")]
        public int MazeGroupIndex { get; set; }

        [JsonProperty("pre_maze_group_index")]
        public int PreMazeGroupIndex { get; set; }
    }

    public class Character
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("rarity")]
        public int Rarity { get; set; }

        [JsonProperty("rank")]
        public int Rank { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("promotion")]
        public int Promotion { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("preview")]
        public string Preview { get; set; }

        [JsonProperty("portrait")]
        public string Portrait { get; set; }

        [JsonProperty("rank_icons")]
        public List<string> RankIcons { get; set; }

        [JsonProperty("path")]
        public Path Path { get; set; }

        [JsonProperty("element")]
        public Element Element { get; set; }

        [JsonProperty("skills")]
        public List<Skill> Skills { get; set; }

        [JsonProperty("skill_trees")]
        public List<SkillTree> SkillTrees { get; set; }

        [JsonProperty("light_cone")]
        public LightCone LightCone { get; set; }

        [JsonProperty("relics")]
        public List<Relic> Relics { get; set; }

        [JsonProperty("relic_sets")]
        public List<RelicSet> RelicSets { get; set; }

        [JsonProperty("attributes")]
        public List<Attribute> Attributes { get; set; }

        [JsonProperty("additions")]
        public List<Attribute> Additions { get; set; }

        [JsonProperty("properties")]
        public List<Property> Properties { get; set; }
    }

    public class Element
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }
    }

    public class LightCone
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("rarity")]
        public int Rarity { get; set; }

        [JsonProperty("rank")]
        public int Rank { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("promotion")]
        public int Promotion { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("preview")]
        public string Preview { get; set; }

        [JsonProperty("portrait")]
        public string Portrait { get; set; }

        [JsonProperty("path")]
        public Path Path { get; set; }

        [JsonProperty("attributes")]
        public List<Attribute> Attributes { get; set; }

        [JsonProperty("properties")]
        public List<Property> Properties { get; set; }
    }

    public class MainAffix
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("field")]
        public string Field { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("display")]
        public string Display { get; set; }

        [JsonProperty("percent")]
        public bool Percent { get; set; }
    }

    public class Path
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }
    }

    public class Player
    {
        [JsonProperty("uid")]
        public string Uid { get; set; }

        [JsonProperty("nickname")]
        public string Nickname { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("world_level")]
        public int WorldLevel { get; set; }

        [JsonProperty("friend_count")]
        public int FriendCount { get; set; }

        [JsonProperty("avatar")]
        public Avatar Avatar { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }

        [JsonProperty("is_display")]
        public bool IsDisplay { get; set; }

        [JsonProperty("space_info")]
        public SpaceInfo SpaceInfo { get; set; }
    }

    public class Property
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("field")]
        public string Field { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("value")]
        public double? Value { get; set; }

        [JsonProperty("display")]
        public string Display { get; set; }

        [JsonProperty("percent")]
        public bool Percent { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("step")]
        public int Step { get; set; }
    }

    public class Relic
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("set_id")]
        public string SetId { get; set; }

        [JsonProperty("set_name")]
        public string SetName { get; set; }

        [JsonProperty("rarity")]
        public int Rarity { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("main_affix")]
        public MainAffix MainAffix { get; set; }

        [JsonProperty("sub_affix")]
        public List<SubAffix> SubAffix { get; set; }
    }

    public class RelicSet
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("num")]
        public int Num { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }

        [JsonProperty("properties")]
        public List<Property> Properties { get; set; }
    }

    public class Skill
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("max_level")]
        public int MaxLevel { get; set; }

        [JsonProperty("element")]
        public Element Element { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("type_text")]
        public string TypeText { get; set; }

        [JsonProperty("effect")]
        public string Effect { get; set; }

        [JsonProperty("effect_text")]
        public string EffectText { get; set; }

        [JsonProperty("simple_desc")]
        public string SimpleDesc { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }
    }

    public class SkillTree
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("anchor")]
        public string Anchor { get; set; }

        [JsonProperty("max_level")]
        public int MaxLevel { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("parent")]
        public string Parent { get; set; }
    }

    public class SpaceInfo
    {
        [JsonProperty("challenge_data")]
        public ChallengeData ChallengeData { get; set; }

        [JsonProperty("pass_area_progress")]
        public int PassAreaProgress { get; set; }

        [JsonProperty("light_cone_count")]
        public int LightConeCount { get; set; }

        [JsonProperty("avatar_count")]
        public int AvatarCount { get; set; }

        [JsonProperty("achievement_count")]
        public int? AchievementCount { get; set; }
    }

    public class SubAffix
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("field")]
        public string Field { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("display")]
        public string Display { get; set; }

        [JsonProperty("percent")]
        public bool Percent { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("step")]
        public int Step { get; set; }
    }


}
