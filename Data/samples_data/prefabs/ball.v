{
   "Position":"0 0 0",
   "Orientation":"0 0 0 1",
   "Scale":"1 1 1",
   "Children":[

   ],
   "Components":[
      {
         "$type":"Triton.Game.World.Components.MeshRenderer, Triton.Game",
         "CastShadows":true,
         "Mesh":"/models/sphere"
      },
      {
         "$type":"Triton.Game.World.Components.SphereRigidBody, Triton.Game",
		 "Radius": 0.5,
		 "IsStatic":false,
		 "CollisionLayer":2
      }
   ]
}