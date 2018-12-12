{
   "Position":"0 0 0",
   "Orientation":"0 0 0 1",
   "Scale":"1 1 1",
   "Children":[
	  {
	    "$type":"Triton.GameObject, Triton",
		"Position": "2 2.5 3",
		"Components": [
			{
				"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics",
				"Color": "0.9 0.5 0.3",
				"Intensity": 5.0,
				"Range": 16,
				"CastShadows": false
			}
		]
	  },
	  {
	    "$type":"Triton.GameObject, Triton",
		"Position": "-10 2.5 1",
		"Components": [
			{
				"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics",
				"Color": "0.5 0.9 0.3",
				"Intensity": 5.0,
				"Range": 32,
				"CastShadows": false
			}
		]
	  },
	  {
	    "$type":"Triton.GameObject, Triton",
		"Position": "-18 2.5 -5",
		"Components": [
			{
				"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics",
				"Color": "0.3 0.5 0.9",
				"Intensity": 5.0,
				"Range": 32,
				"CastShadows": false
			}
		]
	  },
	  {
	    "$type":"Triton.GameObject, Triton",
		"Position": "0 1 0",
		"Components": [
			{
				"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics",
				"Color": "0.3 0.5 0.9",
				"Intensity": 60.0,
				"Range": 64,
				"CastShadows": true,
				"Type": "SpotLight",
				"InnerAngle": 0.8,
				"OuterAngle": 0.9
			}
		]
	  },
	  {
	    "$type":"Triton.GameObject, Triton",
		"Position": "0 1 0",
		"Orientation": "0 1 0 -4.3711",
		"Components": [
			{
				"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics",
				"Color": "0.8 0.5 0.9",
				"Intensity": 30.0,
				"Range": 64,
				"CastShadows": true,
				"Type": "SpotLight",
				"InnerAngle": 0.8,
				"OuterAngle": 0.9
			}
		]
	  }
   ],
   "Components":[
      {
         "$type":"Triton.Graphics.Components.MeshComponent, Triton.Graphics",
         "CastShadows":true,
         "Mesh":"/models/room"
      },
      {
         "$type": "Triton.Physics.Components.RigidBodyComponent, Triton.Physics",
		 "ColliderShape": {
           "$type": "Triton.Physics.Shapes.MeshColliderShape, Triton.Physics",
           "Mesh": "/collision/room"
         },
		 "RigidBodyType": 2
      }
   ]
}