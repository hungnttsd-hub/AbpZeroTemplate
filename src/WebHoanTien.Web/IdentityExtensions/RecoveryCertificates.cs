using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;

namespace WebHoanTien.Web.IdentityExtensions;

public sealed class RecoveryCertificates : IDisposable
{
    public X509Certificate2? Current => All.FirstOrDefault();
    public X509Certificate2[] All { get; }

    public RecoveryCertificates(IConfiguration configuration)
    {
        var loaded = new List<X509Certificate2>();
        try
        {
            var section = configuration.GetSection("DataProtection");
            var current = Load(section);
            if (current != null) loaded.Add(current);
            foreach (var previous in section.GetSection("PreviousCertificates").GetChildren())
                loaded.Add(Load(previous) ?? throw new InvalidOperationException("Previous recovery certificate has no configured source."));
            foreach (var thumbprint in (section["PreviousCertificateThumbprints"] ?? "")
                         .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                loaded.Add(RecoveryCodeProtector.FindCertificate(thumbprint));
            if (current == null && loaded.Count > 0)
                throw new InvalidOperationException("Configure a current DataProtection certificate before previous certificates.");
            foreach (var certificate in loaded)
            {
                using var rsa = certificate.GetRSAPrivateKey();
                if (rsa == null)
                    throw new InvalidOperationException("DataProtection requires an RSA certificate with an accessible private key.");
                var probe = RandomNumberGenerator.GetBytes(32);
                if (!rsa.Decrypt(rsa.Encrypt(probe, RSAEncryptionPadding.OaepSHA256), RSAEncryptionPadding.OaepSHA256).SequenceEqual(probe))
                    throw new CryptographicException("Recovery certificate encryption check failed.");
            }
            All = loaded.ToArray();
        }
        catch
        {
            foreach (var certificate in loaded) certificate.Dispose();
            throw;
        }
    }

    private static X509Certificate2? Load(IConfiguration section)
    {
        var path = section["CertificatePath"];
        var base64Path = section["CertificateBase64Path"];
        var thumbprint = section["CertificateThumbprint"]?.Trim();
        if (!string.IsNullOrWhiteSpace(path) && !string.IsNullOrWhiteSpace(base64Path))
            throw new InvalidOperationException("Configure only one of CertificatePath and CertificateBase64Path.");
        if (string.IsNullOrWhiteSpace(path) && string.IsNullOrWhiteSpace(base64Path))
            return string.IsNullOrWhiteSpace(thumbprint) ? null : RecoveryCodeProtector.FindCertificate(thumbprint);

        byte[]? bytes = null;
        X509Certificate2? certificate = null;
        try
        {
            bytes = !string.IsNullOrWhiteSpace(base64Path)
                ? Convert.FromBase64String(File.ReadAllText(base64Path))
                : File.ReadAllBytes(path!);
            certificate = new X509Certificate2(bytes, section["CertificatePassword"], X509KeyStorageFlags.EphemeralKeySet);
            if (!string.IsNullOrWhiteSpace(thumbprint) &&
                !string.Equals(certificate.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("DataProtection certificate thumbprint does not match the configured file.");
            return certificate;
        }
        catch
        {
            certificate?.Dispose();
            throw new InvalidOperationException("Cannot load DataProtection PFX. Check the secret file, its encoding, password and optional thumbprint.");
        }
        finally
        {
            if (bytes != null) CryptographicOperations.ZeroMemory(bytes);
        }
    }

    public X509Certificate2 Find(string thumbprint) =>
        All.FirstOrDefault(c => string.Equals(c.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase))
        ?? throw new CryptographicException("Recovery certificate is not configured.");

    public void Dispose()
    {
        foreach (var certificate in All) certificate.Dispose();
    }
}
