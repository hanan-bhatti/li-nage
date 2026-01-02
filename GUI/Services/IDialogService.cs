using System;
using System.Windows.Forms;

namespace Linage.GUI.Services
{
    /// <summary>
    /// Service abstraction for dialog interactions, enabling testability and separation of concerns.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Prompts the user for text input.
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="prompt">Prompt message</param>
        /// <param name="defaultValue">Default value for the input</param>
        /// <returns>User input, or null/empty if cancelled</returns>
        string PromptForInput(string title, string prompt, string defaultValue = "");

        /// <summary>
        /// Shows a Yes/No/Cancel dialog.
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Dialog message</param>
        /// <returns>DialogResult indicating user choice</returns>
        DialogResult PromptYesNoCancel(string title, string message);

        /// <summary>
        /// Shows a Yes/No dialog.
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Dialog message</param>
        /// <returns>DialogResult indicating user choice</returns>
        DialogResult PromptYesNo(string title, string message);

        /// <summary>
        /// Prompts the user to select a folder.
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <returns>Selected folder path, or null if cancelled</returns>
        string PromptForFolder(string title);

        /// <summary>
        /// Shows an error message.
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Error message</param>
        void ShowError(string title, string message);

        /// <summary>
        /// Shows an information message.
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Information message</param>
        void ShowInfo(string title, string message);

        /// <summary>
        /// Shows a warning message.
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Warning message</param>
        void ShowWarning(string title, string message);
    }

    /// <summary>
    /// Production implementation of IDialogService using WinForms dialogs.
    /// </summary>
    public class DialogService : IDialogService
    {
        public string PromptForInput(string title, string prompt, string defaultValue = "")
        {
            return Microsoft.VisualBasic.Interaction.InputBox(prompt, title, defaultValue);
        }

        public DialogResult PromptYesNoCancel(string title, string message)
        {
            return MessageBox.Show(message, title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        }

        public DialogResult PromptYesNo(string title, string message)
        {
            return MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }

        public string PromptForFolder(string title)
        {
            using (var dialog = new FolderBrowserDialog { Description = title })
            {
                return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
            }
        }

        public void ShowError(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void ShowInfo(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void ShowWarning(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
