using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var confbuilder = new ConfigurationBuilder();
            // установка пути к текущему каталогу
            confbuilder.SetBasePath(Directory.GetCurrentDirectory());
            // получаем конфигурацию из файла appsettings.json
            confbuilder.AddJsonFile("appsettings.json");
            // создаем конфигурацию
            IConfigurationRoot config = confbuilder.Build();
            // получаем строку подключения
            string? connectionString = config.GetConnectionString("PostgeSQL");

        }
    }
}
