using FactoryKing.Core.Content;
using FactoryKing.Core.Simulation;
using FactoryKing.Runtime.Procedural;
using UnityEngine;

namespace FactoryKing.Runtime.Visuals
{
    /// <summary>
    /// Builds a stylised 3D machine for each <see cref="MachineVisual"/> archetype. Local frame: +X = flow direction
    /// (input arrives from -X, output leaves at +X), origin on the floor. Footprint is about 2.8 x 2.4 units.
    /// </summary>
    public sealed class MachineModelFactory
    {
        public const float BeltHeight = 0.55f;
        public const float HalfLength = 1.4f;

        private readonly ModelKit _kit;

        public MachineModelFactory(ModelKit kit) => _kit = kit;

        public MachineView Build(Transform parent, MachineRef r, MachineDef def, Material inputMat, Material outputMat)
        {
            var root = new GameObject(def.Id);
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<MachineView>();
            var scaleRoot = ModelKit.Pivot(root.transform, "Model", Vector3.zero);
            view.ScaleRoot = scaleRoot;

            // Selection collider on the root (cheap box, no mesh collider).
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.1f, 0);
            col.size = new Vector3(2.9f, 2.2f, 2.6f);

            _kit.Part(scaleRoot, "Plinth", Vector3.zero, Plinth);
            view.TierBand = _kit.Solid(scaleRoot, "TierBand", Vector3.zero, _kit.Materials.Tier(0),
                b => b.Box(new Vector3(0, 0.16f, 1.16f), new Vector3(2.5f, 0.07f, 0.04f), 0.01f));

            switch (def.Visual)
            {
                case MachineVisual.Unloader: Unloader(view, scaleRoot, outputMat); break;
                case MachineVisual.Silo: Silo(view, scaleRoot); break;
                case MachineVisual.Pump: Pump(view, scaleRoot); break;
                case MachineVisual.Crusher: Crusher(view, scaleRoot, outputMat); break;
                case MachineVisual.Furnace: Furnace(view, scaleRoot); break;
                case MachineVisual.Oven: Oven(view, scaleRoot); break;
                case MachineVisual.Extruder: Extruder(view, scaleRoot, outputMat); break;
                case MachineVisual.Mill: Mill(view, scaleRoot); break;
                case MachineVisual.Cutter: Cutter(view, scaleRoot, inputMat); break;
                case MachineVisual.Press: Press(view, scaleRoot); break;
                case MachineVisual.Loom: Loom(view, scaleRoot, outputMat); break;
                case MachineVisual.RobotArm: RobotArm(view, scaleRoot, inputMat); break;
                case MachineVisual.Packer: Packer(view, scaleRoot); break;
                case MachineVisual.Warehouse: Warehouse(view, scaleRoot, outputMat); break;
                case MachineVisual.Dock: Dock(view, scaleRoot, outputMat); break;
            }

            // Status beacon on a short mast at the back-left corner.
            _kit.Part(scaleRoot, "BeaconMast", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Metal;
                b.Cylinder(new Vector3(-1.15f, 0.12f, 0.95f), 0.035f, 2.35f, 6);
            }, false);
            view.StatusLight = _kit.Solid(scaleRoot, "Beacon", new Vector3(-1.15f, 2.47f, 0.95f), _kit.Materials.Status(0),
                b => b.Cylinder(Vector3.zero, 0.09f, 0.16f, 10));

            view.Init(r, def, _kit.Materials);
            return view;
        }

        // ---------------------------------------------------------------- common pieces

        private static void Plinth(MeshBuilder b)
        {
            b.Slot = (int)Mat.Concrete;
            b.Box(new Vector3(0, 0.06f, 0), new Vector3(2.8f, 0.12f, 2.4f), 0.04f);
            b.Slot = (int)Mat.Hazard;
            b.Box(new Vector3(0, 0.125f, -1.16f), new Vector3(2.6f, 0.02f, 0.06f), 0f);
        }

        private static void Legs(MeshBuilder b, Vector3 center, Vector2 half, float height, float thickness = 0.1f)
        {
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                b.Box(center + new Vector3(sx * half.x, height * 0.5f, sz * half.y), new Vector3(thickness, height, thickness), 0.015f);
        }

        /// <summary>Short in/out belt stubs so conveyors visually enter and leave the machine.</summary>
        private static void Ports(MeshBuilder b, bool input = true, bool output = true)
        {
            b.Slot = (int)Mat.Metal;
            if (input) b.Box(new Vector3(-1.25f, BeltHeight - 0.08f, 0), new Vector3(0.3f, 0.14f, 0.7f), 0.02f);
            if (output) b.Box(new Vector3(1.25f, BeltHeight - 0.08f, 0), new Vector3(0.3f, 0.14f, 0.7f), 0.02f);
        }

        private AnimatedPart Animate(MachineView v, Transform t, PartMotion motion, Vector3 axis, float amount, float phase = 0f)
        {
            var p = new AnimatedPart { Transform = t, Motion = motion, Axis = axis, Amount = amount, Phase = phase };
            v.Parts.Add(p);
            return p;
        }

        private void AddSmoke(MachineView v, Transform parent, Vector3 pos)
        {
            var go = new GameObject("Smoke");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = 12;
            main.startLifetime = 2.6f;
            main.startSpeed = 0.6f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startColor = new Color(0.92f, 0.92f, 0.95f, 0.55f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.08f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 2.2f));
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0.55f, 0), new GradientAlphaKey(0f, 1) });
            color.color = g;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = ParticleMaterials.Smoke;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            v.Smoke = ps;
        }

        // ---------------------------------------------------------------- raw input

        private void Unloader(MachineView v, Transform root, Material raw)
        {
            _kit.Part(root, "Unloader.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Metal;
                Legs(b, new Vector3(-0.3f, 0.12f, 0), new Vector2(0.55f, 0.55f), 1.25f);
                b.Slot = (int)Mat.Body;
                b.PushTRS(new Vector3(-0.3f, 2.25f, 0), Quaternion.Euler(180, 0, 0), Vector3.one);
                b.Frustum(Vector3.zero, 0.85f, 0.25f, 0.9f, 4, true, true, false); // inverted pyramid hopper
                b.PopMatrix();
                b.Slot = (int)Mat.Accent;
                b.Box(new Vector3(-0.3f, 2.3f, 0), new Vector3(1.3f, 0.1f, 1.3f), 0.03f);
                // inclined feed belt to the output
                b.Slot = (int)Mat.Metal;
                b.PushTRS(new Vector3(0.65f, 0.95f, 0), Quaternion.Euler(0, 0, -28f), Vector3.one);
                b.Box(Vector3.zero, new Vector3(1.4f, 0.12f, 0.6f), 0.02f);
                b.PopMatrix();
                Ports(b, input: false);
            });
            _kit.Solid(root, "Unloader.Pile", new Vector3(-0.3f, 2.33f, 0), raw, b =>
            {
                b.Frustum(Vector3.zero, 0.55f, 0.1f, 0.28f, 8, true, false, false);
            });
            var drum = _kit.Part(root, "Unloader.Drum", new Vector3(-0.3f, 1.3f, 0), b =>
            {
                b.Slot = (int)Mat.Chrome;
                b.CylinderAlong(new Vector3(0, 0, -0.32f), new Vector3(0, 0, 0.32f), 0.16f, 12);
            }, false);
            Animate(v, drum.transform, PartMotion.Spin, Vector3.forward, 220f);
            Animate(v, root, PartMotion.Shake, Vector3.up, 0.004f);
        }

        private void Silo(MachineView v, Transform root)
        {
            _kit.Part(root, "Silo.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Metal;
                Legs(b, new Vector3(-0.35f, 0.12f, 0), new Vector2(0.55f, 0.55f), 0.7f, 0.12f);
                b.Slot = (int)Mat.Chrome;
                b.Frustum(new Vector3(-0.35f, 0.8f, 0), 0.25f, 0.8f, 0.45f, 18, false, true);
                b.Slot = (int)Mat.Body;
                b.Cylinder(new Vector3(-0.35f, 1.25f, 0), 0.8f, 1.7f, 18, false);
                b.Slot = (int)Mat.Accent;
                b.Cylinder(new Vector3(-0.35f, 1.9f, 0), 0.82f, 0.12f, 18, false);
                b.Frustum(new Vector3(-0.35f, 2.95f, 0), 0.84f, 0.12f, 0.45f, 18);
                b.Slot = (int)Mat.Metal;
                b.Box(new Vector3(-1.13f, 1.9f, -0.2f), new Vector3(0.05f, 2.2f, 0.05f), 0f); // ladder rails
                b.Box(new Vector3(-1.13f, 1.9f, 0.1f), new Vector3(0.05f, 2.2f, 0.05f), 0f);
                for (int i = 0; i < 9; i++) b.Box(new Vector3(-1.13f, 0.95f + i * 0.24f, -0.05f), new Vector3(0.04f, 0.03f, 0.3f), 0f);
                // auger housing to output
                b.CylinderAlong(new Vector3(-0.2f, 0.75f, 0), new Vector3(1.2f, BeltHeight, 0), 0.14f, 10);
                Ports(b, input: false);
            });
            var auger = _kit.Part(root, "Silo.Auger", new Vector3(0.95f, BeltHeight + 0.01f, 0), b =>
            {
                b.Slot = (int)Mat.Hazard;
                for (int i = 0; i < 4; i++)
                {
                    b.PushTRS(Vector3.zero, Quaternion.Euler(i * 45f, 0, 0), Vector3.one);
                    b.Box(Vector3.zero, new Vector3(0.05f, 0.34f, 0.05f), 0f);
                    b.PopMatrix();
                }
            }, false);
            Animate(v, auger.transform, PartMotion.Spin, Vector3.right, 360f);
        }

        private void Pump(MachineView v, Transform root)
        {
            _kit.Part(root, "Pump.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Metal;
                b.Box(new Vector3(-0.2f, 0.3f, 0), new Vector3(1.9f, 0.36f, 0.9f), 0.04f);
                b.Slot = (int)Mat.Body;
                // A-frame samson post
                b.PushTRS(new Vector3(-0.2f, 0.45f, 0.3f), Quaternion.Euler(-12, 0, 0), Vector3.one);
                b.Box(new Vector3(0, 0.75f, 0), new Vector3(0.14f, 1.5f, 0.14f), 0.02f);
                b.PopMatrix();
                b.PushTRS(new Vector3(-0.2f, 0.45f, -0.3f), Quaternion.Euler(12, 0, 0), Vector3.one);
                b.Box(new Vector3(0, 0.75f, 0), new Vector3(0.14f, 1.5f, 0.14f), 0.02f);
                b.PopMatrix();
                b.Slot = (int)Mat.Accent;
                b.Box(new Vector3(0.75f, 0.75f, 0), new Vector3(0.55f, 0.5f, 0.6f), 0.05f); // motor
                b.Slot = (int)Mat.Chrome;
                b.CylinderAlong(new Vector3(-1.1f, 0.12f, 0), new Vector3(-1.1f, 0.6f, 0), 0.07f, 8); // wellhead
                b.CylinderAlong(new Vector3(-1.1f, 0.3f, 0), new Vector3(1.2f, BeltHeight, 0.0f), 0.06f, 8); // flow line
                Ports(b, input: false);
            });
            var beam = ModelKit.Pivot(root, "Pump.Beam", new Vector3(-0.2f, 1.95f, 0));
            _kit.Part(beam, "Pump.BeamMesh", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Body;
                b.Box(new Vector3(0, 0.05f, 0), new Vector3(2.0f, 0.16f, 0.16f), 0.02f);
                b.Slot = (int)Mat.Accent;
                b.Box(new Vector3(-1.0f, -0.05f, 0), new Vector3(0.22f, 0.5f, 0.24f), 0.04f); // horse head
                b.Slot = (int)Mat.Metal;
                b.Box(new Vector3(0.95f, -0.15f, 0), new Vector3(0.3f, 0.3f, 0.3f), 0.03f);
            });
            Animate(v, beam, PartMotion.Swing, Vector3.forward, 14f);
            var crank = _kit.Part(root, "Pump.Crank", new Vector3(0.75f, 0.75f, 0.36f), b =>
            {
                b.Slot = (int)Mat.Hazard;
                b.Box(new Vector3(0.2f, 0, 0), new Vector3(0.5f, 0.12f, 0.05f), 0.01f);
            }, false);
            Animate(v, crank.transform, PartMotion.Spin, Vector3.forward, -200f);
        }

        private void Crusher(MachineView v, Transform root, Material raw)
        {
            _kit.Part(root, "Crusher.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Body;
                b.Box(new Vector3(0, 0.75f, 0), new Vector3(1.9f, 1.25f, 1.6f), 0.08f);
                b.Slot = (int)Mat.Accent;
                b.Box(new Vector3(0, 1.4f, 0), new Vector3(2.0f, 0.1f, 1.7f), 0.03f);
                b.Slot = (int)Mat.Metal;
                b.PushTRS(new Vector3(0, 1.95f, 0), Quaternion.Euler(180, 45, 0), Vector3.one);
                b.Frustum(Vector3.zero, 0.95f, 0.5f, 0.5f, 4, false, false, false);
                b.PopMatrix();
                b.Slot = (int)Mat.Glass;
                b.Box(new Vector3(0, 0.85f, -0.81f), new Vector3(1.1f, 0.45f, 0.02f), 0f);
                Ports(b, input: false);
            });
            _kit.Solid(root, "Crusher.Bale", new Vector3(0, 1.62f, 0), raw, b => b.Box(Vector3.zero, new Vector3(0.7f, 0.3f, 0.7f), 0.12f));
            for (int i = 0; i < 2; i++)
            {
                var roller = _kit.Part(root, "Crusher.Roller", new Vector3(i == 0 ? -0.3f : 0.3f, 1.5f, -0.86f), b =>
                {
                    b.Slot = (int)Mat.Chrome;
                    b.CylinderAlong(new Vector3(0, 0, -0.04f), new Vector3(0, 0, 0.04f), 0.22f, 10);
                    b.Slot = (int)Mat.Hazard;
                    b.Box(Vector3.zero, new Vector3(0.42f, 0.05f, 0.1f), 0f);
                }, false);
                Animate(v, roller.transform, PartMotion.Spin, Vector3.forward, i == 0 ? 300f : -300f);
            }
        }

        // ---------------------------------------------------------------- processing

        private void Furnace(MachineView v, Transform root)
        {
            _kit.Part(root, "Furnace.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Body;
                b.Box(new Vector3(-0.15f, 1.0f, 0), new Vector3(1.8f, 1.8f, 1.7f), 0.12f);
                b.Slot = (int)Mat.Metal;
                b.Box(new Vector3(-0.15f, 1.95f, 0), new Vector3(1.95f, 0.14f, 1.85f), 0.04f);
                b.Frustum(new Vector3(-0.55f, 2.0f, 0.35f), 0.3f, 0.22f, 1.5f, 12, false);
                b.Slot = (int)Mat.Accent;
                b.Cylinder(new Vector3(-0.55f, 3.2f, 0.35f), 0.25f, 0.12f, 12, false);
                b.Slot = (int)Mat.Chrome;
                for (int i = 0; i < 3; i++) b.Box(new Vector3(-0.15f, 0.45f + i * 0.55f, -0.86f), new Vector3(1.85f, 0.06f, 0.04f), 0.01f);
                b.Slot = (int)Mat.Metal;
                b.Box(new Vector3(-0.15f, 0.85f, -0.87f), new Vector3(0.8f, 0.7f, 0.04f), 0.02f); // door frame
                Ports(b);
            });
            var glow = _kit.Part(root, "Furnace.Glow", new Vector3(-0.15f, 0.85f, -0.89f), b =>
            {
                b.Slot = (int)Mat.Glow;
                b.Box(Vector3.zero, new Vector3(0.6f, 0.5f, 0.02f), 0f);
            }, false);
            Animate(v, glow.transform, PartMotion.Pulse, Vector3.one, 0.08f);
            var ladle = ModelKit.Pivot(root, "Furnace.Ladle", new Vector3(0.95f, 0.95f, 0));
            _kit.Part(ladle, "Furnace.LadleMesh", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Metal;
                b.Frustum(new Vector3(0, -0.3f, 0), 0.18f, 0.26f, 0.35f, 10, false, true);
                b.Slot = (int)Mat.Glow;
                b.Cylinder(new Vector3(0, 0.03f, 0), 0.23f, 0.02f, 10);
            }, false);
            Animate(v, ladle, PartMotion.Swing, Vector3.forward, -22f);
            AddSmoke(v, root, new Vector3(-0.55f, 3.35f, 0.35f));
        }

        private void Oven(MachineView v, Transform root)
        {
            _kit.Part(root, "Oven.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Body;
                b.Box(new Vector3(0, 0.95f, 0), new Vector3(2.4f, 1.0f, 1.5f), 0.1f);
                b.Slot = (int)Mat.Chrome;
                b.Box(new Vector3(0, 1.5f, 0), new Vector3(2.45f, 0.1f, 1.55f), 0.03f);
                b.Slot = (int)Mat.Metal;
                Legs(b, Vector3.zero, new Vector2(1.05f, 0.6f), 0.5f);
                b.Cylinder(new Vector3(0.6f, 1.55f, 0.3f), 0.14f, 0.9f, 10);
                b.Slot = (int)Mat.Accent;
                b.Box(new Vector3(-0.85f, 1.25f, -0.76f), new Vector3(0.45f, 0.35f, 0.04f), 0.02f); // control panel
                Ports(b);
            });
            var window = _kit.Part(root, "Oven.Window", new Vector3(0.2f, 0.95f, -0.77f), b =>
            {
                b.Slot = (int)Mat.Glow;
                b.Box(Vector3.zero, new Vector3(1.4f, 0.28f, 0.02f), 0f);
            }, false);
            Animate(v, window.transform, PartMotion.Pulse, Vector3.one, 0.05f);
            for (int i = 0; i < 3; i++)
            {
                var roller = _kit.Part(root, "Oven.Roller", new Vector3(-0.9f + i * 0.9f, 0.5f, -0.6f), b =>
                {
                    b.Slot = (int)Mat.Chrome;
                    b.CylinderAlong(new Vector3(0, 0, -0.08f), new Vector3(0, 0, 0.08f), 0.08f, 8);
                }, false);
                Animate(v, roller.transform, PartMotion.Spin, Vector3.forward, -240f);
            }
            AddSmoke(v, root, new Vector3(0.6f, 2.5f, 0.3f));
        }

        private void Extruder(MachineView v, Transform root, Material product)
        {
            _kit.Part(root, "Extruder.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Metal;
                b.Box(new Vector3(0, 0.4f, 0), new Vector3(2.4f, 0.55f, 1.0f), 0.05f);
                b.Slot = (int)Mat.Body;
                b.CylinderAlong(new Vector3(-0.9f, 0.95f, 0), new Vector3(0.9f, 0.95f, 0), 0.3f, 16);
                b.Slot = (int)Mat.Accent;
                for (int i = 0; i < 4; i++) b.CylinderAlong(new Vector3(-0.7f + i * 0.45f, 0.95f, 0), new Vector3(-0.6f + i * 0.45f, 0.95f, 0), 0.34f, 16);
                b.Box(new Vector3(-1.0f, 1.0f, 0), new Vector3(0.45f, 0.7f, 0.7f), 0.06f); // gearbox
                b.Slot = (int)Mat.Chrome;
                b.PushTRS(new Vector3(-0.5f, 1.85f, 0), Quaternion.Euler(180, 45, 0), Vector3.one);
                b.Frustum(Vector3.zero, 0.42f, 0.12f, 0.6f, 4, true, false, false);
                b.PopMatrix();
                Ports(b);
            });
            var gear = _kit.Part(root, "Extruder.Gear", new Vector3(-1.0f, 1.0f, -0.37f), b =>
            {
                b.Slot = (int)Mat.Hazard;
                b.CylinderAlong(new Vector3(0, 0, -0.03f), new Vector3(0, 0, 0.03f), 0.22f, 10);
                b.Slot = (int)Mat.Metal;
                b.Box(Vector3.zero, new Vector3(0.5f, 0.06f, 0.08f), 0f);
            }, false);
            Animate(v, gear.transform, PartMotion.Spin, Vector3.forward, 280f);
            var strand = _kit.Solid(root, "Extruder.Strand", new Vector3(1.05f, 0.95f, 0), product, b =>
                b.CylinderAlong(Vector3.zero, new Vector3(0.25f, 0, 0), 0.08f, 8));
            Animate(v, strand.transform, PartMotion.Shuttle, Vector3.right, 0.12f);
        }

        private void Mill(MachineView v, Transform root)
        {
            _kit.Part(root, "Mill.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Body;
                b.Box(new Vector3(0.1f, 0.85f, 0.15f), new Vector3(1.7f, 1.45f, 1.3f), 0.1f);
                b.Slot = (int)Mat.Wood;
                b.Frustum(new Vector3(0.1f, 1.57f, 0.15f), 0.95f, 0.55f, 0.55f, 4, true, false, false);
                b.Slot = (int)Mat.Accent;
                b.Box(new Vector3(0.1f, 1.6f, 0.15f), new Vector3(1.8f, 0.08f, 1.4f), 0.02f);
                b.Slot = (int)Mat.Metal;
                b.CylinderAlong(new Vector3(-0.6f, 1.0f, -0.62f), new Vector3(-0.6f, 1.0f, -0.4f), 0.07f, 8);
                Ports(b);
            });
            var wheel = _kit.Part(root, "Mill.Wheel", new Vector3(-0.6f, 1.0f, -0.68f), b =>
            {
                b.Slot = (int)Mat.Wood;
                b.CylinderAlong(new Vector3(0, 0, -0.06f), new Vector3(0, 0, 0.06f), 0.62f, 18);
                b.Slot = (int)Mat.Chrome;
                for (int i = 0; i < 4; i++)
                {
                    b.PushTRS(new Vector3(0, 0, -0.07f), Quaternion.Euler(0, 0, i * 45f), Vector3.one);
                    b.Box(Vector3.zero, new Vector3(1.15f, 0.07f, 0.03f), 0f);
                    b.PopMatrix();
                }
            });
            Animate(v, wheel.transform, PartMotion.Spin, Vector3.forward, 120f);
        }

        // ---------------------------------------------------------------- production

        private void Cutter(MachineView v, Transform root, Material workpiece)
        {
            _kit.Part(root, "Cutter.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Metal;
                Legs(b, Vector3.zero, new Vector2(1.05f, 0.75f), 0.75f, 0.12f);
                b.Slot = (int)Mat.Body;
                b.Box(new Vector3(0, 0.82f, 0), new Vector3(2.4f, 0.16f, 1.7f), 0.05f);
                b.Slot = (int)Mat.Hazard;
                b.Box(new Vector3(0, 0.92f, -0.82f), new Vector3(2.4f, 0.04f, 0.06f), 0f);
                b.Slot = (int)Mat.Chrome;
                b.Box(new Vector3(0, 0.93f, 0.85f), new Vector3(2.4f, 0.06f, 0.06f), 0f); // gantry rail
                b.Box(new Vector3(0, 0.93f, -0.7f), new Vector3(2.4f, 0.06f, 0.06f), 0f);
                Ports(b);
            });
            _kit.Solid(root, "Cutter.Workpiece", new Vector3(0, 0.98f, 0), workpiece, b => b.Box(Vector3.zero, new Vector3(1.6f, 0.12f, 0.45f), 0.03f));
            var gantry = ModelKit.Pivot(root, "Cutter.Gantry", new Vector3(-0.85f, 0, 0));
            _kit.Part(gantry, "Cutter.GantryMesh", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Accent;
                b.Box(new Vector3(0, 1.4f, -0.72f), new Vector3(0.18f, 1.0f, 0.18f), 0.03f);
                b.Box(new Vector3(0, 1.4f, 0.85f), new Vector3(0.18f, 1.0f, 0.18f), 0.03f);
                b.Box(new Vector3(0, 1.9f, 0.06f), new Vector3(0.24f, 0.2f, 1.85f), 0.04f);
                b.Slot = (int)Mat.Metal;
                b.Box(new Vector3(0, 1.55f, 0), new Vector3(0.3f, 0.45f, 0.3f), 0.04f);
            });
            Animate(v, gantry, PartMotion.Shuttle, Vector3.right, 1.7f);
            var blade = _kit.Part(gantry, "Cutter.Blade", new Vector3(0, 1.18f, 0), b =>
            {
                b.Slot = (int)Mat.Chrome;
                b.CylinderAlong(new Vector3(0, 0, -0.02f), new Vector3(0, 0, 0.02f), 0.24f, 16);
            }, false);
            Animate(v, blade.transform, PartMotion.Spin, Vector3.forward, -900f);
        }

        private void Press(MachineView v, Transform root)
        {
            _kit.Part(root, "Press.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Metal;
                b.Box(new Vector3(0, 0.45f, 0), new Vector3(1.9f, 0.7f, 1.5f), 0.06f);
                b.Slot = (int)Mat.Body;
                b.Box(new Vector3(-0.75f, 1.55f, 0), new Vector3(0.3f, 1.5f, 1.3f), 0.06f);
                b.Box(new Vector3(0.75f, 1.55f, 0), new Vector3(0.3f, 1.5f, 1.3f), 0.06f);
                b.Box(new Vector3(0, 2.45f, 0), new Vector3(1.95f, 0.45f, 1.45f), 0.08f);
                b.Slot = (int)Mat.Accent;
                b.Box(new Vector3(0, 2.72f, 0), new Vector3(1.4f, 0.1f, 1.0f), 0.03f);
                b.Slot = (int)Mat.Chrome;
                b.Box(new Vector3(0, 0.83f, 0), new Vector3(1.0f, 0.06f, 0.9f), 0.01f); // die
                b.Slot = (int)Mat.Hazard;
                b.Box(new Vector3(0, 0.6f, -0.76f), new Vector3(1.9f, 0.08f, 0.02f), 0f);
                Ports(b);
            });
            var ram = _kit.Part(root, "Press.Ram", new Vector3(0, 1.95f, 0), b =>
            {
                b.Slot = (int)Mat.Chrome;
                b.Cylinder(new Vector3(0, 0.0f, 0), 0.12f, 0.45f, 10);
                b.Slot = (int)Mat.Metal;
                b.Box(new Vector3(0, -0.05f, 0), new Vector3(1.0f, 0.18f, 0.9f), 0.03f);
            });
            Animate(v, ram.transform, PartMotion.Stroke, Vector3.down, 0.9f);
        }

        private void Loom(MachineView v, Transform root, Material cloth)
        {
            _kit.Part(root, "Loom.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Wood;
                b.Box(new Vector3(-1.0f, 0.95f, 0), new Vector3(0.18f, 1.65f, 1.6f), 0.04f);
                b.Box(new Vector3(1.0f, 0.95f, 0), new Vector3(0.18f, 1.65f, 1.6f), 0.04f);
                b.Box(new Vector3(0, 1.75f, 0), new Vector3(2.2f, 0.14f, 0.2f), 0.03f);
                b.Slot = (int)Mat.Body;
                b.Box(new Vector3(0, 0.5f, 0.55f), new Vector3(2.0f, 0.6f, 0.4f), 0.05f);
                b.Slot = (int)Mat.Chrome;
                for (int i = 0; i < 9; i++) b.Box(new Vector3(-0.8f + i * 0.2f, 1.2f, 0.05f), new Vector3(0.015f, 1.0f, 0.015f), 0f); // warp threads
                Ports(b);
            });
            var beater = ModelKit.Pivot(root, "Loom.Beater", new Vector3(0, 0.6f, -0.1f));
            _kit.Part(beater, "Loom.BeaterMesh", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Accent;
                b.Box(new Vector3(0, 0.6f, 0), new Vector3(1.9f, 0.1f, 0.1f), 0.02f);
                b.Box(new Vector3(-0.9f, 0.3f, 0), new Vector3(0.07f, 0.6f, 0.07f), 0f);
                b.Box(new Vector3(0.9f, 0.3f, 0), new Vector3(0.07f, 0.6f, 0.07f), 0f);
            });
            Animate(v, beater, PartMotion.Swing, Vector3.right, 16f);
            var roll = _kit.Solid(root, "Loom.Roll", new Vector3(0, 0.7f, -0.55f), cloth, b =>
                b.CylinderAlong(new Vector3(-0.9f, 0, 0), new Vector3(0.9f, 0, 0), 0.2f, 14), true);
            Animate(v, roll.transform, PartMotion.Spin, Vector3.right, -90f);
        }

        private void RobotArm(MachineView v, Transform root, Material part)
        {
            _kit.Part(root, "Robot.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Metal;
                b.Box(new Vector3(-0.85f, 0.55f, -0.35f), new Vector3(0.8f, 0.85f, 0.9f), 0.05f); // input table
                b.Box(new Vector3(0.85f, 0.55f, -0.35f), new Vector3(0.8f, 0.85f, 0.9f), 0.05f); // output table
                b.Slot = (int)Mat.Body;
                b.Cylinder(new Vector3(0, 0.12f, 0.45f), 0.42f, 0.4f, 16);
                b.Slot = (int)Mat.Glass;
                b.Box(new Vector3(0, 1.3f, 1.15f), new Vector3(2.6f, 2.2f, 0.04f), 0f); // safety screen
                b.Slot = (int)Mat.Hazard;
                b.Box(new Vector3(0, 0.15f, 1.12f), new Vector3(2.6f, 0.06f, 0.08f), 0f);
                Ports(b);
            });
            _kit.Solid(root, "Robot.InPart", new Vector3(-0.85f, 1.02f, -0.35f), part, b => b.Box(Vector3.zero, new Vector3(0.4f, 0.06f, 0.3f), 0.01f));

            var yaw = ModelKit.Pivot(root, "Robot.Yaw", new Vector3(0, 0.52f, 0.45f));
            _kit.Part(yaw, "Robot.Turret", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Accent;
                b.Cylinder(Vector3.zero, 0.3f, 0.28f, 14);
            });
            var shoulder = ModelKit.Pivot(yaw, "Robot.Shoulder", new Vector3(0, 0.3f, 0));
            _kit.Part(shoulder, "Robot.UpperArm", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Accent;
                b.Box(new Vector3(0, 0.45f, 0), new Vector3(0.2f, 0.95f, 0.22f), 0.05f);
                b.Slot = (int)Mat.Metal;
                b.CylinderAlong(new Vector3(0, 0, -0.16f), new Vector3(0, 0, 0.16f), 0.13f, 12);
            });
            var elbow = ModelKit.Pivot(shoulder, "Robot.Elbow", new Vector3(0, 0.9f, 0));
            _kit.Part(elbow, "Robot.Forearm", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Accent;
                b.Box(new Vector3(0.0f, 0.38f, 0), new Vector3(0.16f, 0.8f, 0.18f), 0.04f);
                b.Slot = (int)Mat.Metal;
                b.CylinderAlong(new Vector3(0, 0, -0.13f), new Vector3(0, 0, 0.13f), 0.1f, 10);
                b.Slot = (int)Mat.Chrome;
                b.Box(new Vector3(0, 0.82f, 0), new Vector3(0.24f, 0.08f, 0.1f), 0.01f);
                b.Box(new Vector3(-0.09f, 0.9f, 0), new Vector3(0.04f, 0.14f, 0.08f), 0f);
                b.Box(new Vector3(0.09f, 0.9f, 0), new Vector3(0.04f, 0.14f, 0.08f), 0f);
            });
            // Arm reaches toward -Z (over the tables) by orienting the yaw pivot.
            yaw.localRotation = Quaternion.identity;
            v.ArmYaw = yaw;
            v.ArmShoulder = shoulder;
            v.ArmElbow = elbow;
        }

        private void Packer(MachineView v, Transform root)
        {
            _kit.Part(root, "Packer.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Body;
                b.Box(new Vector3(0, 0.95f, 0.1f), new Vector3(1.8f, 1.6f, 1.6f), 0.1f);
                b.Slot = (int)Mat.White;
                b.Box(new Vector3(0, 1.8f, 0.1f), new Vector3(1.85f, 0.1f, 1.65f), 0.03f);
                b.Slot = (int)Mat.Glass;
                b.Box(new Vector3(0, 1.05f, -0.71f), new Vector3(1.2f, 0.7f, 0.02f), 0f);
                b.Slot = (int)Mat.Accent;
                b.Box(new Vector3(0.65f, 1.55f, -0.72f), new Vector3(0.35f, 0.25f, 0.04f), 0.02f);
                b.Slot = (int)Mat.Wood;
                b.Box(new Vector3(0, 0.62f, -0.4f), new Vector3(0.55f, 0.35f, 0.45f), 0.02f); // carton
                Ports(b);
            });
            for (int s = -1; s <= 1; s += 2)
            {
                var flap = ModelKit.Pivot(root, "Packer.Flap", new Vector3(s * 0.27f, 0.8f, -0.4f));
                _kit.Part(flap, "Packer.FlapMesh" + s, Vector3.zero, b =>
                {
                    b.Slot = (int)Mat.Wood;
                    b.Box(new Vector3(s * -0.13f, 0, 0), new Vector3(0.26f, 0.02f, 0.44f), 0f);
                }, false);
                Animate(v, flap, PartMotion.Swing, Vector3.forward, s * 40f, 0.25f);
            }
            var pusher = _kit.Part(root, "Packer.Pusher", new Vector3(0, 1.45f, -0.4f), b =>
            {
                b.Slot = (int)Mat.Chrome;
                b.Cylinder(Vector3.zero, 0.05f, 0.25f, 8);
                b.Slot = (int)Mat.Metal;
                b.Box(Vector3.zero, new Vector3(0.4f, 0.05f, 0.35f), 0.01f);
            }, false);
            Animate(v, pusher.transform, PartMotion.Stroke, Vector3.down, 0.35f);
        }

        // ---------------------------------------------------------------- storage & shipping

        private void Warehouse(MachineView v, Transform root, Material crate)
        {
            _kit.Part(root, "Warehouse.Racks", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Accent;
                for (int r = 0; r < 2; r++)
                {
                    float z = 0.25f + r * 0.6f;
                    for (int x = 0; x < 3; x++) b.Box(new Vector3(-0.95f + x * 0.95f, 1.05f, z), new Vector3(0.07f, 1.9f, 0.07f), 0f);
                    b.Slot = (int)Mat.Hazard;
                    for (int lvl = 0; lvl < 3; lvl++) b.Box(new Vector3(0, 0.3f + lvl * 0.62f, z), new Vector3(2.0f, 0.06f, 0.5f), 0f);
                    b.Slot = (int)Mat.Accent;
                }
                b.Slot = (int)Mat.Hazard;
                b.Box(new Vector3(0, 0.13f, -0.55f), new Vector3(2.4f, 0.01f, 0.06f), 0f); // aisle marking
            });

            var slots = new System.Collections.Generic.List<Renderer>();
            for (int r = 0; r < 2; r++)
            for (int lvl = 0; lvl < 3; lvl++)
            for (int x = 0; x < 4; x++)
            {
                var c = _kit.Solid(root, "Warehouse.Crate", new Vector3(-0.72f + x * 0.48f, 0.5f + lvl * 0.62f, 0.25f + r * 0.6f), crate,
                    b => b.Box(Vector3.zero, new Vector3(0.38f, 0.34f, 0.4f), 0.04f));
                c.enabled = false;
                slots.Add(c);
            }
            v.CrateSlots = slots.ToArray();

            var fork = ModelKit.Pivot(root, "Forklift", new Vector3(-1.0f, 0.12f, -0.6f));
            BuildForklift(fork, crate);
            v.Forklift = fork;
            v.ForkliftFrom = new Vector3(-1.0f, 0.12f, -0.6f);
            v.ForkliftTo = new Vector3(0.9f, 0.12f, -0.6f);
        }

        private void BuildForklift(Transform parent, Material crate)
        {
            _kit.Part(parent, "Forklift.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Hazard;
                b.Box(new Vector3(0, 0.28f, 0), new Vector3(0.55f, 0.3f, 0.36f), 0.05f);
                b.Slot = (int)Mat.Metal;
                b.Box(new Vector3(-0.12f, 0.6f, 0), new Vector3(0.05f, 0.4f, 0.32f), 0f); // cage
                b.Box(new Vector3(0.1f, 0.6f, 0), new Vector3(0.05f, 0.4f, 0.32f), 0f);
                b.Box(new Vector3(0, 0.8f, 0), new Vector3(0.3f, 0.04f, 0.34f), 0f);
                b.Box(new Vector3(0.33f, 0.45f, 0), new Vector3(0.05f, 0.7f, 0.3f), 0f); // mast
                b.Slot = (int)Mat.Chrome;
                b.Box(new Vector3(0.48f, 0.12f, -0.08f), new Vector3(0.3f, 0.03f, 0.05f), 0f); // forks
                b.Box(new Vector3(0.48f, 0.12f, 0.08f), new Vector3(0.3f, 0.03f, 0.05f), 0f);
                b.Slot = (int)Mat.Rubber;
                for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    b.CylinderAlong(new Vector3(sx * 0.17f, 0.09f, sz * 0.19f), new Vector3(sx * 0.17f, 0.09f, sz * 0.13f), 0.09f, 10);
            }, false);
            _kit.Solid(parent, "Forklift.Crate", new Vector3(0.5f, 0.3f, 0), crate, b => b.Box(Vector3.zero, new Vector3(0.3f, 0.3f, 0.3f), 0.03f));
        }

        private void Dock(MachineView v, Transform root, Material crate)
        {
            _kit.Part(root, "Dock.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Concrete;
                b.Box(new Vector3(-0.6f, 0.45f, 0), new Vector3(1.5f, 0.7f, 2.2f), 0.04f); // loading platform
                b.Slot = (int)Mat.Hazard;
                b.Box(new Vector3(0.16f, 0.81f, 0), new Vector3(0.06f, 0.02f, 2.0f), 0f);
                b.Slot = (int)Mat.Rubber;
                b.Box(new Vector3(0.17f, 0.45f, -0.6f), new Vector3(0.1f, 0.3f, 0.3f), 0.03f); // bumpers
                b.Box(new Vector3(0.17f, 0.45f, 0.6f), new Vector3(0.1f, 0.3f, 0.3f), 0.03f);
                b.Slot = (int)Mat.Metal;
                b.Box(new Vector3(-1.25f, 1.55f, 1.0f), new Vector3(0.12f, 2.9f, 0.12f), 0.02f); // canopy posts
                b.Box(new Vector3(-1.25f, 1.55f, -1.0f), new Vector3(0.12f, 2.9f, 0.12f), 0.02f);
                b.Slot = (int)Mat.Accent;
                b.PushTRS(new Vector3(-0.1f, 3.0f, 0), Quaternion.Euler(0, 0, -6f), Vector3.one);
                b.Box(Vector3.zero, new Vector3(2.6f, 0.1f, 2.4f), 0.03f);
                b.PopMatrix();
                Ports(b, output: false);
            });

            var truck = ModelKit.Pivot(root, "Truck", new Vector3(1.7f, 0, 0));
            BuildTruck(truck, out var bedCrates, crate);
            v.Truck = truck;
            v.TruckCrates = bedCrates;
            v.TruckParked = new Vector3(1.7f, 0, 0);
            v.TruckAway = new Vector3(14f, 0, 0);
        }

        private void BuildTruck(Transform parent, out Renderer[] crates, Material crate)
        {
            _kit.Part(parent, "Truck.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Metal;
                b.Box(new Vector3(0.2f, 0.42f, 0), new Vector3(3.0f, 0.18f, 1.1f), 0.03f); // chassis
                b.Slot = (int)Mat.Accent;
                b.Box(new Vector3(1.35f, 0.95f, 0), new Vector3(0.8f, 0.95f, 1.15f), 0.12f); // cab
                b.Slot = (int)Mat.Glass;
                b.Box(new Vector3(1.76f, 1.12f, 0), new Vector3(0.02f, 0.4f, 0.95f), 0f);
                b.Slot = (int)Mat.Body;
                b.Box(new Vector3(-0.3f, 0.56f, 0), new Vector3(2.1f, 0.1f, 1.15f), 0.02f); // flat bed
                b.Box(new Vector3(-1.33f, 0.8f, 0), new Vector3(0.06f, 0.4f, 1.15f), 0.01f);
                b.Slot = (int)Mat.Rubber;
                foreach (float x in new[] { -0.9f, -0.35f, 1.3f })
                for (int sz = -1; sz <= 1; sz += 2)
                    b.CylinderAlong(new Vector3(x, 0.24f, sz * 0.6f), new Vector3(x, 0.24f, sz * 0.42f), 0.24f, 12);
                b.Slot = (int)Mat.LampOff;
                b.Box(new Vector3(1.76f, 0.6f, 0.42f), new Vector3(0.03f, 0.1f, 0.16f), 0f);
                b.Box(new Vector3(1.76f, 0.6f, -0.42f), new Vector3(0.03f, 0.1f, 0.16f), 0f);
            });
            var list = new System.Collections.Generic.List<Renderer>();
            for (int i = 0; i < 6; i++)
            {
                int col = i % 3, row = i / 3;
                var c = _kit.Solid(parent, "Truck.Crate", new Vector3(-1.0f + col * 0.6f, 0.8f, -0.25f + row * 0.5f), crate,
                    b => b.Box(Vector3.zero, new Vector3(0.5f, 0.38f, 0.42f), 0.04f), true);
                c.enabled = false;
                list.Add(c);
            }
            crates = list.ToArray();
        }
    }
}
