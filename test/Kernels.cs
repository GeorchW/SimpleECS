using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SimpleECS.Test
{
    public class Kernels
    {
        Scene scene;
        Entity e1, e2, e3;
        [SetUp]
        public void Setup()
        {
            scene = new Scene();

            e1 = scene.CreateEntity();
            e1.Add<ExampleComp1>() = new ExampleComp1 { Value = $"Entity{e1.Id} Comp1 initial value" };
            e1.Add<ExampleComp2>() = new ExampleComp2 { Value = $"Entity{e1.Id} Comp2 initial value" };

            e2 = scene.CreateEntity();
            e2.Add<ExampleComp1>() = new ExampleComp1 { Value = $"Entity{e2.Id} Comp1 initial value" };
            e2.Add<ExampleComp2>() = new ExampleComp2 { Value = $"Entity{e2.Id} Comp2 initial value" };

            e3 = scene.CreateEntity();
            e3.Add<ExampleComp1>() = new ExampleComp1 { Value = $"Entity{e3.Id} Comp1 initial value" };

            Assert.That(new[] { e1.Id, e2.Id, e3.Id }, Is.Unique);
        }
        [Test]
        public void Simple()
        {
            scene.Run(this, nameof(SimpleKernel));
            Assert.That(e1.Get<ExampleComp2>().Value, Is.EqualTo($"Entity{e1.Id} Comp2 initial value + Entity{e1.Id} Comp1 initial value"));
            Assert.That(e2.Get<ExampleComp2>().Value, Is.EqualTo($"Entity{e2.Id} Comp2 initial value + Entity{e2.Id} Comp1 initial value"));
        }

        void SimpleKernel(in ExampleComp1 exampleComp1, ref ExampleComp2 exampleComp2)
        {
            exampleComp2.Value += " + " + exampleComp1.Value;
        }

        [Test]
        public void Out()
        {
            scene.Run(this, nameof(OutKernel));

            foreach (var e in new[] { e1, e2, e3 })
            {
                Assert.That(e.Get<ExampleComp2>().Value, Is.EqualTo(e.Get<ExampleComp1>().Value));
            }
        }

        void OutKernel(in ExampleComp1 exampleComp1, out ExampleComp2 exampleComp2)
        {
            exampleComp2 = new ExampleComp2 { Value = exampleComp1.Value };
        }

        [Test]
        public void Banned()
        {
            scene.Run(this, nameof(BannedKernel));

            Assert.That(e1.Get<ExampleComp2>().Value, Is.EqualTo($"Entity{e1.Id} Comp2 initial value"));
            Assert.That(e2.Get<ExampleComp2>().Value, Is.EqualTo($"Entity{e2.Id} Comp2 initial value"));
            Assert.That(e3.Get<ExampleComp2>().Value, Is.EqualTo($"Entity{e3.Id} Comp1 initial value"));
        }

        void BannedKernel(in ExampleComp1 exampleComp1, [Banned] out ExampleComp2 exampleComp2)
        {
            exampleComp2 = new ExampleComp2 { Value = exampleComp1.Value };
        }

        [Test]
        public void WithEntity()
        {
            scene.Run(this, nameof(EntityKernel));
            Assert.That(e1.Get<ExampleComp2>().Value, Is.EqualTo($"Entity{e1.Id} Comp2 new value"));
            Assert.That(e2.Get<ExampleComp2>().Value, Is.EqualTo($"Entity{e2.Id} Comp2 new value"));
        }

        void EntityKernel(Entity entity, ref ExampleComp2 component)
        {
            Assert.That(entity.IsValid, "Entity is invalid");
            Assert.That(entity == e1 || entity == e2, "Wrong entity selected");
            component.Value = $"Entity{entity.Id} Comp2 new value";
        }

        [Test]
        public void WithGlobal()
        {
            scene.Globals.Add(new List<int>());
            scene.Run(this, nameof(GlobalKernel));
            Assert.That(scene.Globals.Get<List<int>>().Count, Is.EqualTo(2));
        }

        void GlobalKernel(in ExampleComp2 exampleComp2, [Global] List<int> myGlobal)
        {
            myGlobal.Add(1);
        }

        [Test]
        public void Changed()
        {
            e1.Add<InvocationCounter>();
            e2.Add<InvocationCounter>();
            e3.Add<InvocationCounter>();

            scene.Run(this, nameof(ChangedKernel));
            foreach (var e in new[] { e1, e2, e3 })
                Assert.That(e.Get<InvocationCounter>().Count, Is.EqualTo(1));

            scene.Run(this, nameof(ChangedKernel));
            foreach (var e in new[] { e1, e2, e3 })
                Assert.That(e.Get<InvocationCounter>().Count, Is.EqualTo(1));

            Span<int> test = stackalloc[] { 1, 2, 3 };

            e3.GetMutable<ExampleComp1>().Value = "something else";
            scene.Run(this, nameof(ChangedKernel));
            foreach (var e in new[] { e1, e2 })
                Assert.That(e.Get<InvocationCounter>().Count, Is.EqualTo(1));
            Assert.That(e3.Get<InvocationCounter>().Count, Is.EqualTo(2));
        }

        struct InvocationCounter
        {
            public int Count;
        }
        void ChangedKernel([Changed] in ExampleComp1 exampleComp1, ref InvocationCounter counter)
        {
            counter.Count++;
        }
    }
}