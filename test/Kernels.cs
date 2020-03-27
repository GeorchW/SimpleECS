using System;
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
            e1.Add<ExampleComp1>() = new ExampleComp1 { Value = "Entity1 Comp1 initial value" };
            e1.Add<ExampleComp2>() = new ExampleComp2 { Value = "Entity1 Comp2 initial value" };

            e2 = scene.CreateEntity();
            e2.Add<ExampleComp1>() = new ExampleComp1 { Value = "Entity2 Comp1 initial value" };
            e2.Add<ExampleComp2>() = new ExampleComp2 { Value = "Entity2 Comp2 initial value" };

            e3 = scene.CreateEntity();
            e3.Add<ExampleComp1>() = new ExampleComp1 { Value = "Entity3 Comp1 initial value" };
        }
        [Test]
        public void Simple()
        {
            scene.Run(this, nameof(SimpleKernel));
            Assert.That(e1.Get<ExampleComp2>().Value, Is.EqualTo("Entity1 Comp2 initial value + Entity1 Comp1 initial value"));
            Assert.That(e2.Get<ExampleComp2>().Value, Is.EqualTo("Entity2 Comp2 initial value + Entity2 Comp1 initial value"));
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

            Assert.That(e1.Get<ExampleComp2>().Value, Is.EqualTo("Entity1 Comp2 initial value"));
            Assert.That(e2.Get<ExampleComp2>().Value, Is.EqualTo("Entity2 Comp2 initial value"));
            Assert.That(e3.Get<ExampleComp2>().Value, Is.EqualTo("Entity3 Comp1 initial value"));
        }

        void BannedKernel(in ExampleComp1 exampleComp1, [Banned] out ExampleComp2 exampleComp2)
        {
            exampleComp2 = new ExampleComp2 { Value = exampleComp1.Value };
        }
    }
}