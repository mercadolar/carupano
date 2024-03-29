﻿using System;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Carupano.Hosting
{
    using Configuration;
    public class HostProcess
    {
        public static void Run(string[] args)
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            var asmFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll");
            var builder = new BoundedContextModelBuilder();
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Carupano Host");
            Console.ForegroundColor = color;
            Console.WriteLine("Inspecting available assemblies...");
            var configTypes = asmFiles.Select(a => Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(a))).GetTypes().SingleOrDefault(c => c.Name == "Startup")).Where(c => c != null);
            foreach (var config in configTypes)
            {
                Console.WriteLine($"\tFound Startup class {config.Name}");
                var method = config.GetMethods().SingleOrDefault(c => c.Name == "Configure" && c.GetParameters().Count() == 1 && c.GetParameters().Single().ParameterType == builder.GetType());
                var instance = Activator.CreateInstance(config);
                method.Invoke(instance, new[] { builder });
                Console.WriteLine($"\tConfigured {config.Name}");
            }
            if(!configTypes.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No Startup class(es) found.");
            }
            else
            {
                var model = builder.Build();
                var mgr = new ProjectionManager(model.Services.GetService<IEventStore>(), model.Services.GetService<IEventBus>(), model.Projections.Select(c => c.CreateInstance(model.Services)));
                mgr.Start();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Carupano is running...");

            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Press any key to exit process.");
            Console.ReadKey();
        }
    }
}
