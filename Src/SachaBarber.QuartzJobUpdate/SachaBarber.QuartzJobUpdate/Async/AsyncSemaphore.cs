using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SachaBarber.QuartzJobUpdate.Async
{
    /// <summary>
    /// See http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266983.aspx
    /// from the fabulous Stephen Toub
    /// </summary>
    public class AsyncSemaphore
    {
        private static readonly Task s_completed = Task.FromResult(true);
        private readonly Queue<TaskCompletionSource<bool>> _mWaiters = new Queue<TaskCompletionSource<bool>>();
        private int _mCurrentCount;

        public AsyncSemaphore(int initialCount)
        {
            if (initialCount < 0) throw new ArgumentOutOfRangeException("initialCount");
            _mCurrentCount = initialCount;
        }

        public Task WaitAsync()
        {
            lock (_mWaiters)
            {
                if (_mCurrentCount > 0)
                {
                    --_mCurrentCount;
                    return s_completed;
                }
                else
                {
                    var waiter = new TaskCompletionSource<bool>();
                    _mWaiters.Enqueue(waiter);
                    return waiter.Task;
                }
            }
        }


        public void Release()
        {
            TaskCompletionSource<bool> toRelease = null;
            lock (_mWaiters)
            {
                if (_mWaiters.Count > 0)
                    toRelease = _mWaiters.Dequeue();
                else
                    ++_mCurrentCount;
            }
            if (toRelease != null)
                toRelease.SetResult(true);
        }


    }
}
