using NUnit.Framework;

namespace SimpleECS.Test
{
    struct ExampleComp1 { int Value1, Value2;}
    struct ExampleComp2 { float Value3, Value4; }
    public class Tests
    {
        [Test]
        public void TestEntityCount()
        {
            Scene scene = new Scene();
            int entity = scene.InitialContainer.AddEntity();
            Assert.That(scene.InitialContainer.EntityCount, Is.EqualTo(1));

            scene.InitialContainer.Add<ExampleComp1>(entity);
            scene.InitialContainer.Add<ExampleComp2>(entity);
            scene.UpdateArchetypes();

            Assert.That(scene.InitialContainer.EntityCount, Is.EqualTo(0));
        }
    }
}