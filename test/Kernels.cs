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
            e1.Add<ExampleComp1>() = new ExampleComp1 { Value1 = 5, Value2 = 6 };
            e1.Add<ExampleComp2>() = new ExampleComp2 { Value3 = 5.5f, Value4 = 6.3f };

            e2 = scene.CreateEntity();
            e2.Add<ExampleComp1>() = new ExampleComp1 { Value1 = 7, Value2 = 8 };
            e2.Add<ExampleComp2>() = new ExampleComp2 { Value3 = 1.5f, Value4 = 2.0f };

            e3 = scene.CreateEntity();
            e3.Add<ExampleComp1>() = new ExampleComp1 { Value1 = 5, Value2 = 6 };
        }
        [Test]
        public void RunKernel()
        {
            scene.Run(this, nameof(SimpleKernel));
            Assert.That(e1.Get<ExampleComp2>().Value3, Is.EqualTo(10.5f));
            Assert.That(e2.Get<ExampleComp2>().Value3, Is.EqualTo(8.5f));
        }

        void SimpleKernel(in ExampleComp1 exampleComp1, ref ExampleComp2 exampleComp2)
        {
            exampleComp2.Value3 += exampleComp1.Value1;
        }
    }
}