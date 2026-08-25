using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.Affiliates;

public sealed record ValidatedWithdrawalProof(string FileName, string ContentType, string Sha256, byte[] Content);

public class WithdrawalProofValidator : ITransientDependency
{
    public async Task<ValidatedWithdrawalProof> ReadAsync(Stream stream, string fileName, string suppliedContentType,
        long suppliedLength, CancellationToken cancellationToken)
    {
        if (stream is null || !stream.CanRead || suppliedLength <= 0 ||
            suppliedLength > WebHoanTienConsts.MaximumWithdrawalProofSize)
            throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalProofInvalid);

        await using var memory = new MemoryStream((int)suppliedLength);
        await stream.CopyToAsync(memory, cancellationToken);
        if (memory.Length <= 0 || memory.Length > WebHoanTienConsts.MaximumWithdrawalProofSize)
            throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalProofInvalid);

        var content = memory.ToArray();
        var detectedContentType = DetectContentType(content);
        if (detectedContentType is null || !string.Equals(detectedContentType, suppliedContentType?.Trim(),
                StringComparison.OrdinalIgnoreCase))
            throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalProofInvalid);

        var safeFileName = Path.GetFileName(fileName?.Trim());
        if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName.Length > 255)
            throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalProofInvalid);

        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
        var validExtension = detectedContentType switch
        {
            "image/jpeg" => extension is ".jpg" or ".jpeg",
            "image/png" => extension == ".png",
            "image/webp" => extension == ".webp",
            _ => false
        };
        if (!validExtension) throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalProofInvalid);

        var hash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        return new ValidatedWithdrawalProof(safeFileName, detectedContentType, hash, content);
    }

    private static string? DetectContentType(byte[] content)
    {
        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
            return "image/jpeg";
        if (content.Length >= 8 && content.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return "image/png";
        if (content.Length >= 12 && content.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            content.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            return "image/webp";
        return null;
    }
}
