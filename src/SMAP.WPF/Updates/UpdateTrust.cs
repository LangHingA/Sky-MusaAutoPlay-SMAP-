namespace SMAP_WPF.Updates;

public static class UpdateTrust
{
    // Only public keys are compiled in. Stable credentials must be provisioned by the publisher.
    public static readonly UpdateKey[] Keys = Load();
    public static bool StableConfigured => Keys.Any(k => k.Channel == "stable");
    static UpdateKey[] Load()
    {
        var keys = new List<UpdateKey> { new("alpha-20261114", "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE4825SpqT7CpLBYRJeh4R5zXT6Y3nBH/0cTpDrC3Pxch5CDdgkkhMipccWifp8zuEfyyRkVruhvM4MLerMnLD3w==", "alpha") };
        var metadata = typeof(UpdateTrust).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyMetadataAttribute), false)
            .OfType<System.Reflection.AssemblyMetadataAttribute>().ToDictionary(x => x.Key, x => x.Value);
        if (metadata.TryGetValue("SMAP.StableUpdatePublicKey", out var key) && metadata.TryGetValue("SMAP.StableUpdateKeyId", out var id))
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 80 || id == keys[0].Id || key == keys[0].PublicKey) throw new InvalidOperationException("Stable and Alpha trust identities must be distinct.");
            using var verifier = System.Security.Cryptography.ECDsa.Create(); byte[] publicKey = Convert.FromBase64String(key!);
            verifier.ImportSubjectPublicKeyInfo(publicKey, out int used);
            if (used != publicKey.Length || verifier.KeySize != 256) throw new InvalidOperationException("A P-256 public key is required.");
            keys.Add(new(id, key!, "stable"));
        }
        return keys.ToArray();
    }
}
