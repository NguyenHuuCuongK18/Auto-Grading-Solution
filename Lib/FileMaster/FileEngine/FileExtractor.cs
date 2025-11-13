using EnvironmentBuilder.CommandSupporter;
using Ionic.Zip;
using System;
using System.IO;

namespace FileMaster.FileEngine
{
    public class FileExtractor
    {
        // Uses Ionic.Zip to extract zip files. Removes COM Shell32 usage which requires a Windows-only COM reference.
        public static void Unzip(string sourceFile, string destination)
        {
            if (string.IsNullOrEmpty(sourceFile))
                throw new ArgumentNullException("First param in method unzip must not be empty!");

            if (string.IsNullOrEmpty(destination))
                destination = Path.GetDirectoryName(sourceFile);

            using (ZipFile zips = new ZipFile(sourceFile))
            {
                zips.ExtractAll(destination);
            }
        }

        public static void ExtractDestination(string zipPath, string destinationPath)
        {
            if (destinationPath == null)
            {
                destinationPath = Path.GetDirectoryName(zipPath);
            }

            using (ZipFile zips = new ZipFile(zipPath))
            {
                zips.ExtractAll(destinationPath);
            }
            Console.WriteLine($"Extracted {zipPath} to {destinationPath}");
        }
    }
}