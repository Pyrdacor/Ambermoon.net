using Ambermoon.Mod.Tests;

var command = args.Length == 0 ? "all" : args[0];
return Tests.Run(command, args.Skip(1).ToArray());
