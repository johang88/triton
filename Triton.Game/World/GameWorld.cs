using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Common;
using Triton.Graphics;
using Triton.Input;

namespace Triton.Game.World
{
	public class GameWorld
	{
		private List<GameObject> GameObjects { get; set; }
		private HashSet<GameObject> GameObjectsToAdd { get; set; }
		private HashSet<GameObject> GameObjectsToRemove { get; set; }
		private int LastId = 0;
		
		public Stage Stage { get; private set; }
		public InputManager InputManager { get; private set; }
		public ResourceManager ResourceManager { get; private set; }
		public Triton.Physics.World PhysicsWorld { get; private set; }

		Dictionary<int, GameObject> GameObjectsById = new Dictionary<int, GameObject>();

		private int LastGameObjectId = 0;

		public GameWorld(Stage stage, InputManager inputManager, ResourceManager resourceManager, Triton.Physics.World physicsWorld)
		{
			if (stage == null)
				throw new ArgumentNullException("stage");
			if (inputManager == null)
				throw new ArgumentNullException("inputManager");
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");
			if (physicsWorld == null)
				throw new ArgumentNullException("physicsWorld");

			Stage = stage;
			InputManager = inputManager;
			ResourceManager = resourceManager;
			PhysicsWorld = physicsWorld;

			GameObjects = new List<GameObject>();
			GameObjectsToAdd = new HashSet<GameObject>();
			GameObjectsToRemove = new HashSet<GameObject>();
		}

		public int NextId()
		{
			return LastId++;
		}

		public void Clear()
		{
			foreach (var gameOject in GameObjects)
			{
				gameOject.OnDetached();
			}

			GameObjects.Clear();
			GameObjectsById.Clear();
		}

		public GameObject CreateGameObject()
		{
			var gameObject = new GameObject(this, LastGameObjectId++);
			return gameObject;
		}

		public void Add(GameObject gameObject)
		{
			if (gameObject == null)
				throw new ArgumentNullException();

			GameObjectsToAdd.Add(gameObject);
		}

		public void Remove(GameObject gameObject)
		{
			if (gameObject == null)
				throw new ArgumentNullException();

			GameObjectsToRemove.Add(gameObject);
		}

		public void Update(float stepSize)
		{
			foreach (var gameObject in GameObjectsToAdd)
			{
				GameObjects.Add(gameObject);
				GameObjectsById.Add(gameObject.Id, gameObject);
				gameObject.OnAttached();
			}
			GameObjectsToAdd.Clear();

			foreach (var gameObject in GameObjectsToRemove)
			{
				if (GameObjects.Remove(gameObject))
				{
					GameObjectsById.Remove(gameObject.Id);
					gameObject.OnDetached();
				}
			}
			GameObjectsToRemove.Clear();

			foreach (var gameObject in GameObjects)
			{
				gameObject.Update(stepSize);
			}
		}
	}
}
