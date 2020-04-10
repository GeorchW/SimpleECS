using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace SimpleECS.Test
{
    public class Serialization
    {
        [Test]
        public void SerializeEmptyScene()
        {
            Scene scene = new Scene();
            Assert.That(
                SceneJsonSerializer.ToJson(scene, false),
                Is.EqualTo("{\"types\":{},\"entities\":{}}"));
        }
        [Test]
        public void SerializeEntity()
        {
            Scene scene = new Scene();
            scene.CreateEntity().Add<NameComp>().Name = "MyEntity";
            Assert.That(SceneJsonSerializer.ToJson(scene, false), Does.Match("{\"types\":{\"NameComp\":.*},\"entities\":{\"MyEntity\":{\"NameComp\":{\"Name\":\"MyEntity\"}}}}"));
        }
        [Test]
        public void RoundTripEmptyScene()
        {
            Scene scene = new Scene();
            var json = SceneJsonSerializer.ToJson(scene, false);
            var newScene = SceneJsonSerializer.FromJson(json);
            Assert.That(scene.Count, Is.EqualTo(0));
            Assert.That(newScene.Count, Is.EqualTo(0));
        }
        [Test]
        public void RoundTripEntity()
        {
            Scene scene = new Scene();
            Entity entity = scene.CreateEntity();
            entity.Add<NameComp>().Name = "MyEntity";

            var json = SceneJsonSerializer.ToJson(scene, true);
            var newScene = SceneJsonSerializer.FromJson(json);
            Assert.That(scene.Count, Is.EqualTo(1));
            Assert.That(newScene.Count, Is.EqualTo(1));

            var newEntity = newScene.Single();
            AssertEntityHasComponents(newEntity, typeof(NameComp));
            Assert.That(newEntity.Get<NameComp>().Name, Is.EqualTo("MyEntity"));
        }

        struct EntityReferenceComp : ISerializableComponent
        {
            public Entity Other;
        }
        [Test]
        public void RoundTripEntityReference()
        {
            Scene scene = new Scene();
            Entity a = scene.CreateEntity("a");
            Entity b = scene.CreateEntity("b");
            a.Add<EntityReferenceComp>().Other = b;

            var json = SceneJsonSerializer.ToJson(scene, true);
            var newScene = SceneJsonSerializer.FromJson(json);
            Assert.That(scene.Count, Is.EqualTo(2));
            Assert.That(newScene.Count, Is.EqualTo(2));

            Entity.CurrentScene = newScene;
            var newA = newScene.Where(e => e.Get<NameComp>().Name == "a").Single();
            var newB = newScene.Where(e => e.Get<NameComp>().Name == "b").Single();
            AssertEntityHasComponents(newA, typeof(NameComp), typeof(EntityReferenceComp));
            AssertEntityHasComponents(newB, typeof(NameComp));
            Assert.That(newA.Get<EntityReferenceComp>().Other, Is.EqualTo(newB));
        }

        struct NonSerializedComp : IComponent { }
        [Test]
        public void IComponentsAreNotSerialized()
        {
            Scene scene = new Scene();
            Entity entity = scene.CreateEntity("MyEntity");
            entity.Add<NonSerializedComp>();

            var json = SceneJsonSerializer.ToJson(scene);
            var newScene = SceneJsonSerializer.FromJson(json);

            AssertEntityHasComponents(entity, typeof(NameComp));
        }
        void AssertEntityHasComponents(Entity entity, params Type[] componentTypes)
        {
            Assert.That(
                entity.GetComponentsUnsafe().Select(x => x.GetType()),
                Is.EquivalentTo(componentTypes));
        }
    }
}