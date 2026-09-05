// Copyright HTC Corporation All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VIVE.OpenXR.Feature;
using static VIVE.OpenXR.Feature.FutureWrapper;

namespace VIVE.OpenXR.Toolkits
{
    /// <summary>
    /// FutureTask is not a C# Task.  It is a wrapper for OpenXR Future.
    /// Each OpenXR Future may have its own FutureTask type because the result and the
    /// complete function are different.
    /// However the poll and complete are common.  This class use c# Task to poll the future.
    /// You can <see cref="Cancel the future"/> if you do not want to wait for the result.
    /// However Cancel should be called before the Complete.
    /// When <see cref="IsPollCompleted"/> is true, call <see cref="Complete"/> to complete the future and get the result.
    /// </summary>
    /// <typeparam name="TResult">You can customize the type depending on the complete's result.</typeparam>
    public class FutureTask<TResult> : IDisposable
    {
        private Task<(XrResult, XrFutureStateEXT)> pollTask;
        private Task autoCompleteTask;

        Func<IntPtr, TResult> completeFunc;
        private CancellationTokenSource cts;
        private IntPtr future;
        bool autoComplete = false;
        private int pollIntervalMS = 10;

        /// <summary>
        /// Set poll inverval in milliseconds.  The value will be clamped between 1 and 2000.
        /// </summary>
        public int PollIntervalMS {
            get => pollIntervalMS;
            set => pollIntervalMS = Math.Clamp(value, 1, 2000);
        }

        public bool IsAutoComplete => autoComplete;

        bool isCompleted = false;
        public bool IsCompleted => isCompleted;

        public bool Debug { get; set; } = false;

        /// <summary>
        /// The FutureTask is used to poll and complete the future.
        /// Once the FutureTask is create, poll will start running in the period of pollIntervalMS.
        /// if auto complete is set, the future will be do completed once user check IsPollCompleted and IsCompleted.
        /// I prefered to use non-autoComplete.
        /// If no auto complete, you can cancel the task and no need to free resouce.
        /// Once it completed, you need handle the result to avoid leakage.
        /// </summary>
        /// <param name="future"></param>
        /// <param name="completeFunc"></param>
        /// <param name="pollIntervalMS">Set poll inverval in milliseconds.  The value will be clamped between 1 and 2000.</param>
        /// <param name="autoComplete">If true, do Complete when check IsPollCompleted and IsCompleted</param>
        public FutureTask(IntPtr future, Func<IntPtr, TResult> completeFunc, int pollIntervalMS = 10, bool autoComplete = false)
        {
            cts = new CancellationTokenSource();
            this.completeFunc = completeFunc;
            this.future = future;
            this.pollIntervalMS = Math.Clamp(pollIntervalMS, 1, 2000);

            // User may get PollTask and run.  So, we need to make sure the pollTask is created.
            pollTask = MakePollTask(this, cts.Token);

            // will set autoComplete true in AutoComplete.
            this.autoComplete = false;
            if (autoComplete)
                AutoComplete();
        }

        /// <summary>
        /// AutoComplete will complete the future once the poll task is ready and success.
        /// If you want to handle error, you should not use AutoComplete.
        /// </summary>
        public void AutoComplete()
        {
            if (autoComplete)
                return;
            autoComplete = true;
            autoCompleteTask = pollTask.ContinueWith(task =>
            {
                // If the task is cancelled or faulted, we do not need to complete the future.
                if (task.IsCanceled || task.IsFaulted)
                {
                    isCompleted = true;
                    return;
                }

                var result = task.Result;

                // Make sure call Complete only if poll task is ready and success.
                if (result.Item1 == XrResult.XR_SUCCESS)
                {
                    if (result.Item2 == XrFutureStateEXT.Ready)
                    {
                        Complete();
                    }
                }
                isCompleted = true;
            });
        }

        /// <summary>
        /// Used for create FromResult if you need return the result immediately.
        /// </summary>
        /// <param name="pollTask"></param>
        /// <param name="completeFunc"></param>
        FutureTask(Task<(XrResult, XrFutureStateEXT)> pollTask, Func<IntPtr, TResult> completeFunc)
        {
            this.pollTask = pollTask;
            this.completeFunc = completeFunc;
            this.future = IntPtr.Zero;
        }

        public Task<(XrResult, XrFutureStateEXT)> PollTask => pollTask;

        /// <summary>
        /// If AutoComplete is set, the task will be created.  Otherwise, it will be null.
        /// </summary>
        public Task AutoCompleteTask => autoCompleteTask;

        public bool IsPollCompleted => pollTask.IsCompleted;

        public XrResult PollResult => pollTask.Result.Item1;

        public IntPtr Future => future;

        /// <summary>
        /// Cancel the future.  If the future is not completed yet, it will be cancelled. Otherwise, nothing will happen.
        /// </summary>
        public void Cancel()
        {
            if (!isCompleted)
            {
                cts?.Cancel();
                FutureWrapper.Instance?.CancelFuture(future);
            }
            future = IntPtr.Zero;
        }

        /// <summary>
        /// Make sure do Complete after IsPollCompleted.  If the future is not poll completed yet, throw exception.
        /// </summary>
        /// <returns>The result of the completeFunc.</returns>
        /// <exception cref="Exception">Thrown when the pollTask is not completed yet.</exception>
        public TResult Complete()
        {
            if (isCompleted)
                return result;
            if (pollTask.IsCompleted)
            {
                if (this.Debug)
                    UnityEngine.Debug.Log("FutureTask is completed.");
                isCompleted = true;
                if (pollTask.Result.Item1 == XrResult.XR_SUCCESS)
                {
                    result = completeFunc(future);
                    isCompleted = true;
                    return result;
                }
                if (this.Debug)
                    UnityEngine.Debug.Log("FutureTask is completed with error.  Check if pollTask result error.");
                return default;
            }
            else
            {
                throw new Exception("FutureTask is not completed yet.");
            }
        }

        /// <summary>
        /// Wait until poll task is completed.  If the task is not completed, it will block the thread.
        /// If AutoComplete is set, wait until the complete task is completed.
        /// </summary>
        public void Wait()
        {
            pollTask.Wait();
            if (autoComplete)
                autoCompleteTask.Wait();
        }

        TResult result;
        private bool disposedValue;

        /// <summary>
        /// This Result did not block the thread.  If not completed, it will return undefined value.  Make sure you call it when Complete is done.
        /// </summary>
        public TResult Result => result;

        public static FutureTask<TResult> FromResult(TResult result)
        {
            return new FutureTask<TResult>(Task.FromResult((XrResult.XR_SUCCESS, XrFutureStateEXT.Ready)), (future) => result);
        }

        /// <summary>
        /// Poll until the future is ready.  Caceled if the cts is cancelled.  But the future will not be cancelled.
        /// </summary>
        /// <param name="futureTask"></param>
        /// <param name="pollIntervalMS"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        static async Task<(XrResult, XrFutureStateEXT)> MakePollTask(FutureTask<TResult> futureTask, CancellationToken ct)
        {
            XrFuturePollInfoEXT pollInfo = new XrFuturePollInfoEXT()
            {
                type = XrStructureType.XR_TYPE_FUTURE_POLL_INFO_EXT,
                next = IntPtr.Zero,
                future = futureTask.Future
            };
            do
            {
                ct.ThrowIfCancellationRequested();

                XrResult ret = FutureWrapper.Instance.PollFuture(ref pollInfo, out FutureWrapper.XrFuturePollResultEXT pollResult);
                if (ret == XrResult.XR_SUCCESS)
                {
                    if (pollResult.state == XrFutureStateEXT.Ready)
                    {
                        if (futureTask.Debug)
                            UnityEngine.Debug.Log("Future is ready.");
                        return (XrResult.XR_SUCCESS, pollResult.state);
                    }
                    else if (pollResult.state == XrFutureStateEXT.Pending)
                    {
                        if (futureTask.Debug)
                            UnityEngine.Debug.Log("Wait for future.");
                        await Task.Delay(futureTask.pollIntervalMS);
                        continue;
                    }
                }
                else
                {
                    return (ret, XrFutureStateEXT.None);
                }
            } while (true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    pollTask?.Dispose();
                    pollTask = null;
                    autoCompleteTask?.Dispose();
                    autoCompleteTask = null;
                    cts?.Dispose();
                    cts = null;
                }

                if (future != IntPtr.Zero && !isCompleted)
                    FutureWrapper.Instance?.CancelFuture(future);
                future = IntPtr.Zero;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Help to manage the future task.  Tasks are name less.  In order to manager tasks, 
    /// additonal information are required.  And this class is used to store those information.
    /// Helps to retrive the task by the identity, or retrive the identity by the task.
    /// </summary>
    /// <typeparam name="Identity">What the task work for.  How to identify this task.</typeparam>
    /// <typeparam name="Output">The task's output type, for example, XrResult or Tuple.</typeparam>
    public class FutureTaskManager<Identity, TResult> : IDisposable
    {
        readonly List<(Identity, FutureTask<TResult>)> tasks = new List<(Identity, FutureTask<TResult>)>();
        private bool disposedValue;

        public FutureTaskManager() { }

        public FutureTask<TResult> GetTask(Identity identity)
        {
            return tasks.FirstOrDefault(x => x.Item1.Equals(identity)).Item2;
        }

        /// <summary>
        /// Add a task to the manager.
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="task"></param>
        public void AddTask(Identity identity, FutureTask<TResult> task)
        {
            tasks.Add((identity, task));
        }

        /// <summary>
        /// Remove keeped task and cancel it If task is not completed.
        /// </summary>
        /// <param name="task"></param>
        public void RemoveTask(Identity identity)
        {
            var task = tasks.FirstOrDefault(x => x.Item1.Equals(identity));
            if (task.Item2 != null)
            {
                task.Item2.Cancel();
                task.Item2.Dispose();
            }
            tasks.Remove(task);
        }

        /// <summary>
        /// Remove keeped task and cancel it If task is not completed.
        /// </summary>
        /// <param name="task"></param>
        public void RemoveTask(FutureTask<TResult> task)
        {
            var t = tasks.FirstOrDefault(x => x.Item2 == task);
            if (t.Item2 != null)
            {
                t.Item2.Cancel();
                t.Item2.Dispose();
            }
            tasks.Remove(t);
        }

        /// <summary>
        /// Get all tasks's list.
        /// </summary>
        /// <returns></returns>
        public List<(Identity, FutureTask<TResult>)> GetTasks()
        {
            return tasks;
        }

        /// <summary>
        /// Check if has any task.
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return tasks.Count == 0;
        }

        /// <summary>
        /// Clear all tasks and cancel them. If tasks are auto completed, make sure their results handled.
        /// Otherwise, the resource will be leaked.
        /// </summary>
        /// <param name="cancelTask"></param>
        public void Clear(bool cancelTask = true)
        {
            if (cancelTask)
            {
                foreach (var task in tasks)
                {
                    if (task.Item2 != null)
                    {
                        task.Item2.Cancel();
                        task.Item2.Dispose();
                    }
                }
            }
            tasks.Clear();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Clear();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
