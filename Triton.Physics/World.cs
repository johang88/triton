using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jitter.Dynamics;
using Jitter.Collision.Shapes;
using Triton.Common;
using Triton.Graphics;

namespace Triton.Physics
{
	public class World
	{
		private readonly Jitter.World PhysicsWorld;
		private readonly List<Body> Bodies = new List<Body>();
		private readonly DebugDrawer DebugDrawer;

		private Vector3[] DebugColors = new Vector3[]
		{
			new	Vector3(1, 0, 0),
			new	Vector3(0, 1, 0),
			new	Vector3(0, 0, 1),
			new	Vector3(1, 1, 0),
			new	Vector3(1, 0, 1),
			new	Vector3(0, 1, 1),
		};

		public World(Backend backend, ResourceManager resourceManager)
		{
			var collisionSystem = new Jitter.Collision.CollisionSystemPersistentSAP();
			PhysicsWorld = new Jitter.World(collisionSystem);

			DebugDrawer = new DebugDrawer(backend, resourceManager);
		}

		public void Clear()
		{
			PhysicsWorld.Clear();
		}

		public void Update(float stepSize)
		{
			foreach (var body in Bodies)
			{
				body.Update();
			}

			PhysicsWorld.Step(stepSize, true);
		}

		RigidBody CreateRigidBody(Jitter.Collision.Shapes.Shape shape,  bool isStatic)
		{
			var body = new RigidBody(shape);
			body.IsActive = true;
			body.IsStatic = isStatic;
			body.EnableDebugDraw = true;

			return body;
		}

		public Body CreateSphereBody(float radius, Vector3 position, bool isStatic = false)
		{
			var shape = new SphereShape(radius);
			var rigidBody = CreateRigidBody(shape, isStatic);

			rigidBody.Position = Conversion.ToJitterVector(ref position);

			var body = new Body(rigidBody);
			rigidBody.Tag = body;

			Bodies.Add(body);

			PhysicsWorld.AddBody(rigidBody);

			return body;
		}

		public Body CreateBoxBody(float length, float height, float width, Vector3 position, bool isStatic = false)
		{
			var shape = new BoxShape(length, height, width);
			var rigidBody = CreateRigidBody(shape, isStatic);

			rigidBody.Position = Conversion.ToJitterVector(ref position);

			var body = new Body(rigidBody);
			rigidBody.Tag = body;

			Bodies.Add(body);

			PhysicsWorld.AddBody(rigidBody);

			return body;
		}

		public CharacterController CreateCharacterController(float length, float radius)
		{
			var shape = new CapsuleShape(length, radius);
			var rigidBody = CreateRigidBody(shape, false);
			rigidBody.EnableDebugDraw = false;

			PhysicsWorld.AddBody(rigidBody);

			var controller = new CharacterController(PhysicsWorld, rigidBody);
			rigidBody.Tag = controller;

			Bodies.Add(controller);

			return controller;
		}

		public void RemoveBody(Body body)
		{
			PhysicsWorld.RemoveBody(body.RigidBody);
			Bodies.Remove(body);
		}

		public void DrawDebugInfo(Camera camera)
		{
			var count = 0;
			foreach (RigidBody body in PhysicsWorld.RigidBodies)
			{
				DebugDrawer.Color = DebugColors[count % DebugColors.Length];
				body.DebugDraw(DebugDrawer);
				count++;
			}

			DebugDrawer.Render(camera);
		}
	}
}
