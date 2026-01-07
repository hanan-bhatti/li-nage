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
        /// <param name="onYes">Action to run on Yes</param>
        /// <param name="onNo">Action to run on No</param>
        void PromptYesNoCancel(string title, string message, Action onYes, Action onNo = null);

        /// <summary>
        /// Shows a Yes/No dialog.
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Dialog message</param>
        /// <param name="onYes">Action to run on Yes</param>
        /// <param name="onNo">Action to run on No</param>
        void PromptYesNo(string title, string message, Action onYes, Action onNo = null);

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

        /// <summary>
        /// Shows a success message.
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Success message</param>
        void ShowSuccess(string title, string message);
    }

    /// <summary>
    /// Production implementation of IDialogService using NotificationManager.
    /// </summary>
    public class DialogService : IDialogService
    {
        public string PromptForInput(string title, string prompt, string defaultValue = "")
        {
            using (var dialog = new Linage.GUI.Dialogs.ModernInputDialog(title, prompt, defaultValue))
            {
                return dialog.ShowDialog() == DialogResult.OK ? dialog.InputValue : "";
            }
        }

        public void PromptYesNoCancel(string title, string message, Action onYes, Action onNo = null)
        {
             // For now mapping YesNoCancel to simple Confirmation (Yes/No)
             // or could use custom actions for Cancel?
             // Since NotificationManager.ShowConfirmation supports Yes/No, we'll map to that.
             // If strict 3-way is needed, we'd need a ShowYesNoCancel in Manager.
             Linage.Infrastructure.Services.NotificationManager.Instance.ShowConfirmation(title, message, onYes, onNo);
        }

        public void PromptYesNo(string title, string message, Action onYes, Action onNo = null)
        {
            Linage.Infrastructure.Services.NotificationManager.Instance.ShowConfirmation(title, message, onYes, onNo);
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
            Linage.Infrastructure.Services.NotificationManager.Instance.ShowError(title, message);
        }

        public void ShowInfo(string title, string message)
        {
            Linage.Infrastructure.Services.NotificationManager.Instance.Show(title, message, Linage.Core.Notifications.NotificationSeverity.Info);
        }

        public void ShowWarning(string title, string message)
        {
            Linage.Infrastructure.Services.NotificationManager.Instance.ShowWarning(title, message);
        }

        public void ShowSuccess(string title, string message)
        {
            Linage.Infrastructure.Services.NotificationManager.Instance.ShowSuccess(title, message);
        }
    }
}
