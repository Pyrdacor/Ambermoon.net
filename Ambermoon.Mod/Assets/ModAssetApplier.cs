using Ambermoon.Data;
using Ambermoon.Mod.Packaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ambermoon.Mod.Assets;

/// <summary>
/// Applies the asset files of a mod to the loaded base game data.
///
/// Supported files (all inside the "assets" folder of a mod):
/// - maps/{index}.json:          Patch of an existing map (e.g. Texts, Flags, MusicIndex) or a complete
///                               map definition (if it contains a "Grid", see <see cref="MapDefinition"/>).
/// - monsters/{index}.json:      Patch of a monster. New monsters need a "Template" (monster index to copy).
/// - monstergroups/{index}.json: { "Monsters": [[6 monster indices], [6], [6]] } (rows from back to front).
/// - items/{index}.json:         Patch of an item. New items need a "Template" (item index to copy).
/// - partymembers/{index}.json:  Patch of an initial party member.
/// - npcs/{index}.json:          Patch of an NPC.
/// - graphics/{type}/{index}.png Replaces a graphic of the given <see cref="GraphicType"/> (0-based index).
///                               The size must match. An optional {index}.json can specify { "Palette": n }.
/// - music/{song}.mp3            Replaces a song (see <see cref="Data.Enumerations.Song"/> for the numbers).
///                               Music is handled by the frontend, see <see cref="GetMusicFiles"/>.
/// </summary>
public class ModAssetApplier(IModSource source, IGameData gameData)
{
    public const string AssetFolder = "assets";
    const string TemplateKey = "Template";

    readonly IModSource source = source;
    readonly IGameData gameData = gameData;

    public List<string> Warnings { get; } = [];

    public void Apply()
    {
        // Order matters: monster groups reference monsters, maps reference monster groups etc.
        ApplyEach("monsters", ApplyMonster);
        ApplyEach("monstergroups", ApplyMonsterGroup);
        ApplyEach("items", ApplyItem);
        ApplyEach("partymembers", ApplyPartyMember);
        ApplyEach("npcs", ApplyNPC);
        ApplyEach("maps", ApplyMap);
        ApplyGraphics();
    }

    /// <summary>
    /// Returns the song number and path of all music files inside the mod.
    /// </summary>
    public static Dictionary<int, string> GetMusicFiles(IModSource source)
    {
        var result = new Dictionary<int, string>();

        foreach (var file in source.ListFiles($"{AssetFolder}/music"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var digits = new string(name.TakeWhile(char.IsAsciiDigit).ToArray());

            if (digits.Length != 0 && int.TryParse(digits, out int song))
                result[song] = file;
        }

        return result;
    }

    void ApplyEach(string folder, Action<uint, JObject> applier)
    {
        foreach (var file in source.ListFiles($"{AssetFolder}/{folder}"))
        {
            if (!file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!uint.TryParse(Path.GetFileNameWithoutExtension(file), out uint index) || index == 0)
            {
                Warnings.Add($"Ignored '{file}': the file name must be a 1-based index.");
                continue;
            }

            JObject json;

            try
            {
                json = JObject.Parse(source.ReadAllText(file));
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Invalid JSON in '{file}': {ex.Message}", ex);
            }

            try
            {
                applier(index, json);
            }
            catch (Exception ex) when (ex is not InvalidDataException)
            {
                throw new InvalidDataException($"Unable to apply '{file}': {ex.Message}", ex);
            }
        }
    }

    static void Populate(object target, JObject json)
    {
        var data = (JObject)json.DeepClone();
        data.Remove(TemplateKey);
        JsonConvert.PopulateObject(data.ToString(), target, ModJson.Settings);
    }

    static uint? GetTemplate(JObject json) => json[TemplateKey]?.Value<uint>();

    T Modifiable<T>(object manager, string what) where T : class
        => manager as T ?? throw new NotSupportedException($"The base game data does not support modifying {what}.");

    void ApplyMonster(uint index, JObject json)
    {
        var characterManager = Modifiable<IModifiableCharacterManager>(gameData.CharacterManager, "monsters");
        var monster = characterManager.GetMonster(index);

        if (monster == null)
        {
            var template = (GetTemplate(json) is uint templateIndex ? characterManager.GetMonster(templateIndex) : null)
                ?? throw new InvalidDataException($"Monster {index} does not exist. Specify an existing '{TemplateKey}' monster to copy.");
            monster = characterManager.CloneMonster(template);
            monster.Index = index;
        }

        Populate(monster, json);
        characterManager.SetMonster(monster);
    }

    class MonsterGroupDefinition
    {
        public uint[][] Monsters { get; set; } = [];
    }

    void ApplyMonsterGroup(uint index, JObject json)
    {
        var characterManager = Modifiable<IModifiableCharacterManager>(gameData.CharacterManager, "monster groups");
        var definition = json.ToObject<MonsterGroupDefinition>(JsonSerializer.Create(ModJson.Settings))!;

        if (definition.Monsters.Length != 3 || definition.Monsters.Any(row => row.Length != 6))
            throw new InvalidDataException($"Monster group {index} must have 3 rows with 6 monster indices each (0 = empty).");

        var group = new MonsterGroup();

        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 6; column++)
            {
                uint monsterIndex = definition.Monsters[row][column];

                if (monsterIndex == 0)
                    continue;

                group.Monsters[column, row] = characterManager.GetMonster(monsterIndex)
                    ?? throw new InvalidDataException($"Monster group {index} references unknown monster {monsterIndex}.");
            }
        }

        if (group.Monsters.Cast<Monster?>().All(m => m == null))
            throw new InvalidDataException($"Monster group {index} is empty.");

        characterManager.SetMonsterGroup(index, group);
    }

    void ApplyItem(uint index, JObject json)
    {
        var itemManager = Modifiable<IModifiableItemManager>(gameData.ItemManager, "items");
        var item = itemManager.Items.FirstOrDefault(i => i.Index == index);

        if (item == null)
        {
            var templateIndex = GetTemplate(json)
                ?? throw new InvalidDataException($"Item {index} does not exist. Specify an existing '{TemplateKey}' item to copy.");
            var template = itemManager.GetItem(templateIndex);
            item = JsonConvert.DeserializeObject<Item>(JsonConvert.SerializeObject(template))!;
            item.Index = index;
        }

        Populate(item, json);
        itemManager.SetItem(item);
    }

    void ApplyPartyMember(uint index, JObject json)
    {
        var characterManager = Modifiable<IModifiableCharacterManager>(gameData.CharacterManager, "party members");
        var partyMember = characterManager.GetInitialPartyMember(index)
            ?? throw new InvalidDataException($"Party member {index} does not exist.");

        Populate(partyMember, json);
    }

    void ApplyNPC(uint index, JObject json)
    {
        var characterManager = Modifiable<IModifiableCharacterManager>(gameData.CharacterManager, "NPCs");
        var npc = characterManager.GetNPC(index)
            ?? throw new InvalidDataException($"NPC {index} does not exist.");

        Populate(npc, json);
    }

    void ApplyMap(uint index, JObject json)
    {
        var mapManager = Modifiable<IModifiableMapManager>(gameData.MapManager, "maps");

        if (index > 1024)
            throw new InvalidDataException($"Map index {index} is too large. Valid map indices are 1 to 1024.");

        if (json.ContainsKey(nameof(MapDefinition.Grid)))
        {
            var definition = json.ToObject<MapDefinition>(JsonSerializer.Create(ModJson.Settings))!;
            var map = definition.CreateMap(index);

            if (map.Type == MapType.Map3D && mapManager.GetLabdataForMapSafe(map) == null)
                throw new InvalidDataException($"Map {index} references unknown labdata {map.TilesetOrLabdataIndex}.");

            mapManager.SetMap(map);
        }
        else
        {
            var map = mapManager.GetMap(index)
                ?? throw new InvalidDataException($"Map {index} does not exist. New maps need a complete definition including a 'Grid'.");

            Populate(map, json);
        }
    }

    void ApplyGraphics()
    {
        if (gameData.GraphicInfoProvider is not IGraphicProvider graphicProvider)
        {
            if (source.ListFiles($"{AssetFolder}/graphics").Any())
                Warnings.Add("The base game data does not support replacing graphics.");

            return;
        }

        foreach (var file in source.ListFiles($"{AssetFolder}/graphics"))
        {
            if (!file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = file.Split('/');

            if (parts.Length != 4 || !Enum.TryParse<GraphicType>(parts[2], true, out var graphicType) ||
                !int.TryParse(Path.GetFileNameWithoutExtension(parts[3]), out int index))
            {
                Warnings.Add($"Ignored '{file}': expected graphics/{{GraphicType}}/{{index}}.png.");
                continue;
            }

            var graphics = graphicProvider.GetGraphics(graphicType);

            if (graphics == null || index < 0 || index >= graphics.Count)
                throw new InvalidDataException($"'{file}': There is no graphic {index} of type {graphicType}.");

            var original = graphics[index];
            var image = PngDecoder.Decode(source.ReadAllBytes(file));

            if (image.Width != original.Width || image.Height != original.Height)
                throw new InvalidDataException($"'{file}': The image must have the size {original.Width}x{original.Height}.");

            int paletteIndex = graphicProvider.PrimaryUIPaletteIndex;
            var settingsFile = Path.ChangeExtension(file, ".json");

            if (source.Exists(settingsFile))
            {
                var settings = JObject.Parse(source.ReadAllText(settingsFile));
                paletteIndex = settings["Palette"]?.Value<int>() ?? paletteIndex;
            }

            if (!graphicProvider.Palettes.TryGetValue(paletteIndex, out var palette))
                throw new InvalidDataException($"'{file}': Unknown palette {paletteIndex}.");

            graphics[index] = GraphicConverter.ToIndexedGraphic(image, palette);
        }
    }
}

internal static class MapManagerExtensions
{
    public static Labdata? GetLabdataForMapSafe(this IMapManager mapManager, Map map)
    {
        try
        {
            return mapManager.GetLabdataForMap(map);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }
}
