using EnvironmentBuilder.CommandSupporter;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace FileMaster.FileEngine
{
    public class FileExtractor
    {
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
            // Use the IShellDispatch interface instead of ShellClass to avoid CS1752
            Shell32.IShellDispatch sc = (Shell32.IShellDispatch)new Shell32.Shell();
            Shell32.Folder SrcFlder = sc.NameSpace(sourceFile);
            Shell32.Folder DestFlder = sc.NameSpace(destination);
            Shell32.FolderItems items = SrcFlder.Items();
            DestFlder.CopyHere(items, 20);
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