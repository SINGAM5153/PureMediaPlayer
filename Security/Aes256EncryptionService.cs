using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PureMediaPlayer.Security;

public sealed class Aes256EncryptionService
{
    private const int SaltByteLength = 32;
    private const int NonceByteLength = 12;
    private const int AuthTagByteLength = 16;
    private const int KeyBitSize = 256;
    private const int Pbkdf2IterationCount = 100_000;
    private const uint FileFormatMagic = 0x504D5045; // "PMPE"

    public async Task EncryptFileAsync(string sourcePath, string destinationPath, string passphrase, CancellationToken ct = default)
    {
        byte[] rawPayload = await File.ReadAllBytesAsync(sourcePath, ct);
        byte[] encryptedPayload = SealPayload(rawPayload, passphrase);
        await File.WriteAllBytesAsync(destinationPath, encryptedPayload, ct);
    }

    public async Task<byte[]> DecryptFileToMemoryAsync(string encryptedFilePath, string passphrase, CancellationToken ct = default)
    {
        byte[] cipherPackage = await File.ReadAllBytesAsync(encryptedFilePath, ct);
        return UnsealPayload(cipherPackage, passphrase);
    }

    public byte[] SealPayload(ReadOnlySpan<byte> plainBytes, string passphrase)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltByteLength);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceByteLength);
        byte[] authTag = new byte[AuthTagByteLength];
        byte[] derivedKey = Derive256BitKey(passphrase, salt);

        int headerSize = sizeof(uint) + SaltByteLength + NonceByteLength + AuthTagByteLength;
        byte[] outputBuffer = new byte[headerSize + plainBytes.Length];
        Span<byte> outputSpan = outputBuffer.AsSpan();

        BinaryPrimitives.WriteUInt32LittleEndian(outputSpan[..sizeof(uint)], FileFormatMagic);
        salt.CopyTo(outputSpan.Slice(sizeof(uint), SaltByteLength));
        nonce.CopyTo(outputSpan.Slice(sizeof(uint) + SaltByteLength, NonceByteLength));

        Span<byte> cipherDestination = outputSpan.Slice(headerSize, plainBytes.Length);

        using var aesGcm = new AesGcm(derivedKey, AuthTagByteLength);
        aesGcm.Encrypt(nonce, plainBytes, cipherDestination, authTag);

        authTag.CopyTo(outputSpan.Slice(sizeof(uint) + SaltByteLength + NonceByteLength, AuthTagByteLength));
        CryptographicOperations.ZeroMemory(derivedKey);

        return outputBuffer;
    }

    public byte[] UnsealPayload(ReadOnlySpan<byte> packagedBytes, string passphrase)
    {
        int headerSize = sizeof(uint) + SaltByteLength + NonceByteLength + AuthTagByteLength;
        if (packagedBytes.Length < headerSize)
        {
            throw new InvalidDataException("Encrypted stream is truncated or invalid.");
        }

        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(packagedBytes[..sizeof(uint)]);
        if (magic != FileFormatMagic)
        {
            throw new InvalidDataException("Unrecognized encryption header signature.");
        }

        ReadOnlySpan<byte> salt = packagedBytes.Slice(sizeof(uint), SaltByteLength);
        ReadOnlySpan<byte> nonce = packagedBytes.Slice(sizeof(uint) + SaltByteLength, NonceByteLength);
        ReadOnlySpan<byte> authTag = packagedBytes.Slice(sizeof(uint) + SaltByteLength + NonceByteLength, AuthTagByteLength);
        ReadOnlySpan<byte> cipherText = packagedBytes[headerSize..];

        byte[] derivedKey = Derive256BitKey(passphrase, salt);
        byte[] decryptedBytes = new byte[cipherText.Length];

        try
        {
            using var aesGcm = new AesGcm(derivedKey, AuthTagByteLength);
            aesGcm.Decrypt(nonce, cipherText, authTag, decryptedBytes);
            return decryptedBytes;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(derivedKey);
        }
    }

    private static byte[] Derive256BitKey(string passphrase, ReadOnlySpan<byte> salt)
    {
        byte[] utf8Password = Encoding.UTF8.GetBytes(passphrase);
        try
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                utf8Password,
                salt,
                Pbkdf2IterationCount,
                HashAlgorithmName.SHA256,
                KeyBitSize / 8);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(utf8Password);
        }
    }
}