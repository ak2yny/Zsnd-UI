using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;

namespace Zsnd_UI.Functions
{
    /// <summary>
    /// A wrapper that watches a <see cref="Task"/> and privides notifications when it completes/fails.
    /// </summary>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <remarks><see href="https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/march/async-programming-patterns-for-asynchronous-mvvm-applications-data-binding"/></remarks>
    public partial class NotifyTaskCompletion<TResult> : ObservableObject
    {
        public Task<TResult> Task { get; private set; }
        public TResult? Result => (Task.Status == TaskStatus.RanToCompletion) ? Task.Result : default;
        public TaskStatus Status => Task.Status;
        public bool IsRunning { get; set; } = true;
        public bool IsCompleted => Task.IsCompleted;
        public bool IsNotCompleted => !Task.IsCompleted;
        public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;
        public bool IsCanceled => Task.IsCanceled;
        public bool IsFaulted => Task.IsFaulted;
        public AggregateException? Exception => Task.Exception;
        public string? ErrorMessage => Task.Exception?.InnerException?.Message; // No stack trace
        public bool ShowErrorSubtitle { get; private set; }

        public NotifyTaskCompletion(Task<TResult> task)
        {
            Task = task;
            _ = WatchTaskAsync(task, null);
        }

        public void Run(Task<TResult> task, Action? onFaulted = null)
        {
            if (IsRunning) { return; }
            Task = task;
            IsRunning = true;
            OnPropertyChanged(nameof(IsNotCompleted));
            OnPropertyChanged(nameof(IsFaulted));
            _ = WatchTaskAsync(task, onFaulted);
        }

        private async Task WatchTaskAsync(Task<TResult> task, Action? onFaulted)
        {
            try
            {
                _ = await task;
            }
            catch
            {
                // Exceptions are captured in the Task property
            }
            if (task.IsFaulted)
            {
                ShowErrorSubtitle = onFaulted is not null;
                OnPropertyChanged(nameof(IsFaulted));
                OnPropertyChanged(nameof(ShowErrorSubtitle));
                OnPropertyChanged(nameof(ErrorMessage));
                //OnPropertyChanged(nameof(Exception));
                onFaulted?.Invoke();
            }
            //OnPropertyChanged(nameof(Status));
            //OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(IsNotCompleted));
            //OnPropertyChanged(nameof(IsSuccessfullyCompleted));
            //OnPropertyChanged(nameof(IsCanceled));
            //OnPropertyChanged(nameof(Result));
            IsRunning = false;
        }
    }
}