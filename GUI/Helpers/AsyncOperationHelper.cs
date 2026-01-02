using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Linage.GUI.Dialogs;

namespace Linage.GUI.Helpers
{
    /// <summary>
    /// Helper class to standardize async operation execution with consistent error handling,
    /// progress indication, and status updates across the application.
    /// </summary>
    public class AsyncOperationHelper
    {
        private readonly Action<bool> _toggleProgress;
        private readonly Action<string> _updateStatus;
        private readonly Action<string, Exception> _showError;
        private readonly IWin32Window _owner;

        public AsyncOperationHelper(
            Action<bool> toggleProgress,
            Action<string> updateStatus,
            Action<string, Exception> showError,
            IWin32Window owner = null)
        {
            _toggleProgress = toggleProgress ?? throw new ArgumentNullException(nameof(toggleProgress));
            _updateStatus = updateStatus ?? throw new ArgumentNullException(nameof(updateStatus));
            _showError = showError ?? throw new ArgumentNullException(nameof(showError));
            _owner = owner;
        }

        /// <summary>
        /// Executes an async operation with standardized error handling and progress indication.
        /// </summary>
        /// <param name="operation">The async operation to execute</param>
        /// <param name="operationName">Name of the operation for status messages</param>
        /// <param name="startMessage">Optional custom start message (defaults to "{operationName} in progress...")</param>
        /// <param name="successMessage">Optional custom success message (defaults to "{operationName} completed successfully")</param>
        public async Task ExecuteAsync(
            Func<Task> operation,
            string operationName,
            string startMessage = null,
            string successMessage = null)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (string.IsNullOrEmpty(operationName)) throw new ArgumentException("Operation name is required", nameof(operationName));

            try
            {
                _toggleProgress(true);
                _updateStatus(startMessage ?? $"{operationName} in progress...");

                await operation().ConfigureAwait(true);

                var finalMessage = successMessage ?? $"{operationName} completed successfully";
                _updateStatus(finalMessage);
                MessageBox.Show(finalMessage, operationName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _updateStatus($"{operationName} failed");
                _showError($"{operationName} Failed", ex);
            }
            finally
            {
                _toggleProgress(false);
            }
        }

        /// <summary>
        /// Executes an async operation that returns a result with standardized error handling.
        /// </summary>
        /// <typeparam name="T">The type of result returned</typeparam>
        /// <param name="operation">The async operation to execute</param>
        /// <param name="operationName">Name of the operation for status messages</param>
        /// <param name="startMessage">Optional custom start message</param>
        /// <returns>The result of the operation, or default(T) if an error occurred</returns>
        public async Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            string startMessage = null) where T : class
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (string.IsNullOrEmpty(operationName)) throw new ArgumentException("Operation name is required", nameof(operationName));

            try
            {
                _toggleProgress(true);
                _updateStatus(startMessage ?? $"{operationName} in progress...");

                return await operation().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _updateStatus($"{operationName} failed");
                _showError($"{operationName} Failed", ex);
                return null;
            }
            finally
            {
                _toggleProgress(false);
            }
        }

        /// <summary>
        /// Executes an async operation with progress reporting.
        /// </summary>
        /// <param name="operation">The async operation to execute with progress reporting</param>
        /// <param name="operationName">Name of the operation for status messages</param>
        /// <param name="successMessage">Optional custom success message</param>
        public async Task ExecuteWithProgressAsync(
            Func<IProgress<string>, Task> operation,
            string operationName,
            string successMessage = null)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (string.IsNullOrEmpty(operationName)) throw new ArgumentException("Operation name is required", nameof(operationName));

            try
            {
                _toggleProgress(true);

                var progress = new Progress<string>(status => _updateStatus(status));
                await operation(progress).ConfigureAwait(true);

                var finalMessage = successMessage ?? $"{operationName} completed successfully";
                _updateStatus(finalMessage);
            }
            catch (Exception ex)
            {
                _updateStatus($"{operationName} failed");
                _showError($"{operationName} Failed", ex);
            }
            finally
            {
                _toggleProgress(false);
            }
        }

        /// <summary>
        /// Executes a long-running async operation with a cancellable progress dialog.
        /// </summary>
        /// <param name="operation">The async operation to execute with cancellation and progress support</param>
        /// <param name="title">Title for the progress dialog</param>
        /// <param name="message">Initial message for the progress dialog</param>
        /// <param name="showProgressBar">Whether to show an indeterminate progress bar</param>
        public async Task ExecuteWithDialogAsync(
            Func<CancellationToken, IProgress<string>, Task> operation,
            string title,
            string message,
            bool showProgressBar = true)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (string.IsNullOrEmpty(title)) throw new ArgumentException("Title is required", nameof(title));

            try
            {
                await Task.Run(() =>
                {
                    ProgressDialog.Execute(_owner, title, message, operation, showProgressBar);
                }).ConfigureAwait(true);

                _updateStatus($"{title} completed");
            }
            catch (OperationCanceledException)
            {
                _updateStatus($"{title} cancelled");
            }
            catch (Exception ex)
            {
                _updateStatus($"{title} failed");
                _showError($"{title} Failed", ex);
            }
        }

        /// <summary>
        /// Executes a long-running async operation with a cancellable progress dialog showing percentage progress.
        /// </summary>
        /// <param name="operation">The async operation to execute with cancellation and progress support</param>
        /// <param name="title">Title for the progress dialog</param>
        /// <param name="message">Initial message for the progress dialog</param>
        public async Task ExecuteWithPercentageDialogAsync(
            Func<CancellationToken, IProgress<int>, IProgress<string>, Task> operation,
            string title,
            string message)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (string.IsNullOrEmpty(title)) throw new ArgumentException("Title is required", nameof(title));

            try
            {
                await Task.Run(() =>
                {
                    ProgressDialog.ExecuteWithProgress(_owner, title, message, operation);
                }).ConfigureAwait(true);

                _updateStatus($"{title} completed");
            }
            catch (OperationCanceledException)
            {
                _updateStatus($"{title} cancelled");
            }
            catch (Exception ex)
            {
                _updateStatus($"{title} failed");
                _showError($"{title} Failed", ex);
            }
        }
    }
}
