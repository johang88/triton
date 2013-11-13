using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jitter.Dynamics;
using Jitter.Collision.Shapes;

namespace Triton.Physics
{
	public class World
	{
		private readonly Jitter.World PhysicsWorld;
		private readonly List<Body> Bodies = new List<Body>();
		private int LastId = 0;

		public World()
		{
			var collisionSystem = new Jitter.Collision.CollisionSystemPersistentSAP();
			PhysicsWorld = new Jitter.World(collisionSystem);
		}

		private int GetNextId()
		{
			return LastId++;
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

		RigidBody CreateRigidBody(Jitter.Collision.Shapes.Shape shape, int id, bool isStatic)
		{
			var body = new RigidBody(shape);
			body.AllowDeactivation = false;
			body.IsActive = true;
			body.IsStatic = isStatic;
			body.Tag = id;

			return body;
		}

		public Body CreateSphereBody(float radius)
		{
			var id = GetNextId();
			var shape = new SphereShape(radius);
			var rigidBody = CreateRigidBody(shape, id, false);

			var body = new Body(rigidBody, id);
			rigidBody.Tag = body;

			Bodies.Add(body);

			PhysicsWorld.AddBody(rigidBody);

			return body;
		}

		public Body CreateBoxBody(float length, float height, float width, Vector3 position, bool isStatic = false)
		{
			var id = GetNextId();
			var shape = new BoxShape(length, height, width);
			var rigidBody = CreateRigidBody(shape, id, isStatic);

			rigidBody.Position = Conversion.ToJitterVector(ref position);

			var body = new Body(rigidBody, id);
			rigidBody.Tag = body;

			Bodies.Add(body);

			PhysicsWorld.AddBody(rigidBody);

			return body;
		}

		public void RemoveBody(Body body)
		{
			PhysicsWorld.RemoveBody(body.RigidBody);
			Bodies.Remove(body);
		}
	}
}
