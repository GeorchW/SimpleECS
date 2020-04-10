using System;
using NUnit.Framework;

namespace SimpleECS.Test
{
    struct ExampleComp1 : ISerializableComponent { public string Value; }
    struct ExampleComp2 : ISerializableComponent { public string Value; }
    public class Archetypes
    {
        Scene scene = null!;
        Entity entity;
        [SetUp]
        public void Setup()
        {
            scene = new Scene();
            entity = scene.CreateEntity();

            entity.Add<ExampleComp1>() = new ExampleComp1 { Value = "Comp1 initial value" };
            entity.Add<ExampleComp2>() = new ExampleComp2 { Value = "Comp2 initial value" };
        }

        [Test]
        public void Storage()
        {
            Assert.That(entity.Get<ExampleComp1>().Value, Is.EqualTo("Comp1 initial value"));
            Assert.That(entity.Get<ExampleComp2>().Value, Is.EqualTo("Comp2 initial value"));

            scene.InsertNewComponents();

            Assert.That(entity.Get<ExampleComp1>().Value, Is.EqualTo("Comp1 initial value"));
            Assert.That(entity.Get<ExampleComp2>().Value, Is.EqualTo("Comp2 initial value"));
        }

        [TestCase(false), TestCase(true)]
        public void EntityDelete(bool update)
        {
            if (update)
                scene.InsertNewComponents();
            entity.Delete();
            Assert.Throws<Exception>(() => entity.Get<ExampleComp1>());
        }

        [Test]
        public void CorrectArchetypesCreated()
        {
            Assert.That(scene.archetypes.Count == 1);
            scene.InsertNewComponents();
            Assert.That(scene.archetypes.Count == 2);

            var newEntity = scene.CreateEntity();
            scene.InsertNewComponents();
            Assert.That(scene.archetypes.Count == 2);

            newEntity.Add<ExampleComp1>();
            newEntity.Add<ExampleComp2>();
            Assert.That(scene.archetypes.Count == 2);
        }

        [Test]
        public void EnumerateEntities()
        {
            Assert.That(scene, Is.EquivalentTo(new[] { entity }));
        }
    }
}