using System;
using NUnit.Framework;

namespace SimpleECS.Test
{
    struct ExampleComp1 { public int Value1, Value2; }
    struct ExampleComp2 { public float Value3, Value4; }
    public class Tests
    {
        Scene scene;
        Entity entity;
        [SetUp]
        public void Setup()
        {
            scene = new Scene();
            entity = scene.CreateEntity();

            entity.Add<ExampleComp1>() = new ExampleComp1 { Value1 = 5, Value2 = 6 };
            entity.Add<ExampleComp2>() = new ExampleComp2 { Value3 = 5.5f, Value4 = 6.3f };
        }
        [Test]
        public void Storage()
        {
            Assert.That(entity.Get<ExampleComp1>().Value1, Is.EqualTo(5));
            Assert.That(entity.Get<ExampleComp1>().Value2, Is.EqualTo(6));
            Assert.That(entity.Get<ExampleComp2>().Value3, Is.EqualTo(5.5f));
            Assert.That(entity.Get<ExampleComp2>().Value4, Is.EqualTo(6.3f));

            scene.UpdateArchetypes();

            Assert.That(entity.Get<ExampleComp1>().Value1, Is.EqualTo(5));
            Assert.That(entity.Get<ExampleComp1>().Value2, Is.EqualTo(6));
            Assert.That(entity.Get<ExampleComp2>().Value3, Is.EqualTo(5.5f));
            Assert.That(entity.Get<ExampleComp2>().Value4, Is.EqualTo(6.3f));
        }

        [Test]
        public void EntityDelete()
        {
            Scene scene = new Scene();
            var entity = scene.CreateEntity();
            entity.Add<ExampleComp1>() = new ExampleComp1();

            entity.Delete();
            Assert.Throws<Exception>(() => entity.Get<ExampleComp1>());
        }
    }
}