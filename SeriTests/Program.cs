using Ambermoon.Data.Legacy;
using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Legacy.ExecutableData;
using Ambermoon.Data.Serialization.Json;
using Converter = AmbermoonSerialize.Converter;

var gameData = new GameData();
gameData.Load(@"C:\Users\flavia\Desktop\Amiga\Workbench3.1\Ambermoon\Amberfiles");

var graphicProvider = new GraphicProvider(gameData, ExecutableData.FromGameData(gameData),
    new Ambermoon.Data.Legacy.Serialization.IntroData(gameData),
    new Ambermoon.Data.Legacy.Serialization.OutroData(gameData));

var characterManager = new CharacterManager(gameData, graphicProvider);
var serializerSettings = new JsonSerializerSettings();
serializerSettings.Formatting = Formatting.Indented;
serializerSettings.ContractResolver = new StructuredContractResolver();

foreach (var monster in characterManager.Monsters)
{
    File.WriteAllText($@"C:\Projects\Amb\Seri\{monster.Index:000}_{monster.Name.Replace(' ', '_')}.txt", Converter.Serialize(monster, serializerSettings));
}
foreach (var npc in characterManager.NPCs)
{
    File.WriteAllText($@"C:\Projects\Amb\Seri\{npc.Key:000}_{npc.Value.Name.Replace(' ', '_')}.txt", Converter.Serialize(npc.Value, serializerSettings));
}