using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common.Concurrency
{
    public static class TaskHelpers
    {
        private static TaskFactory _mainThreadFactory;
        private static TaskFactory _ioThreadFactory;

        public static void Initialize(SingleThreadScheduler mainThreadScheduler, SingleThreadScheduler ioThreadScheduler)
        {
            _mainThreadFactory = new TaskFactory(mainThreadScheduler);
            _ioThreadFactory = new TaskFactory(ioThreadScheduler);
        }

        public static Task RunOnIOThread(Action f)
            => _ioThreadFactory.StartNew(f);

        public static Task<T> RunOnIOThread<T>(Func<T> f)
            => _ioThreadFactory.StartNew(f);

        public static Task RunOnMainThread(Action f)
            => _mainThreadFactory.StartNew(f);

        public static Task<T> RunOnMainThread<T>(Func<T> f)
            => _mainThreadFactory.StartNew(f);
    }
}
