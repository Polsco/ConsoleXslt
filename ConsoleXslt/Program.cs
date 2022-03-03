using ConsoleXslt.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Xsl;

namespace ConsoleXslt
{
    class Program
    {
        private static Settings appSettings;
        private static Dictionary<string, string[]> menuItems = new Dictionary<string, string[]>();
        private static ConsoleKeyInfo consoleKeyInfo;
        private static int selectedItemIndex;
        private static bool transformFailed;

        private static void InitialiseSettings(Settings settings)              
        {
            settings.XmlInputFilesDirectory = Directory.Exists(settings.XmlInputFilesDirectory) ? settings.XmlInputFilesDirectory : AppDomain.CurrentDomain.BaseDirectory;
            settings.XmlOutputFilesDirectory = Directory.Exists(settings.XmlOutputFilesDirectory) ? settings.XmlOutputFilesDirectory : AppDomain.CurrentDomain.BaseDirectory;
            settings.XslFilesDirectory = Directory.Exists(settings.XslFilesDirectory) ? settings.XslFilesDirectory : AppDomain.CurrentDomain.BaseDirectory;
            settings.XslTransformFileName = File.Exists(settings.XslFilesDirectory + "\\" + settings.XslTransformFileName) ? settings.XslTransformFileName : Directory.GetFiles(settings.XslFilesDirectory, "*.xsl").Length > 0 ? Directory.GetFiles(settings.XslFilesDirectory, "*.xsl").Select(Path.GetFileName).FirstOrDefault() : Directory.GetFiles(settings.XslFilesDirectory, "*.xslt").Length > 0 ? Directory.GetFiles(settings.XslFilesDirectory, "*.xslt").Select(Path.GetFileName).FirstOrDefault() :  "";
            settings.XmlFileNameToTransform = File.Exists(settings.XmlInputFilesDirectory + "\\" + settings.XmlFileNameToTransform) ? settings.XmlFileNameToTransform : Directory.GetFiles(settings.XmlInputFilesDirectory, "*.xml").Length > 0 ? Directory.GetFiles(settings.XmlInputFilesDirectory, "*.xml").Select(Path.GetFileName).FirstOrDefault() : "";
            settings.ForceIndentsAndBreakLinesInXmlOutput = "ON";
            settings.Save();
        }

        private static void InitialiseMenu()
        {
            menuItems.Add("XmlInputFilesDirectory", new string[] { appSettings.XmlInputFilesDirectory });
            menuItems.Add("XmlOutputFilesDirectory", new string[] { appSettings.XmlOutputFilesDirectory });
            menuItems.Add("XslFilesDirectory", new string[] { appSettings.XslFilesDirectory });
            menuItems.Add("XslTransformFileName", Directory.GetFiles(appSettings.XslFilesDirectory, "*.xsl").Select(Path.GetFileName).Union(Directory.GetFiles(appSettings.XslFilesDirectory, "*.xsl").Select(Path.GetFileName)).OrderBy(filename => filename).ToArray());
            menuItems.Add("XmlFileNameToTransform", Directory.GetFiles(appSettings.XmlInputFilesDirectory, "*.xml").Select(Path.GetFileName).OrderBy(filename => filename).ToArray());
            //menuItems.Add("ForceIndentsAndBreakLinesInXmlOutput", new string[] { "OFF", "ON" });
            menuItems.Add("\nStart", new string[]{""});
        }

        private static void DrawMenu()
        {
            Console.CursorVisible = false;
            Console.SetCursorPosition(0, 0);
            //Draw Menu:
            for (int i = 0; i < menuItems.Count; i++)
            {
                //item name:
                Console.BackgroundColor = i == selectedItemIndex ? ConsoleColor.Gray : ConsoleColor.Black;
                Console.ForegroundColor = i == selectedItemIndex ? ConsoleColor.Black : ConsoleColor.Gray;
                Console.Write("{0}{1}", Regex.Replace(menuItems.Keys.ElementAt(i), "(\\B[A-Z])", " $1").ToUpper(), i == selectedItemIndex && menuItems.Values.ElementAt(i).Length > 1 ? " <- ->" : "");
                //item value:
                if (i < menuItems.Count - 1)
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = menuItems.Values.ElementAt(i).Length > 0 ? ConsoleColor.White : ConsoleColor.Red;
                    Console.Write("{0}", Array.IndexOf(menuItems.Values.ElementAt(i), appSettings[menuItems.Keys.ElementAt(i)]) > -1 ? " " + menuItems.Values.ElementAt(i).GetValue(Array.IndexOf(menuItems.Values.ElementAt(i), appSettings[menuItems.Keys.ElementAt(i)])) : " could not find any ." + menuItems.Keys.ElementAt(i).Substring(0,3).ToLower() + " file in specified directory");
                }
                Console.ResetColor();
                Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft - 1) + Environment.NewLine);
            }
        }

        private static void UserInput(string selectedItemKey)
        {
            consoleKeyInfo = Console.ReadKey(true);
            int selectedOptionIndex = selectedItemIndex < menuItems.Count - 1 ? Array.IndexOf(menuItems[selectedItemKey], appSettings[selectedItemKey]) : -1;
            switch (consoleKeyInfo.Key)
            {
                case ConsoleKey.Escape: Environment.Exit(0); break;
                case ConsoleKey.UpArrow: selectedItemIndex = selectedItemIndex > 0 ? selectedItemIndex - 1 : menuItems.Count - 1; break;
                case ConsoleKey.DownArrow: selectedItemIndex = selectedItemIndex < menuItems.Count - 1 ? selectedItemIndex + 1 : 0; break;
                case ConsoleKey.LeftArrow: 
                    if(selectedOptionIndex > 0)
                    {
                        appSettings[selectedItemKey] = menuItems[selectedItemKey].GetValue(selectedOptionIndex - 1);
                        appSettings.Save();
                    }
                    break;
                case ConsoleKey.RightArrow: 
                    if(selectedOptionIndex != -1 && selectedOptionIndex < menuItems[selectedItemKey].Length - 1)
                    {
                        appSettings[selectedItemKey] = menuItems[selectedItemKey].GetValue(selectedOptionIndex + 1);
                        appSettings.Save();
                    }
                    break;
                case ConsoleKey.Enter:
                    if (selectedItemKey.Contains("Directory"))
                    {
                        Console.CursorVisible = true;
                        Console.SetCursorPosition(0, selectedItemIndex);
                        Console.Write("{0}{1}", Regex.Replace(selectedItemKey, "(\\B[A-Z])", " $1").ToUpper(), new string(' ', Console.WindowWidth - Regex.Replace(selectedItemKey, "(\\B[A-Z])", " $1").Length));
                        Console.SetCursorPosition(Regex.Replace(selectedItemKey, "(\\B[A-Z])", " $1").Length + 1, selectedItemIndex);
                        Console.BackgroundColor = ConsoleColor.Gray;
                        Console.ForegroundColor = ConsoleColor.Black;
                        string path = Console.ReadLine().TrimEnd('\\') + "\\";
                        Console.ResetColor();
                        Console.CursorVisible = false;
                        if(Directory.Exists(path) && path != "\\")
                        {
                            appSettings[selectedItemKey] = path;
                            appSettings.XslTransformFileName = File.Exists(appSettings.XslFilesDirectory + "\\" + appSettings.XslTransformFileName) ? appSettings.XslTransformFileName : Directory.GetFiles(appSettings.XslFilesDirectory, "*.xsl").Length > 0 ? Directory.GetFiles(appSettings.XslFilesDirectory, "*.xsl").Select(Path.GetFileName).FirstOrDefault() : Directory.GetFiles(appSettings.XslFilesDirectory, "*.xslt").Length > 0 ? Directory.GetFiles(appSettings.XslFilesDirectory, "*.xslt").Select(Path.GetFileName).FirstOrDefault() : "";
                            appSettings.XmlFileNameToTransform = File.Exists(appSettings.XmlInputFilesDirectory + "\\" + appSettings.XmlFileNameToTransform) ? appSettings.XmlFileNameToTransform : Directory.GetFiles(appSettings.XmlInputFilesDirectory, "*.xml").Length > 0 ? Directory.GetFiles(appSettings.XmlInputFilesDirectory, "*.xml").Select(Path.GetFileName).FirstOrDefault() : "";
                            appSettings.Save();
                            menuItems[selectedItemKey] = new string[] { path };
                            menuItems["XslTransformFileName"] = Directory.GetFiles(appSettings.XslFilesDirectory, "*.xsl").Select(Path.GetFileName).Union(Directory.GetFiles(appSettings.XslFilesDirectory, "*.xsl").Select(Path.GetFileName)).OrderBy(filename => filename).ToArray();
                            menuItems["XmlFileNameToTransform"] = Directory.GetFiles(appSettings.XmlInputFilesDirectory, "*.xml").Select(Path.GetFileName).OrderBy(filename => filename).ToArray();

                        }
                        else if(path != "\\")
                        {
                            Console.SetCursorPosition(0, selectedItemIndex);
                            Console.Write("{0}{1}", Regex.Replace(selectedItemKey, "(\\B[A-Z])", " $1").ToUpper(), new string(' ', Console.WindowWidth - Regex.Replace(selectedItemKey, "(\\B[A-Z])", " $1").Length));
                            Console.SetCursorPosition(Regex.Replace(selectedItemKey, "(\\B[A-Z])", " $1").Length + 1, selectedItemIndex);
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("incorrect directory path");
                            consoleKeyInfo = Console.ReadKey(true);
                        }
                    }
                    if (selectedItemIndex == menuItems.Count - 1 && File.Exists(appSettings.XslFilesDirectory + "\\" + appSettings.XslTransformFileName) && File.Exists(appSettings.XmlInputFilesDirectory + "\\" + appSettings.XmlFileNameToTransform))
                    {
                        Console.SetCursorPosition(Regex.Replace(selectedItemKey, "(\\B[A-Z])", " $1").Length + 1, selectedItemIndex);
                        Console.ResetColor();
                        Console.WriteLine(Regex.Replace(selectedItemKey, "(\\B[A-Z])", " $1").ToUpper() + Environment.NewLine);
                        Console.ForegroundColor = ConsoleColor.Yellow;    
                    }
                    break;
            }
        }

        private static void DoXmlTransform(string fileName)
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(appSettings.XslFilesDirectory + "\\" + appSettings.XslTransformFileName, new XsltSettings(true, true), new XmlUrlResolver());
                if (appSettings.ForceIndentsAndBreakLinesInXmlOutput == "ON")
                {
                    using (XmlWriter writer = XmlWriter.Create(appSettings.XmlOutputFilesDirectory + "\\" + fileName + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".xml", new XmlWriterSettings { Indent = true, IndentChars = "  ", NewLineChars = "\r\n", NewLineHandling = NewLineHandling.Replace, Encoding = new System.Text.UTF8Encoding(false) }))
                    {
                        xslt.Transform(appSettings.XmlInputFilesDirectory + "\\" + appSettings.XmlFileNameToTransform, writer);
                    }
                }
                else
                {
                    xslt.Transform(appSettings.XmlInputFilesDirectory + "\\" + appSettings.XmlFileNameToTransform, appSettings.XmlOutputFilesDirectory + "\\" + fileName + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".xml");
                }
            }
            catch (Exception exception)
            {
                transformFailed = true;
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(exception.Message);
            }
        }


        static void Main(string[] args)
        {
            Console.SetWindowSize(150 > Console.LargestWindowWidth ? Console.LargestWindowWidth : 150, 30 > Console.LargestWindowHeight ? Console.LargestWindowHeight : 30);
             
            appSettings = Settings.Default;
            InitialiseSettings(appSettings);
            InitialiseMenu();

            do
            {
                selectedItemIndex = menuItems.Count - 1;
                do
                {
                    DrawMenu();
                    UserInput(menuItems.Keys.ElementAt(selectedItemIndex));
                } while (consoleKeyInfo.Key != ConsoleKey.Enter || selectedItemIndex != menuItems.Count - 1 || !File.Exists(appSettings.XslFilesDirectory + "\\" + appSettings.XslTransformFileName) || !File.Exists(appSettings.XmlInputFilesDirectory + "\\" + appSettings.XmlFileNameToTransform));

                transformFailed = false;
                Thread transformThread = new Thread(() => DoXmlTransform(Regex.Replace(Regex.Replace(appSettings.XmlFileNameToTransform, @"[\d-]", " ").Trim(), @"\s+", "-").Replace(".xml", "")));
                transformThread.Start();

                Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
                while (transformThread.IsAlive)
                {
                    Console.Write("Transform is running. Time taken: {0:00}m:{1:00}s", watch.Elapsed.Minutes, watch.Elapsed.Seconds);
                    Console.SetCursorPosition(0, Console.CursorTop);
                }
                watch.Stop();

                if (!transformFailed)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("Transform succeed. Total time taken: {0:00}m:{1:00}s\tFile size: {2:n0}kB ({3:0.00}MB)" + Environment.NewLine + Environment.NewLine, watch.Elapsed.Minutes, watch.Elapsed.Seconds, (decimal)new DirectoryInfo(appSettings.XmlOutputFilesDirectory).GetFiles("*.xml").OrderByDescending(f => f.LastWriteTime).First().Length / 1024, (decimal)new DirectoryInfo(appSettings.XmlOutputFilesDirectory).GetFiles("*.xml").OrderByDescending(f => f.LastWriteTime).First().Length / 1048576);
                }


                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.WriteLine("\nPress ESC key exit. Press any other key to continue work with application.");
                Console.ResetColor();
                consoleKeyInfo = Console.ReadKey(true);
                Console.Clear();
            } while (consoleKeyInfo.Key != ConsoleKey.Escape);
        
        }

    }
}
