﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World
{
	public class Component : IComponent
	{
		protected Components.Transform Transform { get; private set; }
		public GameObject Owner { get; private set; }
		protected GameWorld World { get; private set; }
		protected Graphics.Stage Stage { get; private set; }
		protected GameObject Parent { get { return Owner.Parent; } }

		public virtual void OnAttached(GameObject owner)
		{
			if (owner == null)
				throw new ArgumentNullException("owner");

			Owner = owner;
			World = Owner.World;
			Stage = World.Stage;

			Transform = Owner.GetComponent<Components.Transform>();
		}

		public virtual void OnDetached()
		{
		}

		public virtual void OnActivate()
		{

		}

		public virtual void Update(float stepSize)
		{

		}
	}
}