using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World
{
	public class GameObject
	{
		private readonly List<GameObject> Children = new List<GameObject>();
		private readonly List<IComponent> Components = new List<IComponent>();
		public GameObject Parent { get; private set; }

		public readonly int Id;
		public readonly GameWorld World;
		public bool Active { get; private set; }

		public Vector3 Position = Vector3.Zero;
		public Matrix4 Orientation = Matrix4.Identity;

		public GameObject(GameWorld world, int id)
		{
			if (world == null)
				throw new ArgumentNullException("world");

			World = world;
			Id = id;
		}

		public void OnAttached()
		{
			Components.ForEach(c => c.OnActivate());
			Children.ForEach(c => c.OnAttached());

			Active = true;
		}

		public void OnDetached()
		{
			Components.ForEach(c => c.OnDeactivate());
			Children.ForEach(c => c.OnDetached());

			Active = false;
		}

		public void Update(float stepSize)
		{
			Components.ForEach(c => c.Update(stepSize));
			Children.ForEach(c => c.Update(stepSize));
		}

		public void AddComponent(IComponent component)
		{
			if (component == null)
				throw new ArgumentNullException("component");

			if (component.Owner != null)
				throw new ArgumentException("component is already attached to a game object");

			// Determine and add missing required components
			foreach (var attribute in component.GetType().GetCustomAttributes(typeof(RequiresComponentAttribute), true))
			{
				var type = (attribute as RequiresComponentAttribute).ComponentType;
				if (!HasComponent(type))
				{
					var requiredComponent = Triton.Common.Utility.ReflectionHelper.CreateInstance(type) as IComponent;
					Components.Add(requiredComponent);
					requiredComponent.OnAttached(this);
				}
			}

			Components.Add(component);
			component.OnAttached(this);
		}

		public void RemoveComponent(IComponent component)
		{
			if (component == null)
				throw new ArgumentNullException("component");

			if (component.Owner != this)
				throw new ArgumentException("does not own component");

			Components.Remove(component);
			component.OnDeactivate();
		}

		public bool HasComponent<TComponentType>()
		{
			return Components.Exists(c => c is TComponentType);
		}

		public bool HasComponent(Type type)
		{
			return Components.Exists(c => type.IsInstanceOfType(c));
		}

		public TComponentType GetComponent<TComponentType>()
		{
			return (TComponentType)Components.First(c => c is TComponentType);
		}

		public IEnumerable<TComponentType> GetComponents<TComponentType>() where TComponentType : class
		{
			foreach (var component in Components.FindAll(c => c is TComponentType))
				yield return component as TComponentType;
		}

		public IEnumerable<IComponent> GetComponents()
		{
			return Components;
		}

		public void AddChild(GameObject gameObject)
		{
			if (gameObject == null)
				throw new ArgumentNullException("gameObject");
			if (gameObject.Parent != null)
				throw new InvalidOperationException("Child already attached to another GameObject");

			gameObject.Parent = this;
			Children.Add(gameObject);
		}

		public void RemoveChild(GameObject gameObject)
		{
			if (gameObject.Parent != this)
				throw new InvalidOperationException("GameObject does not own child");

			Children.Remove(gameObject);
			gameObject.Parent = null;
		}

		public IEnumerable<GameObject> GetChildren()
		{
			return Children;
		}
	}
}
