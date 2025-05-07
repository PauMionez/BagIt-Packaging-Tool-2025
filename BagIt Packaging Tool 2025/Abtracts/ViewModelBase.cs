using DevExpress.Mvvm;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace BagIt_Packaging_Tool_2025.Abtracts
{
    abstract class ViewModelBase : BindableBase
    {
        private string ApplicationName => "BagIt Packaging Tool 2025";
        private Version Version => new Version(1, 0);

        public string Title => $"{ApplicationName} v{Version}";


        public ViewModelBase()
        {
        }

        #region Dialog Functions
        /// <summary>
        /// Display an error message
        /// </summary>
        /// <param name="ex">Your exception</param>
        /// <param name="message">Title text. Typically, put here your method name</param>
        public static void ErrorMessage(Exception ex)
        {
            try
            {
                StackTrace stackTrace = new StackTrace(ex);
                System.Reflection.MethodBase method = stackTrace.GetFrame(stackTrace.FrameCount - 1).GetMethod();
                string titleText = method.Name;

                MessageBox.Show(string.Format(ex.Message + "\n\n" + ex.StackTrace + "\n\n{0}", "Please screenshot and send procedures on how this error occured. Thank you."), titleText, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex2)
            {
                ErrorMessage(ex2);
            }
        }

        /// <summary>
        /// Displays an information messagebox
        /// </summary>
        /// <param name="message">Text</param>
        /// <param name="title">Title text</param>
        public static void InformationMessage(string message, string title)
        {
            try
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex2)
            {
                ErrorMessage(ex2);
            }
        }

        /// <summary>
        /// Displays a warning messagebox
        /// </summary>
        /// <param name="message">Text</param>
        /// <param name="title">Title text</param>
        public static void WarningMessage(string message, string title)
        {
            try
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex2)
            {
                ErrorMessage(ex2);
            }
        }
        /// <summary>
        /// Displays a warning messagebox
        /// </summary>
        /// <param name="message">Text</param>
        /// <param name="title">Title text</param>
        public static void WarningMessage(string message)
        {
            try
            {
                WarningMessage(message, "Warning");
            }
            catch (Exception ex2)
            {
                ErrorMessage(ex2);
            }
        }

        /// <summary>
        /// Displays a yes/no/cancel messagebox. Returns a MessageBoxResult. Either true or false.
        /// </summary>
        /// <param name="message">Text to ask</param>
        /// <returns>MessageBoxResult result. Either true or false.</returns>
        public static MessageBoxResult YesNoCancelDialog(string message)
        {
            MessageBoxResult res = MessageBoxResult.None;

            try
            {
                res = MessageBox.Show(message, "Confirmation", MessageBoxButton.YesNoCancel, MessageBoxImage.Information);
            }
            catch (Exception ex2)
            {
                ErrorMessage(ex2);
            }

            return res;
        }
        #endregion

        #region Shared functions

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Open the file chooser and return the selected files
        /// </summary>
        /// <returns></returns>
        public List<string> GetFilePaths(string DisplayName, string ExtensionList, string Title)
        {
            List<string> pathList = null;

            try
            {
                Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
                {
                    Title = Title,
                    Multiselect = true
                };

                dialog.Filters.Add(new Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogFilter(DisplayName, ExtensionList));

                if (dialog.ShowDialog() != Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
                {
                    return null;
                }

                pathList = dialog.FileNames.ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }

            return pathList;
        }
        /// <summary>
        /// Open the file chooser and return the selected file or folder
        /// </summary>
        /// <returns></returns>
        public string GetFilePath(string DisplayName, string ExtensionList, string Title)
        {
            string path = null;

            try
            {
                Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
                {
                    Title = Title,
                };

                dialog.Filters.Add(new Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogFilter(DisplayName, ExtensionList));

                if (dialog.ShowDialog() != Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
                {
                    return null;
                }

                path = dialog.FileName;
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }

            return path;
        }
        /// <summary>
        /// Open the folder chooser and return the selected paths
        /// </summary>
        /// <param name="IsMultiSelect">Default is false. True = Select 2 or more files</param>
        /// <returns></returns>
        public IEnumerable<string> GetFolderPaths(string Title)
        {
            IEnumerable<string> path = null;

            try
            {
                CommonOpenFileDialog folderSelectorDialog = new CommonOpenFileDialog
                {
                    IsFolderPicker = true,
                    AllowNonFileSystemItems = false,
                    Multiselect = true,
                    Title = Title
                };

                if (folderSelectorDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    path = folderSelectorDialog.FileNames;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }

            return path;
        }

        /// <summary>
        /// Open the folder chooser and return the path
        /// </summary>
        /// <param name="IsMultiSelect">Default is false. True = Select 2 or more files</param>
        /// <returns></returns>
        public string GetFolderPath(string Title, bool isMultiSelect = false)
        {
            string path = null;

            try
            {
                CommonOpenFileDialog folderSelectorDialog = new CommonOpenFileDialog
                {
                    IsFolderPicker = true,
                    AllowNonFileSystemItems = false,
                    Multiselect = isMultiSelect,
                    Title = Title
                };

                if (folderSelectorDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    path = folderSelectorDialog.FileName;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }

            return path;
        }
        /// <summary>
        /// Get the column
        /// </summary>
        /// <param name="columnNumber"></param>
        /// <returns></returns>
        public string GetExcelColumnName(int columnNumber)
        {
            int dividend = columnNumber;
            string columnName = string.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }

        /// <summary>
        /// Open the documentation file
        /// </summary>
        public void ShowDocumentation()
        {
            try
            {
                Process.Start(@"Assets\TRB Image Manager 2023.docx");
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }
        }


        /// <summary>
        /// Validate if the filepath is being used
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool IsFileBeingUsed(string filePath)
        {
            bool result = false;

            try
            {
                // Attempt to open the file with specific FileShare options
                using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    result = false;
                }
            }
            catch (IOException)
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Returns the relative path from a base directory to a full file or directory path.
        /// Ensures consistent formatting using system directory separators.
        /// </summary>
        /// <param name="basePath">The base directory path.</param>
        /// <param name="fullPath">The target full file or directory path.</param>
        /// <returns>Relative path from basePath to fullPath.</returns>
        public string GetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new Uri(AppendDirectorySeparatorChar(basePath));
            Uri fullUri = new Uri(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Ensures the specified path ends with a directory separator character.
        /// Helps distinguish directories from files in URI-based operations.
        /// </summary>
        /// <param name="path">The input path.</param>
        /// <returns>The path with a trailing directory separator, if not already present.</returns>
        public string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;
            return path;
        }


        #endregion
    }
}
