using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Triton.Common
{
	public class WorkerThread
	{
		private ConcurrentQueue<Action> Queue = new ConcurrentQueue<Action>();
		private ManualResetEvent ShuttingDownEvent = new ManualResetEvent(false);
		private ManualResetEvent LoopEvent = new ManualResetEvent(true);

		public WorkerThread()
		{
			var thread = new Thread(Worker);
			thread.IsBackground = true;
			thread.Name = "Background Worker Thread";
			thread.Start();
		}

		private void Worker()
		{
			while (WaitHandle.WaitAny(new WaitHandle[] { ShuttingDownEvent, LoopEvent }) != 0)
			{
				while (!Queue.IsEmpty)
				{
					Action work;
					if (Queue.TryDequeue(out work))
					{
						work();
					}
				}

				Thread.Sleep(1);
				LoopEvent.Set();
			}
		}

		public void Stop()
		{
			ShuttingDownEvent.Set();
		}

		public void AddItem(Action workItem)
		{
			if (workItem == null)
				throw new ArgumentNullException();

			Queue.Enqueue(workItem);
		}
	}
}
