using System.IO.Compression;
using FieldService.Storage.Entities;
using FieldService.Storage.Types;

namespace FieldService.Storage.Utils;

public static class ZipArchiveUtility
{
    private const string DefaultZipName = "download.zip";

    public static async Task<StoredFileCompressedDownloadsResponse> CreateZipArchiveAsync<TFile>(
        IReadOnlyList<(TFile File, string FileName, Stream Content)> filesToZip,
        Func<TFile, StoredFile> fileMapper,
        string? zipFileName = DefaultZipName,
        CompressionLevel compressionLevel = CompressionLevel.Fastest,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filesToZip);
        ArgumentNullException.ThrowIfNull(fileMapper);
        
        var effectiveZipFileName = string.IsNullOrWhiteSpace(zipFileName) 
            ? DefaultZipName 
            : EnsureZipExtension(zipFileName.Trim());

        var zipStream = new MemoryStream();
        var entries = new List<StoredFileZipEntry>();
        var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (fileItem, fileName, contentStream) in filesToZip)
            {
                var uniquePath = ResolveUniqueZipPath(fileName, usedPaths);
                usedPaths.Add(uniquePath);

                var zipEntryPath = new ZipEntryPath(uniquePath);
                var zipEntry = archive.CreateEntry(zipEntryPath.Value, compressionLevel);

                await using (var entryStream = zipEntry.Open())
                {
                    if (contentStream.CanSeek)
                        contentStream.Position = 0;

                    await contentStream.CopyToAsync(entryStream, ct);
                }

                entries.Add(new StoredFileZipEntry(zipEntryPath, fileMapper(fileItem)));
            }
        }

        zipStream.Position = 0;

        return new StoredFileCompressedDownloadsResponse(entries, zipStream, effectiveZipFileName);
    }
    
    public static Task<StoredFileCompressedDownloadsResponse> CreateZipArchiveAsync(
        IReadOnlyList<(StoredFile File, Stream Content)> filesToZip,
        string? zipFileName = DefaultZipName,
        CompressionLevel compressionLevel = CompressionLevel.Fastest,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filesToZip);

        var normalizedInput = filesToZip
            .Select(x => (File: x.File, FileName: x.File.FileName, Content: x.Content))
            .ToList();

        return CreateZipArchiveAsync(
            filesToZip: normalizedInput,
            fileMapper: file => file,
            zipFileName: zipFileName,
            compressionLevel: compressionLevel,
            ct: ct);
    }

    private static string EnsureZipExtension(string fileName)
    {
        return fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) 
            ? fileName 
            : $"{fileName}.zip";
    }

    private static string ResolveUniqueZipPath(string originalFileName, HashSet<string> usedPaths)
    {
        if (!usedPaths.Contains(originalFileName))
            return originalFileName;

        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
        var extension = Path.GetExtension(originalFileName);
        var counter = 1;

        string candidate;
        do
        {
            candidate = $"{fileNameWithoutExt} ({counter}){extension}";
            counter++;
        } while (usedPaths.Contains(candidate));

        return candidate;
    }
}