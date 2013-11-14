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
		private readonly List<GameObject> GameObjects = new List<GameObject>();
		private readonly HashSet<GameObject> GameObjectsToAdd = new HashSet<GameObject>();
		private readonly HashSet<GameObject> GameObjectsToRemove = new HashSet<GameObject>();

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
		}

		/// <summary>
		/// Removes all game objects from the world.
		/// They will be instantly removed, this function is not safe to call in <see cref="GameObject.Update"/> or <see cref="IComponent.Update"/>
		/// </summary>
		public void Clear()
		{
			foreach (var gameOject in GameObjects)
			{
				gameOject.OnDetached();
			}

			GameObjects.Clear();
			GameObjectsById.Clear();
		}

		/// <summary>
		/// Creates a new empty game object, the game object has to be added to the world using <see cref="GameWorld.Add"/>
		/// </summary>
		/// <returns></returns>
		public GameObject CreateGameObject()
		{
			var gameObject = new GameObject(this, LastGameObjectId++);
			return gameObject;
		}

		/// <summary>
		/// Add a game object to the world, the game object will be added in the next frame.
		/// </summary>
		/// <param name="gameObject"></param>
		public void Add(GameObject gameObject)
		{
			if (gameObject == null)
				throw new ArgumentNullException();

			GameObjectsToAdd.Add(gameObject);
		}

		/// <summary>
		/// Removes a game object from the world, the game object will be removed in the next frame.
		/// </summary>
		/// <param name="gameObject"></param>
		public void Remove(GameObject gameObject)
		{
			if (gameObject == null)
				throw new ArgumentNullException();

			GameObjectsToRemove.Add(gameObject);
		}

		/// <summary>
		/// Update all active game objects and their components
		/// Any game objects pending for removal / adding will be removed / added.
		/// </summary>
		/// <param name="stepSize"></param>
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
