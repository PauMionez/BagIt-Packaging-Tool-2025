using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DevExpress.Mvvm;
using Python.Runtime;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace BagIt_Packaging_Tool_2025.MVVM.ViewModels
{
    class MainViewModel : Abtracts.ViewModelBase
    {
        public AsyncCommand TestCommand { get; private set; }
        public DelegateCommand<DragEventArgs> DropFileCommand { get; set; }

        public MainViewModel()
        {
            TestCommand = new AsyncCommand(TextEvent);
            DropFileCommand = new DelegateCommand<DragEventArgs>(OnDropFile);
            ArchiveOptions = new ObservableCollection<ArchiveType>(Enum.GetValues(typeof(ArchiveType)).Cast<ArchiveType>());
            IsOnProcess = false;

        }

        #region Properties

        private string _fileFullPath;

        public string FileFullPath
        {
            get { return _fileFullPath; }
            set { _fileFullPath = value; RaisePropertyChanged(nameof(FileFullPath)); }
        }

        private List<string> _CropExtensionCollection;

        public List<string> CropExtensionCollection
        {
            get { return _CropExtensionCollection; }
            set { _CropExtensionCollection = value; RaisePropertyChanged(nameof(FileFullPath)); }
        }

        public ObservableCollection<ArchiveType> ArchiveOptions { get; set; }

        private ArchiveType _selectedOption;
        public ArchiveType SelectedOption
        {
            get { return _selectedOption; }
            set { _selectedOption = value; RaisePropertyChanged(nameof(SelectedOption)); }
        }

        private bool _IsOnProcess;

        public bool IsOnProcess
        {
            get { return _IsOnProcess; }
            set { _IsOnProcess = value; RaisePropertiesChanged(nameof(IsOnProcess)); }
        }

        private string _IsProcessMessage;

        public string IsProcessMessage
        {
            get { return _IsProcessMessage; }
            set { _IsProcessMessage = value; RaisePropertiesChanged(nameof(IsProcessMessage)); }
        }


        #endregion

        public enum ArchiveType
        {
            Zip,
            Tar
        }


        private void OnDropFile(DragEventArgs parameter)
        {
            try
            {

                if (parameter.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    // Get the list of dropped file paths
                    string[] filePaths = parameter.Data.GetData(DataFormats.FileDrop) as string[];


                    if (filePaths != null && filePaths.Length > 0)
                    {
                        var filePath = filePaths[0];

                        FileFullPath = filePath;
                    }

                }
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }

        }

        private async Task TextEvent()
        {
            try
            {
                if (string.IsNullOrEmpty(FileFullPath))
                {
                    WarningMessage("Please drag a file to process.");
                    return;
                }

                // Define source and output directory
                string filePath = FileFullPath;
                string fileName = Path.GetFileName(filePath);
                string outputPath = Path.Combine(Path.GetDirectoryName(filePath), "Output BagIt");

                IsOnProcess = true;
                IsProcessMessage = "Processing...";
                await Task.Delay(50);

                string pythonHome = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Python");
                string pythonDll = Path.Combine(pythonHome, "python38.dll");
                Environment.SetEnvironmentVariable("PYTHONHOME", pythonHome);
                Environment.SetEnvironmentVariable("PYTHONPATH", pythonHome);

                Runtime.PythonDLL = pythonDll;

                await Task.Run(() =>
                {

                    PythonEngine.Initialize();

                    using (Py.GIL())
                    {

                        //dynamic sys = Py.Import("sys");
                        //MessageBox.Show("Python version: " + sys.version + "\n\n" + sys.path);

                        // Get the current working directory
                        if (!Directory.Exists(outputPath))
                        { Directory.CreateDirectory(outputPath); }


                        // Copy files and preserve folder structure //This prevents BgaIt the Original file and lost the original file
                        var insertDataFiles = Directory.GetFiles(filePath, "*.*", SearchOption.AllDirectories);
                        foreach (string file in insertDataFiles)
                        {
                            string relativePath = GetRelativePath(filePath, file);
                            string destPath = Path.Combine(outputPath, relativePath);
                            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                            File.Copy(file, destPath, true);
                        }

                        //// Load custom bagit.py from PythonLibs
                        IsProcessMessage = "Creating BagIt...";
                        dynamic bagit = Py.Import("bagit");
                        bagit.make_bag(outputPath, checksums: new[] { "md5" });


                        //bagit.make_bag($@"C:\\Users\\User\\Downloads\\d_62633.IDX.004_I4169141", checksums: new[] { "md5" }); // Assuming you defined that function
                        //bagit.make_bag($@"{FileFullPath}", checksums: new[] { "md5" }); // Assuming you defined that function

                        //C:\Users\User\Downloads\Pos Offline

                    }

                    PythonEngine.Shutdown();
                });

                // Create serialization
                IsProcessMessage = $"Creating {SelectedOption} archive...";
                await ArchiveAndCopyToTemp(outputPath, fileName, filePath, SelectedOption);

                // Remove bagit folder
                if (Directory.Exists(outputPath))
                { Directory.Delete(outputPath, true); }

                IsOnProcess = false;
                IsProcessMessage = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(Title + " " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Creates a zip or tar archive of the specified folder and copies it to the specified output path.
        /// Use SharpCompress.
        /// </summary>
        /// <param name="BagItFolder"></param>
        /// <param name="filename"></param>
        /// <param name="outputPath"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private async Task ArchiveAndCopyToTemp(string BagItFolder, string filename, string outputPath, ArchiveType type)
        {
            try
            {
                // Generate a timestamp-based filename with the correct extension (.zip or .tar)
                //string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string extension = type == ArchiveType.Zip ? "zip" : "tar";
                string archiveFileName = $"{filename}.{extension}";
                string archiveFilePath = Path.Combine(outputPath, archiveFileName);

                await Task.Run(() =>
                {
                    if (type == ArchiveType.Zip)
                    {
                        using (var archive = ZipArchive.Create())
                        {
                            archive.AddAllFromDirectory(BagItFolder);
                            archive.SaveTo(archiveFilePath, SharpCompress.Common.CompressionType.Deflate);
                        }
                    }
                    else if (type == ArchiveType.Tar)
                    {
                        using (var archive = TarArchive.Create())
                        {
                            archive.AddAllFromDirectory(BagItFolder);
                            archive.SaveTo(archiveFilePath, SharpCompress.Common.CompressionType.None);
                        }
                    }
                });

            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }

        }

        /// <summary>
        /// upload.757.dlconsulting.com.
        /// "/upload.757.dlconsulting.com/fromMEI/ATLA",
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private async Task UploadAWSS3(string filePath)
        {
            try
            {
                var s3Config = new AmazonS3Config
                {
                    ServiceURL = "https://upload.757.dlconsulting.com",
                    ForcePathStyle = true // Often required for non-AWS endpoints
                };

                var s3Client = new AmazonS3Client("YOUR_ACCESS_KEY", "YOUR_SECRET_KEY", s3Config);

                var transferUtility = new TransferUtility(s3Client);

                await transferUtility.UploadAsync("path/to/bagit-package.zip", "your-target-bucket", "optional/key/bagit-package.zip");


                //// Upload BagIt package to expected path
                //await transferUtility.UploadAsync(
                //    "C:\\packages\\issue123.zip",              // BagIt package path
                //    "fromMEI",                                 // Likely bucket name
                //    "ATLA/issue123.zip"                        // Key inside the bucket
                //);
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }
        }

        #region dump
        //private async Task<string> ArchiveAndCopyToTemp(string sourceFolder, string filename, string outputPath, ArchiveType type)
        //{
        //    // Generate a timestamp-based filename with the correct extension (.zip or .tar)
        //    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        //    string extension = type == ArchiveType.Zip ? "zip" : "tar";
        //    string archiveFileName = $"{filename}_{timestamp}.{extension}";
        //    string archiveFilePath = Path.Combine(outputPath, archiveFileName);

        //    try
        //    {
        //        await Task.Run(() =>
        //        {
        //            // Get all files in the source folder
        //            var files = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);

        //            // ZIP archive 
        //            if (type == ArchiveType.Zip)
        //            {

        //                using (var zipStream = new ZipOutputStream(File.Create(archiveFilePath)))
        //                {
        //                    // Maximum compression level
        //                    zipStream.SetLevel(9);

        //                    foreach (var filePath in files)
        //                    {
        //                        // Use the file name as the ZIP entry name
        //                        string entryName = Path.GetFileName(filePath);
        //                        var entry = new ZipEntry(entryName) 
        //                        { 
        //                            DateTime = DateTime.Now 
        //                        };


        //                        zipStream.PutNextEntry(entry);

        //                        // Read file data and write to the ZIP archive
        //                        byte[] buffer = File.ReadAllBytes(filePath);
        //                        zipStream.Write(buffer, 0, buffer.Length);
        //                        zipStream.CloseEntry();
        //                    }
        //                    // Finalize the ZIP file
        //                    zipStream.Finish();
        //                }
        //            }
        //            // TAR archive
        //            else if (type == ArchiveType.Tar)
        //            {

        //                using (var tarStream = new TarOutputStream(File.Create(archiveFilePath)))
        //                {


        //                    foreach (var filePath in files)
        //                    {
        //                        var entry = TarEntry.CreateEntryFromFile(filePath);

        //                        tarStream.PutNextEntry(entry);


        //                        byte[] buffer = File.ReadAllBytes(filePath);
        //                        tarStream.Write(buffer, 0, buffer.Length);
        //                        tarStream.CloseEntry();
        //                    }

        //                    tarStream.Finish();
        //                }

        //                //    using (var outStream = File.Create(archiveFilePath))
        //                //using (var tarArchive = TarArchive.CreateOutputTarArchive(outStream))
        //                //{
        //                //    foreach (var filePath in files)
        //                //    {
        //                //        // Create a TAR entry from the file and add it to the archive
        //                //        var entry = TarEntry.CreateEntryFromFile(filePath);
        //                //        tarArchive.WriteEntry(entry, true);
        //                //    }
        //                //}
        //            }
        //        });

        //        return archiveFilePath;
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorMessage(ex);
        //        return string.Empty;
        //    }
        //}


        ///// <summary>
        ///// Zips all image files and copies the zip to TRASH_DIR.
        ///// </summary>
        //private async Task<string> ZipAndCopyToTemp(string sourceFolder, string outputpath)
        //{
        //    string zipFileName = $"images_{DateTime.Now:yyyyMMddHHmmss}.zip";
        //    string zipFilePath = Path.Combine(outputpath, zipFileName);

        //    try
        //    {
        //        await Task.Run(() =>
        //        {
        //            using (FileStream zipToOpen = new FileStream(zipFilePath, FileMode.Create))
        //            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
        //            {

        //                var files = Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories);

        //                foreach (var filePath in files)
        //                {
        //                    string entryName = Path.GetFileName(filePath);
        //                    archive.CreateEntryFromFile(filePath, entryName);

        //                }
        //            }
        //        });

        //        return zipFilePath;
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorMessage(ex);
        //        return string.Empty;
        //    }
        //}
        #endregion
    }
}