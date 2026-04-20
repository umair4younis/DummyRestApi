using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using NLog;
using System.Windows.Threading;

namespace Puma.MDE.Common.Utilities
{
    /// <summary>
    ///  Used when task parallel return some info to user so that
    /// 1- client can call cancel to cancell the task example  clientTask.Cancel();
    /// 2- client can wait if need be for the task to be finished before starting a new one
    /// Task.WaitAll(clientTask.Task) Task.WaitAny(... many or one task) or Task.WaitAll(... many or one task)
    /// </summary>
    public class TaskControl : IDisposable
    {
        public string TaskId { get; private set; }
        public IDisposable CancelTask { get; private set; }
        public Task Task { get; private set;}

        public TaskControl(IDisposable cancel, Task newTask)
        {
            CancelTask = cancel;
            Task = newTask; 
        }

        public TaskControl(IDisposable cancel, Task newTask, string taskId)
        {
            CancelTask = cancel;
            Task = newTask;
            TaskId = taskId;
        }

        public void Cancel()
        {
            if (CancelTask == null)
                return;

            CancelTask.Dispose();
        }

        #region IDisposable

        private bool _disposed;
        public void Dispose()
        {
            Disposing(true);

            GC.SuppressFinalize(this);
        }

        private void Disposing(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                    if (CancelTask != null)
                    {
                        CancelTask.Dispose();
                        CancelTask = null;
                    }

                    Task = null;
                }

                // unmanaged resources here.
                // ...

                // Note disposing has been done.
                _disposed = true;
            }
        }
        #endregion
     
    }

    [ComVisible(false)]
    public class Tasks : List<TaskControl>
    {
        public void CancelAll()
        {
            ForEach(taskControl =>
                        {
                            if (taskControl != null)
                                taskControl.Cancel();
                        });
        }

        public void CancelAndRemove(string taskId)
        {
            if(Count == 0)
                return;

            var foundTask = Find(T => T.TaskId == taskId);
            if (foundTask != null)
            {
                foundTask.Cancel();

                Remove(foundTask);

                foundTask.Dispose();
            }
        }

        public void CancelAllAndRemove()
        {
            CancelAll();

            ForEach(taskControl =>
            {
                if (taskControl != null)
                    taskControl.Dispose();
            });

            // Finally clean up and be ready for new ones to be added
            Clear();
        }
    }


    public class Threading
    {
        public static void RunInDispatcher(Action action)
        {
            if (Dispatcher.CurrentDispatcher.CheckAccess())
            {
                if (action != null)
                    action();
            }
            else
            {
                Dispatcher.CurrentDispatcher.BeginInvoke
                (
                    new Action(() =>
                    {
                        if (action != null)
                            action();
                    }),
                    DispatcherPriority.Send
                );
            }
        }

        public static void RunInDispatcherAbsorbException(Action action)
        {
            if (Dispatcher.CurrentDispatcher.CheckAccess())
            {
                try
                {
                    if (action != null)
                        action();
                }
                catch
                {
                }
            }
            else
            {
                Dispatcher.CurrentDispatcher.BeginInvoke
                (
                    new Action(() =>
                    {
                        try
                        {
                            if (action != null)
                                action();
                        }
                        catch
                        {
                        }
                    }),
                    DispatcherPriority.Send
                );
            }
        }


        /// <summary>
        /// Run in background and return the result in UI thread
        /// </summary>
        /// <typeparam name="T">Type of result ObservableCollection(T)</typeparam>
        /// <param name="methodName">method to run in background thread</param>
        /// <param name="returnResult">action call back to bring back result into observable collection in UI thread and return exception to the caller</param>
        public static void RunInBackground<T>(Func<IEnumerable<T>> methodName,
                                             Action<IEnumerable<T>, Exception> returnResult)
        {
            if (methodName == null)
                return;

            
            IEnumerable<T> resultFromBackground = null;
            var worker = new BackgroundWorker();

            worker.DoWork += (s, e) =>
            {
                resultFromBackground = methodName();
            };

            worker.RunWorkerCompleted += (s, e) =>
            {
                var exception = e.Error;

                if (Dispatcher.CurrentDispatcher.CheckAccess())
                {
                    if (exception != null)
                        ShowException(exception);

                    if (returnResult != null)
                        returnResult(resultFromBackground, exception);
                }
                else
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke
                        (
                            new Action(() =>
                            {
                                if (exception != null)
                                    ShowException(exception);

                                if (returnResult != null)
                                    returnResult(resultFromBackground, exception);
                            })
                        );
                }

            };

            worker.RunWorkerAsync();
        }

        /// <summary>
        /// Run in background and DOES NOT return the result in UI thread
        /// </summary>
        /// <typeparam name="T">Type of result collection(T)</typeparam>
        /// <param name="methodName">method to run in background thread</param>
        /// <param name="returnResult">action call back to bring back result in the same back ground thread and return exception to the caller</param>
        public static void RunInBackgroundOnly<T>(Func<IEnumerable<T>> methodName,
                                                  Action<IEnumerable<T>, Exception> returnResult)
        {
            if (methodName == null)
                return;


            IEnumerable<T> resultFromBackground = null;
            var worker = new BackgroundWorker();

            worker.DoWork += (s, e) =>
            {
                resultFromBackground = methodName();
            };

            worker.RunWorkerCompleted += (s, e) =>
            {
                var exception = e.Error;

        
                if (returnResult != null)
                    returnResult(resultFromBackground, exception);
            };

            worker.RunWorkerAsync();
        }

        public static void RunInBackgroundOnly(Func<object> methodName,
                                               Action<object, Exception> returnResult)
        {
            if (methodName == null)
                return;

            object resultFromBackground = null;
            var worker = new BackgroundWorker();

            worker.DoWork += (s, e) =>
            {
                resultFromBackground = methodName();
            };

            worker.RunWorkerCompleted += (s, e) =>
            {
                var exception = e.Error;

                if (returnResult != null)
                    returnResult(resultFromBackground, exception);
            };

            worker.RunWorkerAsync();
        }

        /// <summary>
        /// By passing cancelation token to method it can check for cancelation.
        /// The return of CancellableParallelTask has refernce to IDisposable that caller can call Dispose() to request cancelation
        /// </summary>
        /// <typeparam name="T">Type of result collection</typeparam>
        /// <param name="cancellableMethodWithReturningResult">method to call in background</param>
        /// <param name="returnResult">result of either collection or exception</param>
        /// <returns>Disposable ability to cancel long running process in background thread</returns>
        public static TaskControl CancellableParallelTask<T>(Func<Func<bool>, IEnumerable<T>> cancellableMethodWithReturningResult,
                                                             Action<IEnumerable<T>, Exception> returnResult)
        {
           var tokenSource = new CancellationTokenSource(); 
           var cancelToken = tokenSource.Token;
             
            Func<bool> cancel = null;
            cancel += () => cancelToken.IsCancellationRequested; // returns a bool that suggest if cacellation requested

            var taskName = cancellableMethodWithReturningResult.Method.Name;
            var newTask = new Task
                (
                    ()=>
                    {
                        IEnumerable<T> resultFromBackground = null;
                        Exception exception = null;
                        try
                        {
                            Thread.CurrentThread.Name = taskName;
                            // Cancelation can be called at any point in the method execution by calling cancel()
                            resultFromBackground = cancellableMethodWithReturningResult(cancel);
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }

                        // If it is cacelled then return with no result and no exception
                        if (cancel())
                            return;
                
                        if (Dispatcher.CurrentDispatcher.CheckAccess())
                        {
                            if (exception != null)
                                ShowException(exception);

                            if (returnResult != null)
                                returnResult(resultFromBackground, exception);
                        }
                        else
                        {
                            Dispatcher.CurrentDispatcher.BeginInvoke
                                (
                                    new Action(() =>
                                    {
                                        if (exception != null)
                                            ShowException(exception);

                                        if (returnResult != null)
                                            returnResult(resultFromBackground, exception);
                                    })
                                );
                        }
                            
                            
                    }, 
                    cancelToken 
                );

            newTask.Start();

            // return idisposable so that we can cancel the parallel processing
            return new TaskControl(null, newTask, taskName);
         }

        public static TaskControl CancellableParallelTask<T>(string taskId,
                                                             Func<Func<bool>, IEnumerable<T>> cancellableMethodWithReturningResult,
                                                             Action<IEnumerable<T>, Exception> returnResult)
        {
            var tokenSource = new CancellationTokenSource();
            var cancelToken = tokenSource.Token;

            Func<bool> cancel = null;
            cancel += () => cancelToken.IsCancellationRequested; // returns a bool that suggest if cacellation requested

            var newTask = new Task
                (
                    () =>
                    {
                        IEnumerable<T> resultFromBackground = null;
                        Exception exception = null;
                        try
                        {
                            Thread.CurrentThread.Name = taskId;
                            // Cancelation can be called at any point in the method execution by calling cancel()
                            resultFromBackground = cancellableMethodWithReturningResult(cancel);
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }

                        // If it is cacelled then return with no result and no exception
                        if (cancel())
                            return;

                        if (Dispatcher.CurrentDispatcher.CheckAccess())
                        {
                            if (exception != null)
                                ShowException(exception);

                            if (returnResult != null)
                                returnResult(resultFromBackground, exception);
                        }
                        else
                        {
                            Dispatcher.CurrentDispatcher.BeginInvoke
                                (
                                    new Action(() =>
                                    {
                                        if (exception != null)
                                            ShowException(exception);

                                        if (returnResult != null)
                                            returnResult(resultFromBackground, exception);
                                    })
                                );
                        }


                    },
                    cancelToken
                );

            newTask.Start();

            // return idisposable so that we can cancel the parallel processing
            return new TaskControl(null, newTask, taskId);
        }

        public static TaskControl CancellableParallelTask(Func<Func<bool>, object> cancellableMethodWithReturningResult,
                                                            Action<object, Exception> returnResult)
        {
            var tokenSource = new CancellationTokenSource();
            var cancelToken = tokenSource.Token;

            Func<bool> cancel = null;
            cancel += () => cancelToken.IsCancellationRequested; // returns a bool that suggest if cacellation requested

            var taskName = cancellableMethodWithReturningResult.Method.Name;
            var newTask = new Task
                (
                    () =>
                    {
                        object resultFromBackground = null;
                        Exception exception = null;
                        try
                        {
                            Thread.CurrentThread.Name = taskName;
                            // Cancelation can be called at any point in the method execution by calling cancel()
                            resultFromBackground = cancellableMethodWithReturningResult(cancel);
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }

                        // If it is cacelled then return with no result and no exception
                        if (cancel())
                            return;

                        if (Dispatcher.CurrentDispatcher.CheckAccess())
                        {
                            if (exception != null)
                                ShowException(exception);

                            if (returnResult != null)
                                returnResult(resultFromBackground, exception);
                        }
                        else
                        {
                            Dispatcher.CurrentDispatcher.BeginInvoke
                                (
                                    new Action(() =>
                                    {
                                        if (exception != null)
                                            ShowException(exception);

                                        if (returnResult != null)
                                            returnResult(resultFromBackground, exception);
                                    })
                                );
                        }


                    },
                    cancelToken
                );

            newTask.Start();

            // return idisposable so that we can cancel the parallel processing
            return new TaskControl(null, newTask, taskName);
        }

        public static TaskControl CancellableParallelTask(Func<Func<bool>, object, object> cancellableMethodWithReturningResult,
                                                          object parameter,
                                                          Action<object, Exception> returnResult)
        {
            var tokenSource = new CancellationTokenSource();
            var cancelToken = tokenSource.Token;

            Func<bool> cancel = null;
            cancel += () => cancelToken.IsCancellationRequested; // returns a bool that suggest if cacellation requested

            var taskName = cancellableMethodWithReturningResult.Method.Name;
            var newTask = new Task
                (
                    () =>
                    {
                        object resultFromBackground = null;
                        Exception exception = null;
                        try
                        {
                            Thread.CurrentThread.Name = taskName;
                            // Cancelation can be called at any point in the method execution by calling cancel()
                            resultFromBackground = cancellableMethodWithReturningResult(cancel, parameter);
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }

                        // If it is cacelled then return with no result and no exception
                        if (cancel())
                            return;

                        if (Dispatcher.CurrentDispatcher.CheckAccess())
                        {
                            if (exception != null)
                                ShowException(exception);

                            if (returnResult != null)
                                returnResult(resultFromBackground, exception);
                        }
                        else
                        {
                            Dispatcher.CurrentDispatcher.BeginInvoke
                                (
                                    new Action(() =>
                                    {
                                        if (exception != null)
                                            ShowException(exception);

                                        if (returnResult != null)
                                            returnResult(resultFromBackground, exception);
                                    })
                                );
                        }


                    },
                    cancelToken
                );

            newTask.Start();

            // return idisposable so that we can cancel the parallel processing
            return new TaskControl(null, newTask, taskName);
        }

 
        /// <summary>
        /// Task with no cancellation ability with returning a collection
        /// </summary>
        public static TaskControl ParallelTask<T>(Func<IEnumerable<T>> unCancellableMethodWithReturningResult,
                                                  Action<IEnumerable<T>, Exception> result)
        {
            if (unCancellableMethodWithReturningResult == null)
                return null;

            var taskId = unCancellableMethodWithReturningResult.Method.Name;
            var newTask = new Task
                (
                    () =>
                    {
                        IEnumerable<T> resultFromBackground = null;
                        Exception exception = null;
                        try
                        {
                            Thread.CurrentThread.Name = taskId;
                            // Cancelation can be called at any point in the method execution by calling cancel()
                            resultFromBackground = unCancellableMethodWithReturningResult();
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }

                        
                        if (Dispatcher.CurrentDispatcher.CheckAccess())
                        {
                            if (exception != null)
                                ShowException(exception);

                            if (result != null)
                                result(resultFromBackground, exception);
                        }
                        else
                        {
                            Dispatcher.CurrentDispatcher.BeginInvoke
                               (
                                   new Action(() =>
                                   {
                                       if (exception != null)
                                           ShowException(exception);

                                       if (result != null)
                                           result(resultFromBackground, exception);
                                   })
                               );
                        }
                           

                    }
                );

            newTask.Start();

            // return idisposable so that we can cancel the parallel processing
            return new TaskControl(null, newTask, taskId);
        }

        /// <summary>
        /// Task with no cancellation ability with returning an object
        /// </summary>
        public static TaskControl ParallelTask(Func<object> unCancellableMethodWithReturningResult,
                                                  Action<object, Exception> result)
        {
            if (unCancellableMethodWithReturningResult == null)
                return null;

            var taskId = unCancellableMethodWithReturningResult.Method.Name;
            var newTask = new Task
                (
                    () =>
                    {
                        object resultFromBackground = null;
                        Exception exception = null;
                        try
                        {
                            Thread.CurrentThread.Name = taskId;
                            // Cancelation can be called at any point in the method execution by calling cancel()
                            resultFromBackground = unCancellableMethodWithReturningResult();
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }


                        if (Dispatcher.CurrentDispatcher.CheckAccess())
                        {
                            if (exception != null)
                                ShowException(exception);

                            if (result != null)
                                result(resultFromBackground, exception);
                        }
                        else
                        {
                            Dispatcher.CurrentDispatcher.BeginInvoke
                               (
                                   new Action(() =>
                                   {
                                       if (exception != null)
                                           ShowException(exception);

                                       if (result != null)
                                           result(resultFromBackground, exception);
                                   })
                               );
                        }


                    }
                );

            newTask.Start();

            // return idisposable so that we can cancel the parallel processing
            return new TaskControl(null, newTask, taskId);
        }


        public static TaskControl ParallelTask(string taskId, 
                                               Func<object> unCancellableMethodWithReturningResult,
                                                 Action<object, Exception> result)
        {
            if (unCancellableMethodWithReturningResult == null)
                return null;

            var newTask = new Task
                (
                    () =>
                    {
                        object resultFromBackground = null;
                        Exception exception = null;
                        try
                        {
                            Thread.CurrentThread.Name = taskId;
                            // Cancelation can be called at any point in the method execution by calling cancel()
                            resultFromBackground = unCancellableMethodWithReturningResult();
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }


                        if (Dispatcher.CurrentDispatcher.CheckAccess())
                        {
                            if (exception != null)
                                ShowException(exception);

                            if (result != null)
                                result(resultFromBackground, exception);
                        }
                        else
                        {
                            Dispatcher.CurrentDispatcher.BeginInvoke
                               (
                                   new Action(() =>
                                   {
                                       if (exception != null)
                                           ShowException(exception);

                                       if (result != null)
                                           result(resultFromBackground, exception);
                                   })
                               );
                        }


                    }
                );

            newTask.Start();

            // return idisposable so that we can cancel the parallel processing
            return new TaskControl(null, newTask, taskId);
        }



        public static TaskControl ParallelTask<T>(string taskId, 
                                                  Func<IEnumerable<T>> unCancellableMethodWithReturningResult,
                                                  Action<IEnumerable<T>, Exception> result)
        {
            if (unCancellableMethodWithReturningResult == null)
                return null;

            var newTask = new Task
                (
                    () =>
                    {
                        IEnumerable<T> resultFromBackground = null;
                        Exception exception = null;
                        try
                        {
                            Thread.CurrentThread.Name = taskId;
                            // Cancelation can be called at any point in the method execution by calling cancel()
                            resultFromBackground = unCancellableMethodWithReturningResult();
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }


                        if (Dispatcher.CurrentDispatcher.CheckAccess())
                        {
                            if (exception != null)
                                ShowException(exception);

                            if (result != null)
                                result(resultFromBackground, exception);
                        }
                        else
                        {
                            Dispatcher.CurrentDispatcher.BeginInvoke
                               (
                                   new Action(() =>
                                   {
                                       if (exception != null)
                                           ShowException(exception);

                                       if (result != null)
                                           result(resultFromBackground, exception);
                                   })
                               );
                        }


                    }
                );

            newTask.Start();

            // return idisposable so that we can cancel the parallel processing
            return new TaskControl(null, newTask, taskId);
        }


        /// <summary>
        /// Task with no cancellation ability
        /// </summary>
        public static TaskControl ParallelTask(Action unCancellableVoidMethod, Action<Exception> result)
        {
            if (unCancellableVoidMethod == null)
                return null;

            var taskId = unCancellableVoidMethod.Method.Name;

            var newTask = new Task
                (
                    () =>
                    {
                        Exception exception = null;
                        try
                        {
                            Thread.CurrentThread.Name = taskId;
                            // Cancelation can be called at any point in the method execution by calling cancel()
                            unCancellableVoidMethod();
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }

                        if(Dispatcher.CurrentDispatcher.CheckAccess())
                        {
                            if (exception != null)
                                ShowException(exception);

                            if (result != null)
                                result(exception);
                        }
                        else
                        {
                            Dispatcher.CurrentDispatcher.BeginInvoke
                               (
                                   new Action(() =>
                                   {
                                       if (exception != null)
                                           ShowException(exception);

                                       if (result != null)
                                           result(exception);
                                   })
                               );
                        }
                           
                    }
                );

            newTask.Start();

            // return idisposable so that we can cancel the parallel processing
            return new TaskControl(null, newTask, taskId);
        }

        public static TaskControl ParallelTask(string taskId, Action unCancellableVoidMethod, Action<Exception> result)
        {
            if (unCancellableVoidMethod == null)
                return null;


            var newTask = new Task
                (
                    () =>
                    {
                        Exception exception = null;
                        try
                        {
                            Thread.CurrentThread.Name = taskId;
                            // Cancelation can be called at any point in the method execution by calling cancel()
                            unCancellableVoidMethod();
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }

                        if (Dispatcher.CurrentDispatcher.CheckAccess())
                        {
                            if (exception != null)
                                ShowException(exception);

                            if (result != null)
                                result(exception);
                        }
                        else
                        {
                            Dispatcher.CurrentDispatcher.BeginInvoke
                               (
                                   new Action(() =>
                                   {
                                       if (exception != null)
                                           ShowException(exception);

                                       if (result != null)
                                           result(exception);
                                   })
                               );
                        }

                    }
                );

            newTask.Start();

            // return idisposable so that we can cancel the parallel processing
            return new TaskControl(null, newTask, taskId);
        }

        private static void ShowException(Exception e)
        {
            Exception currentException;
            if (e is TargetInvocationException)
                currentException = e.InnerException;
            if (e is AggregateException)
                currentException = ((AggregateException)e).Flatten().GetBaseException();
            else
                currentException = e;

            if (currentException == null) 
                return;

            var log = LogManager.GetLogger("puma.mde");
            string message =
                String.Format("MDE Error : Time: {0}\n{1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), currentException.Message);

            log.Error(currentException);
            System.Windows.Forms.MessageBox.Show(message, "MDE Exception");
        }
    }
}
