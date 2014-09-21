using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton
{
	public interface ITask
	{
		void Execute(float deltaTime);
		bool Enabled { get; }
	}
}
