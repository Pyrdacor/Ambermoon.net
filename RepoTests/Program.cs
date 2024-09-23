using System.Drawing;
using Ambermoon.Data;
using Ambermoon.Data.GameDataRepository;
using Ambermoon.Data.GameDataRepository.Enumerations;
using Ambermoon.Data.GameDataRepository.Windows;

//var path = @"C:\Users\Robert\Desktop\ambermoon_advanced_german_1.03_extracted\Amberfiles"; // Advanced
//var path = @"C:\Users\Robert\Desktop\ambermoon_german_1.19_extracted\Amberfiles"; // Original
var path =
	@"D:\Projects\AmberworldsInfo\AmberworldsInfo.Server\data\ambermoon\advanced\english\ambermoon_advanced_english_1.03_extracted\Amberfiles"; // English Advanced
var repo = new GameDataRepository(path);

Console.WriteLine($"Advanced: {repo.Advanced}");
Console.WriteLine($"Version: {repo.Version}");
Console.WriteLine($"Language: {repo.Language}");
Console.WriteLine($"ReleaseDate: {repo.ReleaseDate}");
Console.WriteLine($"Info: {repo.Info}");

var foo = repo.GetDefaultMonsterImagePalettes();

Console.WriteLine($"Foo Count: {foo.Count}");

var mgfx = repo.MonsterImages;
var monsters = repo.Monsters;

Console.WriteLine($"Monster Count: {monsters.Count}");

foreach (var monster in monsters)
{
    if (!foo.ContainsKey(monster.Index))
        Console.WriteLine($"Missing palette for monster {monster.Name} ({monster.Index}) with combat index {monster.GraphicIndex}");
}

foreach (var p in foo)
{
    var mon = monsters[p.Key];
    var image = mgfx[mon.GraphicIndex];
    var bmp = image.Frames[0].ToBitmap(p.Value[0], true);
    bmp.Save(@$"C:\Users\Robert\Desktop\MonsterGfx\{p.Key:000}.png");
}
return;

var itemTexts = repo.ItemTexts;

var images = repo.MonsterCombatIcons;

var black = Color.FromArgb(0, 0, 0);
var colors = repo.UserInterfacePalette.ToColors();

var bandit = repo.Monsters.FirstOrDefault(monster => monster.Name == "BANDIT");
var combatBackground = repo.CombatBackgroundImages3D[9]; // Town background
var paletteIndex = combatBackground.GetPaletteIndex(CombatBackgroundDaytime.Day); // Get day palette for this background
var palette = repo.Palettes[paletteIndex];

if (bandit is not null)
{
    var banditImage = repo.MonsterImages[bandit.GraphicIndex];

    string outPath = @"C:\Users\Robert\Desktop\MonsterGfx\Bandit";
    Directory.CreateDirectory(outPath);
    int frameIndex = 0;

    foreach (var frame in banditImage.Frames)
    {
        var bitmap = frame.ToBitmap(palette, true);
        bitmap.Save(Path.Combine(outPath, $"{frameIndex++:000}.png"));
    }
}

string ppath = @"C:\Users\Robert\Desktop\Portraits";
Directory.CreateDirectory(ppath);

foreach (var portrait in repo.Portraits)
{
    var bitmap = portrait.Frames[0].ToBitmap(repo.PortraitPalette, true);
    bitmap.Save(Path.Combine(ppath, $"{portrait.Index:000}.png"));
}

string ipath = @"C:\Users\Robert\Desktop\Items";
Directory.CreateDirectory(ipath);

foreach (var item in repo.ItemImages)
{
    var bitmap = item.Frames[0].ToBitmap(repo.ItemPalette, true);
    bitmap.Save(Path.Combine(ipath, $"{item.Index:000}.png"));
}