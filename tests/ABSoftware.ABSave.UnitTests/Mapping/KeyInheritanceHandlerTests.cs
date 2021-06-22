using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation.Inheritance;
using ABCo.ABSave.TestOtherAssembly;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ABCo.ABSave.UnitTests.Mapping
{
    [TestClass]
    public class KeyInheritanceHandlerTests
    {
        [TestMethod]
        public void GetOrAddTypeKeyFromCache_Exists()
        {
            SaveInheritanceAttribute attribute = typeof(KeyBase).GetCustomAttribute<SaveInheritanceAttribute>();

            // Fill the cache up.
            var oldSerializeCache = attribute.KeySerializeCache = new Dictionary<Type, string>();
            var oldDeserializeCache = attribute.KeyDeserializeCache = new Dictionary<string, Type>();

            oldSerializeCache.Add(typeof(KeySubFirst), "First");
            oldDeserializeCache.Add("First", typeof(KeySubFirst));

            string key = KeyInheritanceHandler.GetOrAddTypeKeyFromCache(typeof(KeyBase), typeof(KeySubFirst), attribute);
            Assert.AreEqual("First", key);

            // Ensure it didn't add to the cache.
            Assert.AreEqual(oldSerializeCache, attribute.KeySerializeCache);
            Assert.AreEqual(oldDeserializeCache, attribute.KeyDeserializeCache);

            Assert.AreEqual(1, attribute.KeySerializeCache.Count);
            Assert.AreEqual(1, attribute.KeyDeserializeCache.Count);
        }

        [TestMethod]
        public void GetOrAddTypeKeyFromCache_New()
        {
            SaveInheritanceAttribute attribute = typeof(KeyBase).GetCustomAttribute<SaveInheritanceAttribute>();

            string key = KeyInheritanceHandler.GetOrAddTypeKeyFromCache(typeof(KeyBase), typeof(KeySubFirst), attribute);
            Assert.AreEqual("First", key);

            Assert.AreEqual(1, attribute.KeySerializeCache.Count);
            Assert.AreEqual(1, attribute.KeyDeserializeCache.Count);

            Assert.AreEqual("First", attribute.KeySerializeCache[typeof(KeySubFirst)]);
            Assert.AreEqual(typeof(KeySubFirst), attribute.KeyDeserializeCache["First"]);
        }

        [TestMethod]
        public void EnsureHasAllTypeCache_New_SameAssembly()
        {
            SaveInheritanceAttribute attribute = typeof(KeyBase).GetCustomAttribute<SaveInheritanceAttribute>();

            KeyInheritanceHandler.EnsureHasAllTypeCache(typeof(KeyBase), attribute);

            Assert.AreEqual(2, attribute.KeySerializeCache.Count);
            Assert.AreEqual(2, attribute.KeyDeserializeCache.Count);

            Assert.AreEqual("First", attribute.KeySerializeCache[typeof(KeySubFirst)]);
            Assert.AreEqual(typeof(KeySubFirst), attribute.KeyDeserializeCache["First"]);

            Assert.AreEqual("Second", attribute.KeySerializeCache[typeof(KeySubSecond)]);
            Assert.AreEqual(typeof(KeySubSecond), attribute.KeyDeserializeCache["Second"]);

            Assert.IsTrue(attribute.HasGeneratedFullKeyCache);
        }

        [TestMethod]
        public void EnsureHasAllTypeCache_New_CrossAssembly()
        {
            SaveInheritanceAttribute attribute = typeof(KeyBase).GetCustomAttribute<SaveInheritanceAttribute>();

            KeyInheritanceHandler.EnsureHasAllTypeCache(typeof(OtherAssemblyBase), attribute);

            Assert.AreEqual(2, attribute.KeySerializeCache.Count);
            Assert.AreEqual(2, attribute.KeyDeserializeCache.Count);

            Assert.AreEqual("First", attribute.KeySerializeCache[typeof(OtherAssemblySub)]);
            Assert.AreEqual(typeof(OtherAssemblySub), attribute.KeyDeserializeCache["First"]);

            Assert.AreEqual("Second", attribute.KeySerializeCache[typeof(CrossAssemblySub)]);
            Assert.AreEqual(typeof(CrossAssemblySub), attribute.KeyDeserializeCache["Second"]);

            Assert.IsTrue(attribute.HasGeneratedFullKeyCache);
        }

        [TestMethod]
        public void EnsureHasAllTypeCache_Existing()
        {
            SaveInheritanceAttribute attribute = typeof(KeyBase).GetCustomAttribute<SaveInheritanceAttribute>();

            // Simulate it having already been done.
            attribute.HasGeneratedFullKeyCache = true;

            KeyInheritanceHandler.EnsureHasAllTypeCache(typeof(KeyBase), attribute);

            // Since nothing has actually been done these will just be null.
            Assert.IsNull(attribute.KeySerializeCache);
            Assert.IsNull(attribute.KeyDeserializeCache);
        }
    }
}
