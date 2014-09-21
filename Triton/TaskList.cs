using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton
{
	public class TaskList
	{
		private readonly List<ITask> Tasks = new List<ITask>();

		public void Add(ITask task)
		{
			if (task == null)
				throw new ArgumentNullException("task");

			Tasks.Add(task);
		}

		public void Remove(ITask task)
		{
			Tasks.Remove(task);
		}

		public void Execute(float deltaTime)
		{
			foreach (var task in Tasks)
			{
				if (task.Enabled)
					task.Execute(deltaTime);
			}
		}
	}
}
