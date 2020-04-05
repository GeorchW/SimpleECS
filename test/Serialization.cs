using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NUnit.Framework;

namespace SimpleECS.Test
{
    public class Serialization
    {
        void TestSerialization(Scene scene, string expected)
        {
            string text = SceneJsonSerializer.ToJson(scene);

            var test = JsonDocument.Parse(text);
            test.RootElement[2].GetUInt16();
        }


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
    }
}