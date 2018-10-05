using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Triton.Common.Concurrency
{
    public class SingleThreadScheduler : System.Threading.Tasks.TaskScheduler
    {
        private readonly ConcurrentQueue<Task> _pending = new ConcurrentQueue<Task>();
        private readonly Thread _thread;

        public SingleThreadScheduler(Thread thread)
        {
            _thread = thread ?? throw new ArgumentNullException("thread");
        }

        public bool HasScheduledTasks { get { return _pending.Count > 0; } }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _pending;
        }

        protected override void QueueTask(Task task)
        {
            _pending.Enqueue(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (Thread.CurrentThread == _thread)
            {
                return TryExecuteTask(task);
            }
            else
            {
                return false;
            }
        }

        public void Tick(Stopwatch timer, long maxMs)
        {
            var start = timer.ElapsedMilliseconds;
            while ((timer.ElapsedMilliseconds - start) < maxMs && !_pending.IsEmpty)
            {
                if (_pending.TryDequeue(out var task))
                {
                    TryExecuteTask(task);
                }
            }
        }
    }
}
