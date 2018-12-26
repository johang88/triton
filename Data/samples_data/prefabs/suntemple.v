{
   "Position":"0 0 0",
   "Orientation":"0 0 0 1",
   "Scale":"0.01 0.01 0.01",
   "Children":[{"$type":"Triton.GameObject, Triton","Position":"1.6393406391143799 2.450000047683716 70.8550033569336","Components":[{"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics","Color":"1 0.23529411852359772 0.0784313753247261","Intensity":40,"Range":32,"CastShadows":true,"Enabled":true}]},{"$type":"Triton.GameObject, Triton","Position":"-1.4549025297164917 2.450000047683716 70.8550033569336","Components":[{"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics","Color":"1 0.23529411852359772 0.0784313753247261","Intensity":40,"Range":32,"CastShadows":true,"Enabled":true}]},{"$type":"Triton.GameObject, Triton","Position":"-2.093762159347534 2.450000047683716 50.80758285522461","Components":[{"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics","Color":"1 0.23529411852359772 0.0784313753247261","Intensity":40,"Range":32,"CastShadows":true,"Enabled":true}]},{"$type":"Triton.GameObject, Triton","Position":"0 1.723734974861145 58.020023345947266","Components":[{"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics","Color":"1 0.0784313753247261 0","Intensity":40,"Range":32,"CastShadows":true,"Enabled":true}]},{"$type":"Triton.GameObject, Triton","Position":"-7.148975372314453 2.450000047683716 43.18354034423828","Components":[{"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics","Color":"1 0.23529411852359772 0.0784313753247261","Intensity":40,"Range":32,"CastShadows":true,"Enabled":true}]},{"$type":"Triton.GameObject, Triton","Position":"7.211925506591797 2.450000047683716 43.288002014160156","Components":[{"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics","Color":"1 0.23529411852359772 0.0784313753247261","Intensity":40,"Range":32,"CastShadows":true,"Enabled":true}]},{"$type":"Triton.GameObject, Triton","Position":"2.4240572452545166 2.450000047683716 28.55856704711914","Components":[{"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics","Color":"1 0.23529411852359772 0.0784313753247261","Intensity":40,"Range":32,"CastShadows":true,"Enabled":true}]},{"$type":"Triton.GameObject, Triton","Position":"7.45982027053833 4.4150004386901855 18.061458587646484","Components":[{"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics","Color":"1 0.23529411852359772 0.0784313753247261","Intensity":40,"Range":32,"CastShadows":true,"Enabled":true}]},{"$type":"Triton.GameObject, Triton","Position":"7.909985065460205 4.414999961853027 -2.1510772705078125","Components":[{"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics","Color":"1 0.23529411852359772 0.0784313753247261","Intensity":40,"Range":32,"CastShadows":true,"Enabled":true}]},{"$type":"Triton.GameObject, Triton","Position":"2.1134257316589355 4.414999961853027 -7.972902297973633","Components":[{"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics","Color":"1 0.23529411852359772 0.0784313753247261","Intensity":40,"Range":32,"CastShadows":true,"Enabled":true}]},{"$type":"Triton.GameObject, Triton","Position":"-7.98183012008667 4.414999961853027 -2.0562009811401367","Components":[{"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics","Color":"1 0.23529411852359772 0.0784313753247261","Intensity":40,"Range":32,"CastShadows":true,"Enabled":true}]},{"$type":"Triton.GameObject, Triton","Position":"-3.0991241931915283 4.414999961853027 7.863924026489258","Components":[{"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics","Color":"1 0.23529411852359772 0.0784313753247261","Intensity":40,"Range":32,"CastShadows":true,"Enabled":true}]},{"$type":"Triton.GameObject, Triton","Position":"-0.028223201632499695 1.624000072479248 31.539669036865234","Components":[{"$type":"Triton.Graphics.Components.LightComponent, Triton.Graphics","Color":"1 0.0784313753247261 0","Intensity":40,"Range":32,"CastShadows":true,"Enabled":true}]}],
   "Components":[
      {
         "$type":"Triton.Graphics.Components.MeshComponent, Triton.Graphics",
         "CastShadows":true,
         "Mesh":"/models/SunTemple"
      },
      {
         "$type": "Triton.Physics.Components.RigidBodyComponent, Triton.Physics",
		 "ColliderShape": {
           "$type": "Triton.Physics.Shapes.MeshColliderShape, Triton.Physics",
           "Mesh": "/collision/SunTemple"
         },
		 "RigidBodyType": 2
      }
   ]
}