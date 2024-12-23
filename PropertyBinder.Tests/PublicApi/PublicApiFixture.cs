using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace PropertyBinder.Tests
{
    [TestFixture]
    public class PublicApiFixture
    {
        [Test]
        public void ShouldPreservePublicApi()
        {
            var publicApiFolder = AppDomain.CurrentDomain.BaseDirectory;
#if NET462
            string fileName = Path.Combine(publicApiFolder, @"PublicApi_NET45.txt");
#endif

#if NETCOREAPP 
            string fileName = Path.Combine(publicApiFolder, @"PublicApi_NETSTANDARD21.txt");
#endif

            var assembly = typeof(Binder<>).Assembly;
            var api = PublicApiGenerator.ApiGenerator.GeneratePublicApi(assembly);
            var frameworkVersion = assembly.ImageRuntimeVersion;
            if (File.Exists(fileName))
            {
                var currentApi = File.ReadAllText(fileName);
                if (!string.Equals(api, currentApi))
                {
                    File.WriteAllText(fileName, api);
                }

                Assert.AreEqual(api, currentApi, $"API mismatch for {frameworkVersion}, check git diff");
            }
            else
            {
                File.WriteAllText(fileName, api);
            }
        }
    }
}