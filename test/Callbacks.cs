using NUnit.Framework;

namespace SimpleECS.Test
{
    public class Callbacks
    {
        struct DisposableComp
        {
        }
        Scene scene = null!;
        Entity entity;
        int deletesCalled = 0;

        [SetUp]
        public void Setup()
        {
            deletesCalled = 0;
            scene = new Scene();

            entity = scene.CreateEntity();
            entity.Add<ExampleComp1>() = new ExampleComp1 { Value = "I want to be free!" };
            entity.Add<DisposableComp>();

            scene.Callbacks.Get(typeof(DisposableComp)).ComponentRemoved += (sender, entity) => deletesCalled++;
        }

        [Test]
        public void OnEntityDelete()
        {
            Assert.That(deletesCalled, Is.EqualTo(0));
            entity.Delete();
            Assert.That(deletesCalled, Is.EqualTo(1));
        }

        [Test]
        public void OnComponentRemove()
        {
            Assert.That(deletesCalled, Is.EqualTo(0));
            entity.Remove<DisposableComp>();
            Assert.That(deletesCalled, Is.EqualTo(1));
        }
    }
}