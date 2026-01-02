using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Linage.GUI
{
    /// <summary>
    /// A modern folder browser dialog using native Windows IFileOpenDialog.
    /// Falls back to standard FolderBrowserDialog on error.
    /// </summary>
    public class ModernFolderBrowserDialog
    {
        public string SelectedPath { get; private set; }
        public string Title { get; set; } = "Select Folder";

        public DialogResult ShowDialog(IWin32Window owner = null)
        {
            try
            {
                return ShowModernDialog(owner);
            }
            catch (Exception)
            {
                // Fallback to standard dialog on any error
                return ShowFallbackDialog(owner);
            }
        }

        private DialogResult ShowModernDialog(IWin32Window owner)
        {
            IntPtr hwndOwner = owner?.Handle ?? IntPtr.Zero;
            IFileOpenDialog dialog = null;

            try
            {
                dialog = (IFileOpenDialog)new FileOpenDialog();

                // Set options to pick folders
                uint options;
                dialog.GetOptions(out options);
                dialog.SetOptions(options | FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM);

                if (!string.IsNullOrEmpty(Title))
                {
                    dialog.SetTitle(Title);
                }

                int hr = dialog.Show(hwndOwner);
                
                // User cancelled
                if (hr != 0)
                    return DialogResult.Cancel;

                // Get result
                IShellItem item = null;
                try
                {
                    dialog.GetResult(out item);
                    
                    if (item == null)
                        return DialogResult.Cancel;

                    IntPtr pathPtr = IntPtr.Zero;
                    try
                    {
                        item.GetDisplayName(SIGDN_FILESYSPATH, out pathPtr);
                        
                        if (pathPtr == IntPtr.Zero)
                            return DialogResult.Cancel;

                        SelectedPath = Marshal.PtrToStringAuto(pathPtr);
                        return DialogResult.OK;
                    }
                    finally
                    {
                        if (pathPtr != IntPtr.Zero)
                            Marshal.FreeCoTaskMem(pathPtr);
                    }
                }
                finally
                {
                    if (item != null)
                        Marshal.ReleaseComObject(item);
                }
            }
            finally
            {
                if (dialog != null)
                    Marshal.ReleaseComObject(dialog);
            }
        }

        private DialogResult ShowFallbackDialog(IWin32Window owner)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = Title ?? "Select Folder";
                dialog.ShowNewFolderButton = true;
                
                var result = dialog.ShowDialog(owner);
                if (result == DialogResult.OK)
                    SelectedPath = dialog.SelectedPath;
                
                return result;
            }
        }

        // --- COM Interfaces ---

        [ComImport]
        [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
        private class FileOpenDialog { }

        [ComImport]
        [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig] int Show(IntPtr parent);
            void SetFileTypes();
            void SetFileTypeIndex();
            void GetFileTypeIndex();
            void Advise();
            void Unadvise();
            void SetOptions(uint options);
            void GetOptions(out uint options);
            void SetDefaultFolder(IShellItem item);
            void SetFolder(IShellItem item);
            void GetFolder(out IShellItem item);
            void GetCurrentSelection(out IShellItem item);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string label);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);
            void GetResult(out IShellItem item);
            void AddPlace(IShellItem item, int placement);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string extension);
            void Close(int hr);
            void SetClientGuid(ref Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr filter);
            void GetResults(out IntPtr results);
            void GetSelectedItems(out IntPtr items);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler();
            void GetParent();
            void GetDisplayName(uint sigdnName, out IntPtr ppszName);
            void GetAttributes();
            void Compare();
        }

        // Constants
        private const uint FOS_PICKFOLDERS = 0x00000020;
        private const uint FOS_FORCEFILESYSTEM = 0x00000040;
        private const uint SIGDN_FILESYSPATH = 0x80058000;
    }
}
