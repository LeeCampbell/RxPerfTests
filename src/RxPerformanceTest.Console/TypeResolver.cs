using System;
using System.IO;
using System.Reflection;

namespace RxPerformanceTest.Console
{
    public sealed class TypeResolver
    {
        private readonly string _basePath;

        public TypeResolver(string basePath)
        {
            _basePath = basePath;
            AppDomain.CurrentDomain.AssemblyResolve += LoadFromSameFolder;
        }

        public object GetInstance(string assemblyName, string subjectTypeName, params object[] args)
        {
            var assemblyFullPath = Path.Combine(_basePath, assemblyName);
            var ass = Assembly.LoadFile(assemblyFullPath);

            var subjectType = ass.GetType(subjectTypeName);
            var itemType = typeof(int);
            return Activator.CreateInstance(subjectType.MakeGenericType(itemType), args);
        }

        private Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
            string assemblyPath = Path.Combine(_basePath, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(assemblyPath) == false) return null;
            var assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }
    }
}