using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Ambermoon.Mod;

/// <summary>
/// Remembers which mod assemblies the player has trusted.
/// A mod is identified by its id and the SHA-256 hash of its assembly,
/// so an updated assembly has to be confirmed again.
/// </summary>
public class ModTrustStore(string file)
{
    readonly string file = file;
    Dictionary<string, string>? trustedMods;

    Dictionary<string, string> TrustedMods
    {
        get
        {
            if (trustedMods == null)
            {
                try
                {
                    trustedMods = File.Exists(file)
                        ? JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file))
                        : null;
                }
                catch
                {
                    trustedMods = null;
                }

                trustedMods ??= [];
            }

            return trustedMods;
        }
    }

    public static string ComputeHash(byte[] assemblyData) => Convert.ToHexString(SHA256.HashData(assemblyData));

    public bool IsTrusted(string modId, string assemblyHash)
        => TrustedMods.TryGetValue(modId, out var hash) && string.Equals(hash, assemblyHash, StringComparison.OrdinalIgnoreCase);

    public void Trust(string modId, string assemblyHash)
    {
        TrustedMods[modId] = assemblyHash;

        try
        {
            var directory = Path.GetDirectoryName(file);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(file, JsonConvert.SerializeObject(TrustedMods, Formatting.Indented));
        }
        catch
        {
            // The trust is still valid for this session.
        }
    }
}
