using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ConsoleApplication
{
    internal static class ClothesDownloader
    {
        internal static async Task DownloadClothesAsync()
        {
            string release;
            HttpClient httpClient_Clothes = new HttpClient();
            httpClient_Clothes.DefaultRequestHeaders.Add("user-agent", CommonConfig.UserAgent);

            try
            {
                HttpResponseMessage res = await httpClient_Clothes.GetAsync("https://www.habbo.com/gamedata/external_variables/1");
                string source = (await res.Content.ReadAsStringAsync());

                foreach (string Line in source.Split(Environment.NewLine.ToCharArray()))
                {
                    if (!Line.Contains("flash.client.url="))
                    {
                        continue;
                    }
                    release = Line.Substring(0, Line.Length - 1).Split('/')[4];
                    Console.WriteLine("We are going to download from release: " + release);

                    string CurrentDirectory;
                    string HotelVersion;
                    string GordonDirectory;
                    string figuremap;
                    string figuremapfile;
                    string figuredata;
                    string DownloadDirectory;

                    CurrentDirectory = Environment.CurrentDirectory;

                    HotelVersion = release;
                    GordonDirectory = "https://images.habbo.com/gordon/";
                    DownloadDirectory = CurrentDirectory + @"\clothes\";

                    figuremap = "https://images.habbo.com/gordon/" + HotelVersion + "/figuremap.xml";
                    figuredata = "http://habbo.com/gamedata/figuredata/1";

                    WebClient WebClient = new WebClient();
                    WebClient.Headers.Add("user-agent", CommonConfig.UserAgent);

                    try
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        WebClient.DownloadFile(figuredata, DownloadDirectory + @"\figuredata.xml");
                        Console.WriteLine("Downloaded figuredata.xml\n");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error! Cant download file");
                        Console.WriteLine();
                        Console.WriteLine("figuredata.xml \ndownload url: " + figuredata);
                        Console.WriteLine();
                        Console.WriteLine("Trying download figuremap.xml");
                        Console.WriteLine();
                    }

                    try
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        WebClient.DownloadFile(figuremap, DownloadDirectory + @"\figuremap.xml");
                        Console.WriteLine("Downloaded figuremap.xml\n");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error! Cant download file! Please check your Habbo_Downloader.exe.config is OK!");
                        Console.WriteLine();
                        Console.WriteLine("figuremap.xml download url: " + figuremap);
                        Console.WriteLine();
                    }

                    figuremapfile = DownloadDirectory + @"\figuremap.xml";

                    try
                    {
                        string file = File.ReadAllText(figuremapfile);
                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error! Cant read file: " + figuremapfile);
                        Console.WriteLine();
                    }

                    StringBuilder sb = new StringBuilder();
                    using (StreamReader sr = new StreamReader(figuremapfile))
                    {
                        String line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            sb.AppendLine(line);
                        }
                    }
                    string allines = sb.ToString();

                    StringBuilder output = new StringBuilder();
                    int DownloadCount = 0;
                    using (XmlReader reader = XmlReader.Create(new StringReader(allines)))
                    {
                        while (reader.Read())
                        {
                            if (reader.IsStartElement())
                            {
                                switch (reader.Name)
                                {
                                    case "map":
                                        break;
                                    case "lib":
                                        string id = reader["id"];
                                        if (id != null)
                                        {
                                            try
                                            {
                                                if (!File.Exists(DownloadDirectory + id + ".swf"))
                                                {
                                                    WebClient.DownloadFile(GordonDirectory + HotelVersion + "/" + id + ".swf", DownloadDirectory + id + ".swf");
                                                    Console.ForegroundColor = ConsoleColor.Green;
                                                    Console.WriteLine("Downloaded: " + id);
                                                    Console.ForegroundColor = ConsoleColor.Gray;
                                                    DownloadCount++;
                                                }
                                            }
                                            catch
                                            {
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.WriteLine("Error when downloading file: " + id);
                                                Console.ForegroundColor = ConsoleColor.Gray;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }

                    if (DownloadCount > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine();
                        Console.WriteLine("Downloaded " + DownloadCount + " new clothes!");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("You have the latest clothes!");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }

                    Console.WriteLine();
                    Console.WriteLine("All has been done!");
                    Console.WriteLine();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}