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
		public readonly GameObjectManager World;
		public bool Active { get; private set; }

		public Vector3 Position = Vector3.Zero;
		public Matrix4 Orientation = Matrix4.Identity;
		public Vector3 Scale = new Vector3(1, 1, 1);

		public GameObject(GameObjectManager world, int id)
		{
            World = world ?? throw new ArgumentNullException("world");
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
			Components.ForEach(c => c.OnDetached());
			Children.ForEach(c => c.OnDetached());

			Active = false;
		}

		public void Update(float stepSize)
		{
            foreach (var component in Components)
            {
                component.Update(stepSize);
            }

            foreach (var child in Children)
            {
                child.Update(stepSize);
            }
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

			if (Active)
			{
				component.OnActivate();
			}
		}

		public void RemoveComponent(IComponent component)
		{
			if (component == null)
				throw new ArgumentNullException("component");

			if (component.Owner != this)
				throw new ArgumentException("does not own component");

			Components.Remove(component);
			component.OnDetached();
		}

		public bool HasComponent<TComponentType>()
		    => Components.Exists(c => c is TComponentType);

        public bool HasComponent(Type type) 
            => Components.Exists(c => type.IsInstanceOfType(c));

        public TComponentType GetComponent<TComponentType>() 
            => (TComponentType)Components.First(c => c is TComponentType);

        public IEnumerable<TComponentType> GetComponents<TComponentType>() where TComponentType : class
		{
			foreach (var component in Components.FindAll(c => c is TComponentType))
				yield return component as TComponentType;
		}

        public IEnumerable<IComponent> GetComponents() 
            => Components;

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
            => Children;
    }
}
