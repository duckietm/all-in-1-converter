using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace ConsoleApplication
{
    internal class ConsoleCommandHandeling
    {
        internal static void InvokeCommand(string inputData)
        {
            Console.WriteLine();
            try
            {
                string[] starupconsole = inputData.Split(new char[1]
                {
                    ' '
                });
                switch (starupconsole[0].ToLower())
                {
                    case "download":
                        Console.WriteLine("Starting Download...");
                        switch (starupconsole[1].ToLower())
                        {
                            case "reception":
                                if (!Directory.Exists("./temp"))
                                {
                                    Directory.CreateDirectory("./temp");
                                }
                                if (!Directory.Exists("./reception"))
                                {
                                    Directory.CreateDirectory("./reception");
                                }
                                if (!Directory.Exists("./reception/catalogue"))
                                {
                                    Directory.CreateDirectory("./reception/catalogue");
                                }
                                if (!Directory.Exists("./reception/web_promo_small"))
                                {
                                    Directory.CreateDirectory("./reception/web_promo_small");
                                }
                                Console.WriteLine("This downloads not all images. Only the ones that are defined in the external_variables");
                                Console.WriteLine("Run this once in a while to collect all images!");
                                Console.WriteLine("Catalogue Teasers used on the reception are stored in /catalogue/");
                                Console.WriteLine("web_promo_small images used on the reception are stored in /reception/web_promo_small");
                                Console.WriteLine();
                                string str2 = "./temp/external_variables.txt";
                                Console.WriteLine("Downloading external variables");
                                WebClient webClient1 = new WebClient();
                                webClient1.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webClient1.DownloadFile("https://www.habbo.com/gamedata/external_variables/", str2);
                                Console.WriteLine("Lets start downloading!");
                                int num1 = 0;
                                using (new StreamWriter("./temp/external_variables2.txt", true))
                                {
                                    using (StreamReader streamReader = new StreamReader(str2))
                                    {
                                        string string3;
                                        while ((string3 = streamReader.ReadLine()) != null)
                                        {
                                            if (string3.Contains("reception/"))
                                            {
                                                string[] receptionarray = string3.Split(new string[1]
                                                {
                                                    "reception/"
                                                }, StringSplitOptions.None);
                                                try
                                                {
                                                    if (receptionarray[1].Contains(".png,"))
                                                    {
                                                        string[] reception1 = receptionarray[1].Split(new string[1]
                                                        {
                                                            ","
                                                        }, StringSplitOptions.None);
                                                        if (!System.IO.File.Exists("./reception/" + reception1[0]))
                                                        {
                                                            string receptionurl = System.Configuration.ConfigurationManager.AppSettings["receptionurl"];
                                                            webClient1.DownloadFile(receptionurl + "/" + reception1[0], "./reception/" + reception1[0]);
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Downloading " + reception1[0]);
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                            ++num1;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                            Console.WriteLine(reception1[0] + " already exists!");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        string[] reception2 = receptionarray[1].Split(new string[1]
                                                        {
                                                            ";"
                                                        }, StringSplitOptions.None);
                                                        if (!System.IO.File.Exists("./reception/" + reception2[0]))
                                                        {
                                                            string receptionurl = System.Configuration.ConfigurationManager.AppSettings["receptionurl"];
                                                            webClient1.DownloadFile(receptionurl + "/" + reception2[0], "./reception/" + reception2[0]);
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Downloading " + reception2[0]);
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                            ++num1;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                            Console.WriteLine(reception2[0] + " already exists!");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                }
                                                catch
                                                {
                                                    if (receptionarray[1].Contains(".png,"))
                                                    {
                                                        string[] catalogue3 = receptionarray[1].Split(new string[1]
                                                        {
                                                            ","
                                                        }, StringSplitOptions.None);
                                                        Console.ForegroundColor = ConsoleColor.Red;
                                                        Console.WriteLine("Error downloading " + catalogue3[0]);
                                                        Console.ForegroundColor = ConsoleColor.Gray;
                                                    }
                                                    else
                                                    {
                                                        string[] catalogue4 = receptionarray[1].Split(new string[1]
                                                        {
                                                            ";"
                                                        }, StringSplitOptions.None);
                                                        Console.ForegroundColor = ConsoleColor.Red;
                                                        Console.WriteLine("Error downloading " + catalogue4[0]);
                                                        Console.ForegroundColor = ConsoleColor.Gray;
                                                    }
                                                }
                                            }
                                            if (string3.Contains("catalogue/"))
                                            {
                                                string[] cataloguearray = string3.Split(new string[1]
                                                {
                                                    "catalogue/"
                                                }, StringSplitOptions.None);
                                                try
                                                {
                                                    if ((cataloguearray[1].Contains(".png,") ? 0 : (!cataloguearray[1].Contains(".gif,") ? 1 : 0)) == 0)
                                                    {
                                                        string[] catalogue1 = cataloguearray[1].Split(new string[1]
                                                        {
                                                            ","
                                                        }, StringSplitOptions.None);
                                                        if (!System.IO.File.Exists("./reception/catalogue/" + catalogue1[0]))
                                                        {
                                                            string catalogurl = System.Configuration.ConfigurationManager.AppSettings["catalogurl"];
                                                            webClient1.DownloadFile(catalogurl + "/" + catalogue1[0], "./reception/catalogue/" + catalogue1[0]);
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Downloading catalogue " + catalogue1[0]);
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                            ++num1;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                            Console.WriteLine(catalogue1[0] + " already exists!");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        string[] catalogue2 = cataloguearray[1].Split(new string[1]
                                                        {
                                                            ";"
                                                        }, StringSplitOptions.None);
                                                        if (!System.IO.File.Exists("./reception/catalogue/" + catalogue2[0]))
                                                        {
                                                            string catalogurl = System.Configuration.ConfigurationManager.AppSettings["catalogurl"];
                                                            webClient1.DownloadFile(catalogurl + "/" + catalogue2[0], "./reception/catalogue/" + catalogue2[0]);
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Downloading catalogue " + catalogue2[0]);
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                            ++num1;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                            Console.WriteLine(catalogue2[0] + " already exists!");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                }
                                                catch
                                                {
                                                    if ((cataloguearray[1].Contains(".png,") ? 0 : (!cataloguearray[1].Contains(".gif,") ? 1 : 0)) == 0)
                                                    {
                                                        string[] catalogue3 = cataloguearray[1].Split(new string[1]
                                                        {
                                                            ","
                                                        }, StringSplitOptions.None);
                                                        Console.ForegroundColor = ConsoleColor.Red;
                                                        Console.WriteLine("Error downloading catalogue " + catalogue3[0]);
                                                        Console.ForegroundColor = ConsoleColor.Gray;
                                                    }
                                                    else
                                                    {
                                                        string[] catalogue4 = cataloguearray[1].Split(new string[1]
                                                        {
                                                            ";"
                                                        }, StringSplitOptions.None);
                                                        Console.ForegroundColor = ConsoleColor.Red;
                                                        Console.WriteLine("Error downloading catalogue " + catalogue4[0]);
                                                        Console.ForegroundColor = ConsoleColor.Gray;
                                                    }
                                                }
                                            }
                                            if (string3.Contains("web_promo_small/"))
                                            {
                                                string[] webpromoarray = string3.Split(new string[1]
                                                {
                                                    "web_promo_small/"
                                                }, StringSplitOptions.None);
                                                try
                                                {
                                                    if ((webpromoarray[1].Contains(".png,") ? 0 : (!webpromoarray[1].Contains(".gif,") ? 1 : 0)) == 0)
                                                    {
                                                        string[] webpromo1 = webpromoarray[1].Split(new string[1]
                                                        {
                                                            ","
                                                        }, StringSplitOptions.None);
                                                        if (!System.IO.File.Exists("./reception/web_promo_small/" + webpromo1[0]))
                                                        {
                                                            string promosmallurl = System.Configuration.ConfigurationManager.AppSettings["promosmallurl"];
                                                            webClient1.DownloadFile(promosmallurl + "/" + webpromo1[0], "./reception/web_promo_small/" + webpromo1[0]);
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Downloading web_promo_small " + webpromo1[0]);
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                            ++num1;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                            Console.WriteLine(webpromo1[0] + " already exists!");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        string[] webpromo2 = webpromoarray[1].Split(new string[1]
                                                        {
                                                            ";"
                                                        }, StringSplitOptions.None);
                                                        if (!System.IO.File.Exists("./reception/web_promo_small/" + webpromo2[0]))
                                                        {
                                                            string promosmallurl = System.Configuration.ConfigurationManager.AppSettings["promosmallurl"];
                                                            webClient1.DownloadFile(promosmallurl + "/" + webpromo2[0], "./reception/web_promo_small/" + webpromo2[0]);
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Downloading web_promo_small " + webpromo2[0]);
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                            ++num1;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                            Console.WriteLine(webpromo2[0] + " already exists!");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                }
                                                catch
                                                {
                                                    if ((webpromoarray[1].Contains(".png,") ? 0 : (!webpromoarray[1].Contains(".gif,") ? 1 : 0)) == 0)
                                                    {
                                                        string[] webpromo3 = webpromoarray[1].Split(new string[1]
                                                        {
                                                            ","
                                                        }, StringSplitOptions.None);
                                                        Console.ForegroundColor = ConsoleColor.Red;
                                                        Console.WriteLine("Error downloading web_promo_small " + webpromo3[0]);
                                                        Console.ForegroundColor = ConsoleColor.Gray;
                                                    }
                                                    else
                                                    {
                                                        string[] strArray3 = webpromoarray[1].Split(new string[1]
                                                        {
                                                            ";"
                                                        }, StringSplitOptions.None);
                                                        Console.ForegroundColor = ConsoleColor.Red;
                                                        Console.WriteLine("Error downloading web_promo_small " + strArray3[0]);
                                                        Console.ForegroundColor = ConsoleColor.Gray;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("Finished downloading images");
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                    goto temp;
                                }
                            case "furniture":
                                Console.WriteLine("Furniture Download Started");
                                break;

                            case "icons":
                                Console.WriteLine("Catalogue Icons Download Started");
                                if (!Directory.Exists("./icons"))
                                {
                                    Directory.CreateDirectory("./icons");
                                }
                                WebClient webClient2 = new WebClient();
                                webClient2.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                int num2 = 1;
                                int num3 = 1;
                                while (num3 <= 20)
                                {
                                    try
                                    {
                                        if (!System.IO.File.Exists("./icons/icon_" + (object)num2 + ".png"))
                                        {
                                            string catalogiconurl = System.Configuration.ConfigurationManager.AppSettings["catalogiconurl"];
                                            webClient2.DownloadFile(catalogiconurl + (object)num2 + ".png", "./icons/icon_" + (object)num2 + ".png");
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine("Downloaded Icon " + (object)num2);
                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            ++num2;
                                            num3 = 1;
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                            Console.WriteLine("Icon " + (object)num2 + " already exists!");
                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            ++num2;
                                            num3 = 1;
                                        }
                                    }
                                    catch
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("Error downloading icon " + (object)num2);
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                        ++num3;
                                        ++num2;
                                    }
                                }
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Finished downloading icons!");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                goto readline;

                            case "mp3":
                                Console.WriteLine("MP3 Sounds Download Started");
                                if (!Directory.Exists("./mp3"))
                                {
                                    Directory.CreateDirectory("./mp3");
                                }
                                WebClient webClient3 = new WebClient();
                                webClient3.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                int num4 = 1;
                                int num5 = 1;
                                while (num5 <= 20)
                                {
                                    try
                                    {
                                        if (!System.IO.File.Exists("./mp3/sound_machine_sample_" + (object)num4 + ".mp3"))
                                        {
                                            string soundmachineurl = System.Configuration.ConfigurationManager.AppSettings["soundmachineurl"];
                                            webClient3.DownloadFile(soundmachineurl + (object)num4 + ".mp3", "./mp3/sound_machine_sample_" + (object)num4 + ".mp3");
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine("Downloaded MP3 " + (object)num4);
                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            ++num4;
                                            num5 = 1;
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                            Console.WriteLine("MP3 " + (object)num4 + " already exists!");
                                            Console.ForegroundColor = ConsoleColor.Gray;
                                            ++num4;
                                            num5 = 1;
                                        }
                                    }
                                    catch
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("Error downloading MP3 " + (object)num4);
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                        ++num5;
                                        ++num4;
                                    }
                                }
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Finished downloading MP3!");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                goto readline;

                            case "furnidata":
                                if (!Directory.Exists("./files"))
                                {
                                    Directory.CreateDirectory("./files");
                                }
                                Console.WriteLine("Saving furnidata...");
                                WebClient webClient = new WebClient();
                                string furnidataTXT = System.Configuration.ConfigurationManager.AppSettings["furnidataTXT"];
                                string furnidataXML = System.Configuration.ConfigurationManager.AppSettings["furnidataXML"];
                                webClient.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webClient.DownloadFile(furnidataTXT, "./files/furnidata.txt");
                                webClient.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webClient.DownloadFile(furnidataXML, "./files/furnidata_xml.xml");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Furnidata Saved");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                goto readline;

                            case "effects":
                                string release_effect;
                                HttpClient httpClient_version_effect = new HttpClient();
                                httpClient_version_effect.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.86 Safari/537.36");
                                Task.Run(async () =>
                                 {
                                     try
                                     {
                                         string externalvarsurl1 = System.Configuration.ConfigurationManager.AppSettings["externalvarsurl"];
                                         HttpResponseMessage res = await httpClient_version_effect.GetAsync(externalvarsurl1);
                                         string source = (await res.Content.ReadAsStringAsync());
                                         foreach (string Line in source.Split(Environment.NewLine.ToCharArray()))
                                         {
                                             if (!Line.Contains("flash.client.url="))
                                             {
                                                 continue;
                                             }
                                             release_effect = Line.Substring(0, Line.Length - 1).Split('/')[4];
                                             if (!Directory.Exists("./files"))
                                             {
                                                 Directory.CreateDirectory("./files");
                                             }
                                             Console.ForegroundColor = ConsoleColor.Blue;
                                             Console.WriteLine("Downloading Effects version : " + release_effect);
                                             string effecturl = System.Configuration.ConfigurationManager.AppSettings["effecturl"];
                                             WebClient webClient_effect = new WebClient();
                                             webClient_effect.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                             webClient_effect.DownloadFile(effecturl + "/" + release_effect + "/effectmap.xml", "./effect/effectmap.xml");
                                             webClient_effect.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                             webClient_effect.DownloadFile(effecturl + "/" + release_effect + "/HabboAvatarActions.xml", "./effect/HabboAvatarActions.xml");
                                             Console.ForegroundColor = ConsoleColor.Green;
                                             Console.WriteLine("Downloading Effects Saved");
                                             Console.ForegroundColor = ConsoleColor.Gray;
                                         }
                                     }
                                     catch (Exception e)
                                     {
                                         Console.WriteLine(e);
                                     }
                                 });
                                goto readline;

                            case "texts":
                                if (!Directory.Exists("./files"))
                                {
                                    Directory.CreateDirectory("./files");
                                }
                                Console.WriteLine("Saving external flash texts...");
                                string externaltexturl1 = System.Configuration.ConfigurationManager.AppSettings["externaltexturl"];
                                WebClient webclient_externaltexturl = new WebClient();
                                webclient_externaltexturl.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webclient_externaltexturl.DownloadFile(externaltexturl1, "./files/external_flash_texts.txt");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Furnidata Saved");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                goto readline;

                            case "custfurni":
                                if (!Directory.Exists("./hof_furni_custom"))
                                {
                                    Directory.CreateDirectory("./hof_furni_custom");
                                }
                                if (!Directory.Exists("./hof_furni_custom/config"))
                                {
                                    Directory.CreateDirectory("./hof_furni_custom/config");
                                }
                                if (!Directory.Exists("./hof_furni_custom/icons"))
                                {
                                    Directory.CreateDirectory("./hof_furni_custom/icons");
                                }
                                if (!Directory.Exists("./temp"))
                                {
                                    Directory.CreateDirectory("./temp");
                                }
                                string customfurnidata = System.Configuration.ConfigurationManager.AppSettings["customfurnidata"];
                                string customproductdataurl = System.Configuration.ConfigurationManager.AppSettings["customproductdataurl"];
                                string customfurnidataxml = System.Configuration.ConfigurationManager.AppSettings["customfurnidataxml"];
                                string str8 = "./temp/furnidata.txt";
                                WebClient webClient8 = new WebClient();
                                webClient8.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webClient8.DownloadFile(customfurnidata, str8);
                                Console.WriteLine("Saving productdata...");
                                webClient8.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webClient8.DownloadFile(customproductdataurl, "./hof_furni_custom/config/productdata.txt");
                                Console.WriteLine("Saving Furnidata...");
                                webClient8.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webClient8.DownloadFile(customfurnidata, "./hof_furni_custom/config/furnidata.txt");
                                webClient8.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webClient8.DownloadFile(customfurnidataxml, "./hof_furni_custom/config/furnidata_xml.xml");

                                Console.WriteLine("Custom Furnidata Downloaded...");
                                int num10 = 0;
                                System.IO.File.WriteAllLines(str8, Enumerable.ToArray<string>(Enumerable.Select<string, string>((IEnumerable<string>)System.IO.File.ReadAllLines(str8), (Func<string, string>)(line => string.Join("\n", line.Split(new string[1]
                              {
                                  "],"
                              }, StringSplitOptions.RemoveEmptyEntries))))));
                                System.IO.File.WriteAllLines(str8, Enumerable.ToArray<string>(Enumerable.Select<string, string>((IEnumerable<string>)System.IO.File.ReadAllLines(str8), (Func<string, string>)(line => string.Join("\n", line.Split(new string[1]
                              {
                                  "["
                              }, StringSplitOptions.RemoveEmptyEntries))))));
                                System.IO.File.WriteAllLines(str8, Enumerable.ToArray<string>(Enumerable.Select<string, string>((IEnumerable<string>)System.IO.File.ReadAllLines(str8), (Func<string, string>)(line => string.Join("", line.Split(new string[1]
                              {
                                  "\""
                              }, StringSplitOptions.RemoveEmptyEntries))))));
                                using (StreamReader streamReader = new StreamReader(str8))
                                {
                                    Console.WriteLine("Begin downloading Custom Furniture...");
                                    Thread.Sleep(3000);
                                    string str9;
                                    while ((str9 = streamReader.ReadLine()) != null)
                                    {
                                        string[] direcory_path = str9.Split(new string[1]
                                        {
                                            ","
                                        }, StringSplitOptions.None);
                                        string[] furniture_name = direcory_path[2].Split(new string[1]
                                        {
                                            "*"
                                        }, StringSplitOptions.None);
                                        string customfileextension = System.Configuration.ConfigurationManager.AppSettings["customfileextension"];
                                        string customfurnitureurl = System.Configuration.ConfigurationManager.AppSettings["customfurnitureurl"];
                                        string customiconureurl = System.Configuration.ConfigurationManager.AppSettings["customiconureurl"];

                                        if (!System.IO.File.Exists("./hof_furni_custom/" + furniture_name[0] + customfileextension))
                                        {
                                            try
                                            {
                                                Console.ForegroundColor = ConsoleColor.Green;
                                                Console.WriteLine("Furniture Downloading: " + furniture_name[0] + customfileextension);
                                                Console.ForegroundColor = ConsoleColor.DarkGreen;
                                                Console.WriteLine("Icon Downloading: " + furniture_name[0] + ".png");
                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                webClient8.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                webClient8.DownloadFile(customfurnitureurl + "/" + furniture_name[0] + ".swf", "hof_furni_custom/" + furniture_name[0] + customfileextension);
                                                webClient8.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                webClient8.DownloadFile(customiconureurl + "/" + furniture_name[0] + "_icon.png", "hof_furni_custom/icons/" + furniture_name[0] + ".png");
                                                    }
                                            catch
                                            {
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.WriteLine("Error while downloading: " + furniture_name[0]);
                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                --num10;
                                            }
                                            ++num10;
                                        }
                                    }
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("Downloading Custom Furniture Done!");
                                    Console.WriteLine("We've downloaded " + (object)num10 + " new furniture!");
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                }
                                goto readline;


                            case "productdata":
                                if (!Directory.Exists("./files"))
                                {
                                    Directory.CreateDirectory("./files");
                                }
                                Console.WriteLine("Saving productdata...");
                                string productdataurl = System.Configuration.ConfigurationManager.AppSettings["productdataurl"];
                                WebClient webclient_productdata = new WebClient();
                                webclient_productdata.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webclient_productdata.DownloadFile(productdataurl, "./files/productdata.txt");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Productdata Saved");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                goto readline;

                            case "variables":
                                if (!Directory.Exists("./files"))
                                {
                                    Directory.CreateDirectory("./files");
                                }
                                Console.WriteLine("Saving External Variables...");
                                string externalvarsurl2 = System.Configuration.ConfigurationManager.AppSettings["externalvarsurl"];
                                WebClient webclient_variables = new WebClient();
                                webclient_variables.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webclient_variables.DownloadFile(externalvarsurl2, "./files/external_variables.txt");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("External Variables Saved");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                goto readline;

                            case "quests":
                                string externaltexturl2 = System.Configuration.ConfigurationManager.AppSettings["externaltexturl"];
                                int num6 = 0;
                                string str4 = "./temp/external_texts.txt";
                                if (!Directory.Exists("./temp"))
                                {
                                    Directory.CreateDirectory("./temp");
                                }
                                System.IO.File.Delete(str4);
                                WebClient webclient_quests = new WebClient();
                                webclient_quests.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webclient_quests.DownloadFile(externaltexturl2, str4);
                                Console.WriteLine("External Flash Texts Downloaded...");
                                Console.WriteLine("Begin parsing...");
                                using (StreamReader streamReader = new StreamReader(str4))
                                {
                                    Thread.Sleep(1000);
                                    string str3;
                                    while ((str3 = streamReader.ReadLine()) != null)
                                    {
                                        if (str3.StartsWith("quests."))
                                        {
                                            string[] strArray2 = str3.Split(new string[1]
                                            {
                                                "."
                                            }, StringSplitOptions.None);
                                            try
                                            {
                                                string str5 = strArray2[1].ToLower();
                                                string str6 = strArray2[2].ToLower();
                                                if ((System.IO.File.Exists("quests/" + str5 + "_" + str6 + ".png") ? 1 : (strArray2[2].Contains("=") ? 1 : 0)) == 0)
                                                {
                                                    try
                                                    {
                                                        string questsurl1 = System.Configuration.ConfigurationManager.AppSettings["questsurl"];
                                                        ++num6;
                                                        Console.ForegroundColor = ConsoleColor.Green;
                                                        Console.WriteLine("Downloading: " + strArray2[1] + "_" + strArray2[2] + ".png");
                                                        Console.ForegroundColor = ConsoleColor.Gray;
                                                        webclient_quests.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                        webclient_quests.DownloadFile(questsurl1 + "/" + strArray2[1] + "_" + strArray2[2] + ".png", "quests/" + strArray2[1] + "_" + strArray2[2] + ".png");
                                                    }
                                                    catch
                                                    {
                                                        Console.ForegroundColor = ConsoleColor.Red;
                                                        Console.WriteLine("Error while downloading: " + strArray2[1] + "_" + strArray2[2] + ".png");
                                                        Console.ForegroundColor = ConsoleColor.Gray;
                                                        --num6;
                                                        try
                                                        {
                                                            string questsurl2 = System.Configuration.ConfigurationManager.AppSettings["questsurl"];
                                                            ++num6;
                                                            Console.ForegroundColor = ConsoleColor.Magenta;
                                                            Console.WriteLine("Retry Downloading: " + str5 + "_" + str6 + ".png");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                            webclient_quests.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                            webclient_quests.DownloadFile(questsurl2 + str5 + "_" + str6 + ".png", "quests/" + str5 + "_" + str6 + ".png");
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                                                            Console.WriteLine("Retry Error while downloading: " + str5 + "_" + str6 + ".png");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                            --num6;
                                                        }
                                                    }
                                                }
                                                else if (strArray2[2].Contains("="))
                                                {
                                                    Console.ForegroundColor = ConsoleColor.Red;
                                                    Console.WriteLine(str5 + "_" + str6 + ".png is not valid!");
                                                    Console.ForegroundColor = ConsoleColor.Gray;
                                                }
                                                else
                                                {
                                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                    Console.WriteLine(str5 + "_" + str6 + ".png already exists!");
                                                    Console.ForegroundColor = ConsoleColor.Gray;
                                                }
                                                if (!System.IO.File.Exists("quests/" + strArray2[1] + ".png"))
                                                {
                                                    try
                                                    {
                                                        string questsurl3 = System.Configuration.ConfigurationManager.AppSettings["questsurl"];
                                                        ++num6;
                                                        Console.ForegroundColor = ConsoleColor.Green;
                                                        Console.WriteLine("Downloading: " + strArray2[1] + ".png");
                                                        Console.ForegroundColor = ConsoleColor.Gray;
                                                        webclient_quests.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                        webclient_quests.DownloadFile(questsurl3 + "/" + strArray2[1] + ".png", "quests/" + strArray2[1] + ".png");
                                                    }
                                                    catch
                                                    {
                                                        Console.ForegroundColor = ConsoleColor.Red;
                                                        Console.WriteLine("Error while downloading: " + strArray2[1] + ".png");
                                                        Console.ForegroundColor = ConsoleColor.Gray;
                                                        --num6;
                                                        try
                                                        {
                                                            string questsurl4 = System.Configuration.ConfigurationManager.AppSettings["questsurl"];
                                                            ++num6;
                                                            Console.ForegroundColor = ConsoleColor.Magenta;
                                                            Console.WriteLine("Retry Downloading: " + str5 + ".png");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                            webclient_quests.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                            webclient_quests.DownloadFile(questsurl4 + "/" + str5 + ".png", "quests/" + str5 + ".png");
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                                                            Console.WriteLine("Retry Downloading: " + str5 + ".png failed!");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                            --num6;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                    Console.WriteLine(strArray2[1] + ".png already exists!");
                                                    Console.ForegroundColor = ConsoleColor.Gray;
                                                }
                                            }
                                            catch
                                            {
                                            }
                                        }
                                    }
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("Finished Downloading Quest images!");
                                    Console.WriteLine(" images have been downloaded!");
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                    goto temp;
                                }
                            case "badges":
                                if (!Directory.Exists("./badges"))
                                {
                                    Directory.CreateDirectory("./badges");
                                }
                                if (!Directory.Exists("./temp"))
                                {
                                    Directory.CreateDirectory("./temp");
                                }
                                int length = Directory.GetFiles("./badges", "*.*", SearchOption.AllDirectories).Length;
                                WebClient webClient6 = new WebClient();

                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webClient6.DownloadFile("https://www.habbo.com/gamedata/external_flash_texts/1", "./temp/external_flash_texts_com.txt");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Start initializing badges from .com - Global");
                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webClient6.DownloadFile("https://www.habbo.fr/gamedata/external_flash_texts/1", "./temp/external_flash_texts_fr.txt");
                                Console.WriteLine("Start initializing badges from .fr - France");
                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webClient6.DownloadFile("https://www.habbo.fi/gamedata/external_flash_texts/1", "./temp/external_flash_texts_fi.txt");
                                Console.WriteLine("Start initializing badges from .fi - Finland");
                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webClient6.DownloadFile("https://www.habbo.es/gamedata/external_flash_texts/1", "./temp/external_flash_texts_es.txt");
                                Console.WriteLine("Start initializing badges from .es - Spain");
                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webClient6.DownloadFile("https://www.habbo.com/gamedata/external_flash_texts/1", "./temp/external_flash_texts_nl.txt");
                                Console.WriteLine("Start initializing badges from .nl - The Dutch");
                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webClient6.DownloadFile("https://www.habbo.de/gamedata/external_flash_texts/1", "./temp/external_flash_texts_de.txt");
                                Console.WriteLine("Start initializing badges from .de - Germany");
                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webClient6.DownloadFile("https://www.habbo.it/gamedata/external_flash_texts/1", "./temp/external_flash_texts_it.txt");
                                Console.WriteLine("Start initializing badges from .it - Italy");
                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webClient6.DownloadFile("https://www.habbo.com.tr/gamedata/external_flash_texts/1", "./temp/external_flash_texts_tr.txt");
                                Console.WriteLine("Start initializing badges from .com.tr - Turkey");
                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                webClient6.DownloadFile("https://www.habbo.com.br/gamedata/external_flash_texts/1", "./temp/external_flash_texts_br.txt");
                                Console.WriteLine("Start initializing badges from .com.br - Brasil");

                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.WriteLine("External Flash Texts Downloaded...");
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Begin Downloading .COM");
                                Thread.Sleep(2000);

                                using (StreamWriter streamWriter = new StreamWriter("./temp/external_flash_texts_com2.txt", true))
                                {
                                    using (StreamReader streamReader = new StreamReader("./temp/external_flash_texts_com.txt"))
                                    {
                                        string str3;
                                        while ((str3 = streamReader.ReadLine()) != null)
                                        {
                                            if (str3.StartsWith("badge_name_"))
                                            {
                                                streamWriter.WriteLine(str3);
                                                if (str3.StartsWith("badge_name_fb_"))
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    string[] strArray3 = strArray2[0].Split(new string[1]
                                                    {
                            "fb_"
                                                    }, StringSplitOptions.None);
                                                    if (strArray2[1].Contains("%roman%"))
                                                    {
                                                        int num7 = 1;
                                                        int num8 = 0;
                                                        while (num8 == 0)
                                                        {
                                                            if (!System.IO.File.Exists(string.Concat(new object[4]
                                                            {
                                (object) "./badges/",
                                (object) strArray3[1],
                                (object) num7,
                                (object) ".gif"
                                                            })))
                                                            {
                                                                try
                                                                {
                                                                    webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                    webClient6.DownloadFile(string.Concat(new object[4]
                                                                    {
                                    (object) "http://images-eussl.habbo.com/c_images/album1584/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }), string.Concat(new object[4]
                                                                    {
                                    (object) "./badges/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Green;
                                                                    Console.WriteLine(string.Concat(new object[4]
                                                                    {
                                    (object) "Downloading badge: ",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Gray;
                                                                    ++num7;
                                                                }
                                                                catch
                                                                {
                                                                    ++num8;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + (object)num7 + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                                ++num7;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[1] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[1] + ".gif", "./badges/" + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }

                                                        }
                                                        catch
                                                        {
                                                        }
                                                    }
                                                }
                                                if (str3.StartsWith("badge_name_al_"))
                                                {
                                                    try
                                                    {
                                                        string[] strArray2 = str3.Split(new string[1]
                                                        {
                              "badge_name_"
                                                        }, StringSplitOptions.None)[1].Split(new string[1]
                                                        {
                              "="
                                                        }, StringSplitOptions.None)[0].Split(new string[1]
                                                        {
                              "al_"
                                                        }, StringSplitOptions.None);
                                                        if (!System.IO.File.Exists("./badges/" + strArray2[1] + ".gif"))
                                                        {
                                                            webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                            webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[1] + ".gif", "./badges/" + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Downloading badge: " + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                            Console.WriteLine(strArray2[1] + ".gif already exists!");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    catch
                                                    {
                                                    }
                                                }
                                                if ((str3.StartsWith("badge_name_al") ? 1 : (str3.StartsWith("badge_name_fb") ? 1 : 0)) == 0)
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    if ((strArray2[0].Contains("_HHCA") ? 1 : (strArray2[0].Contains("_HHUK") ? 1 : 0)) == 0)
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[0] + ".gif", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray2[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }

                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge PNG: " + strArray2[0] + ".png and converting to gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        string str5 = "";
                                                        if (strArray2[0].Contains("_HHCA"))
                                                        {
                                                            str5 = "_HHCA";
                                                        }
                                                        else if (strArray2[0].Contains("_HHUK"))
                                                        {
                                                            str5 = "_HHUK";
                                                        }
                                                        string[] strArray3 = strArray2[0].Split(new string[1]
                                                        {
                              str5
                                                        }, StringSplitOptions.None);
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[0] + ".gif", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray3[0] + ".png", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray3[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                Console.WriteLine("Begin Downloading .FR");
                                Thread.Sleep(2000);
                                using (StreamWriter streamWriter = new StreamWriter("./temp/external_flash_texts_fr2.txt", true))
                                {
                                    using (StreamReader streamReader = new StreamReader("./temp/external_flash_texts_fr.txt"))
                                    {
                                        string str3;
                                        while ((str3 = streamReader.ReadLine()) != null)
                                        {
                                            if (str3.StartsWith("badge_name_"))
                                            {
                                                streamWriter.WriteLine(str3);
                                                if (str3.StartsWith("badge_name_fb_"))
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    string[] strArray3 = strArray2[0].Split(new string[1]
                                                    {
                            "fb_"
                                                    }, StringSplitOptions.None);
                                                    if (strArray2[1].Contains("%roman%"))
                                                    {
                                                        int num7 = 1;
                                                        int num8 = 0;
                                                        while (num8 == 0)
                                                        {
                                                            if (!System.IO.File.Exists(string.Concat(new object[4]
                                                            {
                                (object) "./badges/",
                                (object) strArray3[1],
                                (object) num7,
                                (object) ".gif"
                                                            })))
                                                            {
                                                                try
                                                                {
                                                                    webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                    webClient6.DownloadFile(string.Concat(new object[4]
                                                                    {
                                    (object) "http://images-eussl.habbo.com/c_images/album1584/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }), string.Concat(new object[4]
                                                                    {
                                    (object) "./badges/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Green;
                                                                    Console.WriteLine(string.Concat(new object[4]
                                                                    {
                                    (object) "Downloading badge: ",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Gray;
                                                                    ++num7;
                                                                }
                                                                catch
                                                                {
                                                                    ++num8;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + (object)num7 + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                                ++num7;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[1] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[1] + ".gif", "./badges/" + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                        }
                                                    }
                                                }
                                                if (str3.StartsWith("badge_name_al_"))
                                                {
                                                    try
                                                    {
                                                        string[] strArray2 = str3.Split(new string[1]
                                                        {
                              "badge_name_"
                                                        }, StringSplitOptions.None)[1].Split(new string[1]
                                                        {
                              "="
                                                        }, StringSplitOptions.None)[0].Split(new string[1]
                                                        {
                              "al_"
                                                        }, StringSplitOptions.None);
                                                        if (!System.IO.File.Exists("./badges/" + strArray2[1] + ".gif"))
                                                        {
                                                            webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                            webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[1] + ".gif", "./badges/" + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Downloading badge: " + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                            Console.WriteLine(strArray2[1] + ".gif already exists!");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    catch
                                                    {
                                                    }
                                                }
                                                if ((str3.StartsWith("badge_name_al") ? 1 : (str3.StartsWith("badge_name_fb") ? 1 : 0)) == 0)
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    if ((strArray2[0].Contains("_HHCA") ? 1 : (strArray2[0].Contains("_HHUK") ? 1 : 0)) == 0)
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[0] + ".gif", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray2[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }

                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge PNG: " + strArray2[0] + ".png and converting to gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        string str5 = "";
                                                        if (strArray2[0].Contains("_HHCA"))
                                                        {
                                                            str5 = "_HHCA";
                                                        }
                                                        else if (strArray2[0].Contains("_HHUK"))
                                                        {
                                                            str5 = "_HHUK";
                                                        }
                                                        string[] strArray3 = strArray2[0].Split(new string[1]
                                                        {
                              str5
                                                        }, StringSplitOptions.None);
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[0] + ".gif", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray3[0] + ".png", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray3[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                Console.WriteLine("Begin Downloading .FI");
                                Thread.Sleep(2000);
                                using (StreamWriter streamWriter = new StreamWriter("./temp/external_flash_texts_fi2.txt", true))
                                {
                                    using (StreamReader streamReader = new StreamReader("./temp/external_flash_texts_fi.txt"))
                                    {
                                        string str3;
                                        while ((str3 = streamReader.ReadLine()) != null)
                                        {
                                            if (str3.StartsWith("badge_name_"))
                                            {
                                                streamWriter.WriteLine(str3);
                                                if (str3.StartsWith("badge_name_fb_"))
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    string[] strArray3 = strArray2[0].Split(new string[1]
                                                    {
                            "fb_"
                                                    }, StringSplitOptions.None);
                                                    if (strArray2[1].Contains("%roman%"))
                                                    {
                                                        int num7 = 1;
                                                        int num8 = 0;
                                                        while (num8 == 0)
                                                        {
                                                            if (!System.IO.File.Exists(string.Concat(new object[4]
                                                            {
                                (object) "./badges/",
                                (object) strArray3[1],
                                (object) num7,
                                (object) ".gif"
                                                            })))
                                                            {
                                                                try
                                                                {
                                                                    webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                    webClient6.DownloadFile(string.Concat(new object[4]
                                                                    {
                                    (object) "http://images-eussl.habbo.com/c_images/album1584/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }), string.Concat(new object[4]
                                                                    {
                                    (object) "./badges/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Green;
                                                                    Console.WriteLine(string.Concat(new object[4]
                                                                    {
                                    (object) "Downloading badge: ",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Gray;
                                                                    ++num7;
                                                                }
                                                                catch
                                                                {
                                                                    ++num8;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + (object)num7 + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                                ++num7;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[1] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[1] + ".gif", "./badges/" + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                        }
                                                    }
                                                }
                                                if (str3.StartsWith("badge_name_al_"))
                                                {
                                                    try
                                                    {
                                                        string[] strArray2 = str3.Split(new string[1]
                                                        {
                              "badge_name_"
                                                        }, StringSplitOptions.None)[1].Split(new string[1]
                                                        {
                              "="
                                                        }, StringSplitOptions.None)[0].Split(new string[1]
                                                        {
                              "al_"
                                                        }, StringSplitOptions.None);
                                                        if (!System.IO.File.Exists("./badges/" + strArray2[1] + ".gif"))
                                                        {
                                                            webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                            webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[1] + ".gif", "./badges/" + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Downloading badge: " + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                            Console.WriteLine(strArray2[1] + ".gif already exists!");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    catch
                                                    {
                                                    }
                                                }
                                                if ((str3.StartsWith("badge_name_al") ? 1 : (str3.StartsWith("badge_name_fb") ? 1 : 0)) == 0)
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    if ((strArray2[0].Contains("_HHCA") ? 1 : (strArray2[0].Contains("_HHUK") ? 1 : 0)) == 0)
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[0] + ".gif", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray2[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }

                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge PNG: " + strArray2[0] + ".png and converting to gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        string str5 = "";
                                                        if (strArray2[0].Contains("_HHCA"))
                                                        {
                                                            str5 = "_HHCA";
                                                        }
                                                        else if (strArray2[0].Contains("_HHUK"))
                                                        {
                                                            str5 = "_HHUK";
                                                        }
                                                        string[] strArray3 = strArray2[0].Split(new string[1]
                                                        {
                              str5
                                                        }, StringSplitOptions.None);
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[0] + ".gif", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray3[0] + ".png", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray3[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                Console.WriteLine("Begin Downloading .ES");
                                Thread.Sleep(2000);
                                using (StreamWriter streamWriter = new StreamWriter("./temp/external_flash_texts_es2.txt", true))
                                {
                                    using (StreamReader streamReader = new StreamReader("./temp/external_flash_texts_es.txt"))
                                    {
                                        string str3;
                                        while ((str3 = streamReader.ReadLine()) != null)
                                        {
                                            if (str3.StartsWith("badge_name_"))
                                            {
                                                streamWriter.WriteLine(str3);
                                                if (str3.StartsWith("badge_name_fb_"))
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    string[] strArray3 = strArray2[0].Split(new string[1]
                                                    {
                            "fb_"
                                                    }, StringSplitOptions.None);
                                                    if (strArray2[1].Contains("%roman%"))
                                                    {
                                                        int num7 = 1;
                                                        int num8 = 0;
                                                        while (num8 == 0)
                                                        {
                                                            if (!System.IO.File.Exists(string.Concat(new object[4]
                                                            {
                                (object) "./badges/",
                                (object) strArray3[1],
                                (object) num7,
                                (object) ".gif"
                                                            })))
                                                            {
                                                                try
                                                                {
                                                                    webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                    webClient6.DownloadFile(string.Concat(new object[4]
                                                                    {
                                    (object) "http://images-eussl.habbo.com/c_images/album1584/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }), string.Concat(new object[4]
                                                                    {
                                    (object) "./badges/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Green;
                                                                    Console.WriteLine(string.Concat(new object[4]
                                                                    {
                                    (object) "Downloading badge: ",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Gray;
                                                                    ++num7;
                                                                }
                                                                catch
                                                                {
                                                                    ++num8;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + (object)num7 + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                                ++num7;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[1] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[1] + ".gif", "./badges/" + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                        }
                                                    }
                                                }
                                                if (str3.StartsWith("badge_name_al_"))
                                                {
                                                    try
                                                    {
                                                        string[] strArray2 = str3.Split(new string[1]
                                                        {
                              "badge_name_"
                                                        }, StringSplitOptions.None)[1].Split(new string[1]
                                                        {
                              "="
                                                        }, StringSplitOptions.None)[0].Split(new string[1]
                                                        {
                              "al_"
                                                        }, StringSplitOptions.None);
                                                        if (!System.IO.File.Exists("./badges/" + strArray2[1] + ".gif"))
                                                        {
                                                            webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                            webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[1] + ".gif", "./badges/" + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Downloading badge: " + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                            Console.WriteLine(strArray2[1] + ".gif already exists!");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    catch
                                                    {
                                                    }
                                                }
                                                if ((str3.StartsWith("badge_name_al") ? 1 : (str3.StartsWith("badge_name_fb") ? 1 : 0)) == 0)
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    if ((strArray2[0].Contains("_HHCA") ? 1 : (strArray2[0].Contains("_HHUK") ? 1 : 0)) == 0)
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[0] + ".gif", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray2[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }

                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge PNG: " + strArray2[0] + ".png and converting to gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        string str5 = "";
                                                        if (strArray2[0].Contains("_HHCA"))
                                                        {
                                                            str5 = "_HHCA";
                                                        }
                                                        else if (strArray2[0].Contains("_HHUK"))
                                                        {
                                                            str5 = "_HHUK";
                                                        }
                                                        string[] strArray3 = strArray2[0].Split(new string[1]
                                                        {
                              str5
                                                        }, StringSplitOptions.None);
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[0] + ".gif", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray3[0] + ".png", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray3[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                Console.WriteLine("Begin Downloading .NL");
                                Thread.Sleep(2000);
                                using (StreamWriter streamWriter = new StreamWriter("./temp/external_flash_texts_nl2.txt", true))
                                {
                                    using (StreamReader streamReader = new StreamReader("./temp/external_flash_texts_nl.txt"))
                                    {
                                        string str3;
                                        while ((str3 = streamReader.ReadLine()) != null)
                                        {
                                            if (str3.StartsWith("badge_name_"))
                                            {
                                                streamWriter.WriteLine(str3);
                                                if (str3.StartsWith("badge_name_fb_"))
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    string[] strArray3 = strArray2[0].Split(new string[1]
                                                    {
                            "fb_"
                                                    }, StringSplitOptions.None);
                                                    if (strArray2[1].Contains("%roman%"))
                                                    {
                                                        int num7 = 1;
                                                        int num8 = 0;
                                                        while (num8 == 0)
                                                        {
                                                            if (!System.IO.File.Exists(string.Concat(new object[4]
                                                            {
                                (object) "./badges/",
                                (object) strArray3[1],
                                (object) num7,
                                (object) ".gif"
                                                            })))
                                                            {
                                                                try
                                                                {
                                                                    webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                    webClient6.DownloadFile(string.Concat(new object[4]
                                                                    {
                                    (object) "http://images-eussl.habbo.com/c_images/album1584/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }), string.Concat(new object[4]
                                                                    {
                                    (object) "./badges/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Green;
                                                                    Console.WriteLine(string.Concat(new object[4]
                                                                    {
                                    (object) "Downloading badge: ",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Gray;
                                                                    ++num7;
                                                                }
                                                                catch
                                                                {
                                                                    ++num8;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + (object)num7 + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                                ++num7;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[1] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[1] + ".gif", "./badges/" + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                        }
                                                    }
                                                }
                                                if (str3.StartsWith("badge_name_al_"))
                                                {
                                                    try
                                                    {
                                                        string[] strArray2 = str3.Split(new string[1]
                                                        {
                              "badge_name_"
                                                        }, StringSplitOptions.None)[1].Split(new string[1]
                                                        {
                              "="
                                                        }, StringSplitOptions.None)[0].Split(new string[1]
                                                        {
                              "al_"
                                                        }, StringSplitOptions.None);
                                                        if (!System.IO.File.Exists("./badges/" + strArray2[1] + ".gif"))
                                                        {
                                                            webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                            webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[1] + ".gif", "./badges/" + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Downloading badge: " + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                            Console.WriteLine(strArray2[1] + ".gif already exists!");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    catch
                                                    {
                                                    }
                                                }
                                                if ((str3.StartsWith("badge_name_al") ? 1 : (str3.StartsWith("badge_name_fb") ? 1 : 0)) == 0)
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    if ((strArray2[0].Contains("_HHCA") ? 1 : (strArray2[0].Contains("_HHUK") ? 1 : 0)) == 0)
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[0] + ".gif", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray2[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }

                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge PNG: " + strArray2[0] + ".png and converting to gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        string str5 = "";
                                                        if (strArray2[0].Contains("_HHCA"))
                                                        {
                                                            str5 = "_HHCA";
                                                        }
                                                        else if (strArray2[0].Contains("_HHUK"))
                                                        {
                                                            str5 = "_HHUK";
                                                        }
                                                        string[] strArray3 = strArray2[0].Split(new string[1]
                                                        {
                              str5
                                                        }, StringSplitOptions.None);
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[0] + ".gif", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray3[0] + ".png", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray3[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                Console.WriteLine("Begin Downloading .DE");
                                Thread.Sleep(2000);
                                using (StreamWriter streamWriter = new StreamWriter("./temp/external_flash_texts_de2.txt", true))
                                {
                                    using (StreamReader streamReader = new StreamReader("./temp/external_flash_texts_de.txt"))
                                    {
                                        string str3;
                                        while ((str3 = streamReader.ReadLine()) != null)
                                        {
                                            if (str3.StartsWith("badge_name_"))
                                            {
                                                streamWriter.WriteLine(str3);
                                                if (str3.StartsWith("badge_name_fb_"))
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    string[] strArray3 = strArray2[0].Split(new string[1]
                                                    {
                            "fb_"
                                                    }, StringSplitOptions.None);
                                                    if (strArray2[1].Contains("%roman%"))
                                                    {
                                                        int num7 = 1;
                                                        int num8 = 0;
                                                        while (num8 == 0)
                                                        {
                                                            if (!System.IO.File.Exists(string.Concat(new object[4]
                                                            {
                                (object) "./badges/",
                                (object) strArray3[1],
                                (object) num7,
                                (object) ".gif"
                                                            })))
                                                            {
                                                                try
                                                                {
                                                                    webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                    webClient6.DownloadFile(string.Concat(new object[4]
                                                                    {
                                    (object) "http://images-eussl.habbo.com/c_images/album1584/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }), string.Concat(new object[4]
                                                                    {
                                    (object) "./badges/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Green;
                                                                    Console.WriteLine(string.Concat(new object[4]
                                                                    {
                                    (object) "Downloading badge: ",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Gray;
                                                                    ++num7;
                                                                }
                                                                catch
                                                                {
                                                                    ++num8;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + (object)num7 + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                                ++num7;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[1] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[1] + ".gif", "./badges/" + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                        }
                                                    }
                                                }
                                                if (str3.StartsWith("badge_name_al_"))
                                                {
                                                    try
                                                    {
                                                        string[] strArray2 = str3.Split(new string[1]
                                                        {
                              "badge_name_"
                                                        }, StringSplitOptions.None)[1].Split(new string[1]
                                                        {
                              "="
                                                        }, StringSplitOptions.None)[0].Split(new string[1]
                                                        {
                              "al_"
                                                        }, StringSplitOptions.None);
                                                        if (!System.IO.File.Exists("./badges/" + strArray2[1] + ".gif"))
                                                        {
                                                            webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                            webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[1] + ".gif", "./badges/" + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Downloading badge: " + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                            Console.WriteLine(strArray2[1] + ".gif already exists!");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    catch
                                                    {
                                                    }
                                                }
                                                if ((str3.StartsWith("badge_name_al") ? 1 : (str3.StartsWith("badge_name_fb") ? 1 : 0)) == 0)
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    if ((strArray2[0].Contains("_HHCA") ? 1 : (strArray2[0].Contains("_HHUK") ? 1 : 0)) == 0)
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[0] + ".gif", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray2[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }

                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge PNG: " + strArray2[0] + ".png and converting to gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        string str5 = "";
                                                        if (strArray2[0].Contains("_HHCA"))
                                                        {
                                                            str5 = "_HHCA";
                                                        }
                                                        else if (strArray2[0].Contains("_HHUK"))
                                                        {
                                                            str5 = "_HHUK";
                                                        }
                                                        string[] strArray3 = strArray2[0].Split(new string[1]
                                                        {
                              str5
                                                        }, StringSplitOptions.None);
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[0] + ".gif", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray3[0] + ".png", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray3[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                Console.WriteLine("Begin Downloading .IT");
                                Thread.Sleep(2000);
                                using (StreamWriter streamWriter = new StreamWriter("./temp/external_flash_texts_it2.txt", true))
                                {
                                    using (StreamReader streamReader = new StreamReader("./temp/external_flash_texts_it.txt"))
                                    {
                                        string str3;
                                        while ((str3 = streamReader.ReadLine()) != null)
                                        {
                                            if (str3.StartsWith("badge_name_"))
                                            {
                                                streamWriter.WriteLine(str3);
                                                if (str3.StartsWith("badge_name_fb_"))
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    string[] strArray3 = strArray2[0].Split(new string[1]
                                                    {
                            "fb_"
                                                    }, StringSplitOptions.None);
                                                    if (strArray2[1].Contains("%roman%"))
                                                    {
                                                        int num7 = 1;
                                                        int num8 = 0;
                                                        while (num8 == 0)
                                                        {
                                                            if (!System.IO.File.Exists(string.Concat(new object[4]
                                                            {
                                (object) "./badges/",
                                (object) strArray3[1],
                                (object) num7,
                                (object) ".gif"
                                                            })))
                                                            {
                                                                try
                                                                {
                                                                    webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                    webClient6.DownloadFile(string.Concat(new object[4]
                                                                    {
                                    (object) "http://images-eussl.habbo.com/c_images/album1584/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }), string.Concat(new object[4]
                                                                    {
                                    (object) "./badges/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Green;
                                                                    Console.WriteLine(string.Concat(new object[4]
                                                                    {
                                    (object) "Downloading badge: ",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Gray;
                                                                    ++num7;
                                                                }
                                                                catch
                                                                {
                                                                    ++num8;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + (object)num7 + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                                ++num7;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[1] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[1] + ".gif", "./badges/" + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                        }
                                                    }
                                                }
                                                if (str3.StartsWith("badge_name_al_"))
                                                {
                                                    try
                                                    {
                                                        string[] strArray2 = str3.Split(new string[1]
                                                        {
                              "badge_name_"
                                                        }, StringSplitOptions.None)[1].Split(new string[1]
                                                        {
                              "="
                                                        }, StringSplitOptions.None)[0].Split(new string[1]
                                                        {
                              "al_"
                                                        }, StringSplitOptions.None);
                                                        if (!System.IO.File.Exists("./badges/" + strArray2[1] + ".gif"))
                                                        {
                                                            webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                            webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[1] + ".gif", "./badges/" + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Downloading badge: " + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                            Console.WriteLine(strArray2[1] + ".gif already exists!");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    catch
                                                    {
                                                    }
                                                }
                                                if ((str3.StartsWith("badge_name_al") ? 1 : (str3.StartsWith("badge_name_fb") ? 1 : 0)) == 0)
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    if ((strArray2[0].Contains("_HHCA") ? 1 : (strArray2[0].Contains("_HHUK") ? 1 : 0)) == 0)
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[0] + ".gif", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray2[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }

                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge PNG: " + strArray2[0] + ".png and converting to gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        string str5 = "";
                                                        if (strArray2[0].Contains("_HHCA"))
                                                        {
                                                            str5 = "_HHCA";
                                                        }
                                                        else if (strArray2[0].Contains("_HHUK"))
                                                        {
                                                            str5 = "_HHUK";
                                                        }
                                                        string[] strArray3 = strArray2[0].Split(new string[1]
                                                        {
                              str5
                                                        }, StringSplitOptions.None);
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[0] + ".gif", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray3[0] + ".png", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray3[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                Console.WriteLine("Begin Downloading .TR");
                                Thread.Sleep(2000);
                                using (StreamWriter streamWriter = new StreamWriter("./temp/external_flash_texts_tr2.txt", true))
                                {
                                    using (StreamReader streamReader = new StreamReader("./temp/external_flash_texts_tr.txt"))
                                    {
                                        string str3;
                                        while ((str3 = streamReader.ReadLine()) != null)
                                        {
                                            if (str3.StartsWith("badge_name_"))
                                            {
                                                streamWriter.WriteLine(str3);
                                                if (str3.StartsWith("badge_name_fb_"))
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    string[] strArray3 = strArray2[0].Split(new string[1]
                                                    {
                            "fb_"
                                                    }, StringSplitOptions.None);
                                                    if (strArray2[1].Contains("%roman%"))
                                                    {
                                                        int num7 = 1;
                                                        int num8 = 0;
                                                        while (num8 == 0)
                                                        {
                                                            if (!System.IO.File.Exists(string.Concat(new object[4]
                                                            {
                                (object) "./badges/",
                                (object) strArray3[1],
                                (object) num7,
                                (object) ".gif"
                                                            })))
                                                            {
                                                                try
                                                                {
                                                                    webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                    webClient6.DownloadFile(string.Concat(new object[4]
                                                                    {
                                    (object) "http://images-eussl.habbo.com/c_images/album1584/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }), string.Concat(new object[4]
                                                                    {
                                    (object) "./badges/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Green;
                                                                    Console.WriteLine(string.Concat(new object[4]
                                                                    {
                                    (object) "Downloading badge: ",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Gray;
                                                                    ++num7;
                                                                }
                                                                catch
                                                                {
                                                                    ++num8;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + (object)num7 + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                                ++num7;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[1] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[1] + ".gif", "./badges/" + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                        }
                                                    }
                                                }
                                                if (str3.StartsWith("badge_name_al_"))
                                                {
                                                    try
                                                    {
                                                        string[] strArray2 = str3.Split(new string[1]
                                                        {
                              "badge_name_"
                                                        }, StringSplitOptions.None)[1].Split(new string[1]
                                                        {
                              "="
                                                        }, StringSplitOptions.None)[0].Split(new string[1]
                                                        {
                              "al_"
                                                        }, StringSplitOptions.None);
                                                        if (!System.IO.File.Exists("./badges/" + strArray2[1] + ".gif"))
                                                        {
                                                            webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                            webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[1] + ".gif", "./badges/" + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Downloading badge: " + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                            Console.WriteLine(strArray2[1] + ".gif already exists!");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    catch
                                                    {
                                                    }
                                                }
                                                if ((str3.StartsWith("badge_name_al") ? 1 : (str3.StartsWith("badge_name_fb") ? 1 : 0)) == 0)
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    if ((strArray2[0].Contains("_HHCA") ? 1 : (strArray2[0].Contains("_HHUK") ? 1 : 0)) == 0)
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[0] + ".gif", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray2[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }

                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge PNG: " + strArray2[0] + ".png and converting to gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        string str5 = "";
                                                        if (strArray2[0].Contains("_HHCA"))
                                                        {
                                                            str5 = "_HHCA";
                                                        }
                                                        else if (strArray2[0].Contains("_HHUK"))
                                                        {
                                                            str5 = "_HHUK";
                                                        }
                                                        string[] strArray3 = strArray2[0].Split(new string[1]
                                                        {
                              str5
                                                        }, StringSplitOptions.None);
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[0] + ".gif", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray3[0] + ".png", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray3[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Begin Downloading .COM.BR");
                                Thread.Sleep(2000);
                                using (StreamWriter streamWriter = new StreamWriter("./temp/external_flash_texts_br2.txt", true))
                                {
                                    using (StreamReader streamReader = new StreamReader("./temp/external_flash_texts_br.txt"))
                                    {
                                        string str3;
                                        while ((str3 = streamReader.ReadLine()) != null)
                                        {
                                            if (str3.StartsWith("badge_name_"))
                                            {
                                                streamWriter.WriteLine(str3);
                                                if (str3.StartsWith("badge_name_fb_"))
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                            "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                            "="
                                                    }, StringSplitOptions.None);
                                                    string[] strArray3 = strArray2[0].Split(new string[1]
                                                    {
                            "fb_"
                                                    }, StringSplitOptions.None);
                                                    if (strArray2[1].Contains("%roman%"))
                                                    {
                                                        int num7 = 1;
                                                        int num8 = 0;
                                                        while (num8 == 0)
                                                        {
                                                            if (!System.IO.File.Exists(string.Concat(new object[4]
                                                            {
                                (object) "./badges/",
                                (object) strArray3[1],
                                (object) num7,
                                (object) ".gif"
                                                            })))
                                                            {
                                                                try
                                                                {
                                                                    webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                    webClient6.DownloadFile(string.Concat(new object[4]
                                                                    {
                                    (object) "http://images-eussl.habbo.com/c_images/album1584/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }), string.Concat(new object[4]
                                                                    {
                                    (object) "./badges/",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Green;
                                                                    Console.WriteLine(string.Concat(new object[4]
                                                                    {
                                    (object) "Downloading badge: ",
                                    (object) strArray3[1],
                                    (object) num7,
                                    (object) ".gif"
                                                                    }));
                                                                    Console.ForegroundColor = ConsoleColor.Gray;
                                                                    ++num7;
                                                                }
                                                                catch
                                                                {
                                                                    ++num8;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + (object)num7 + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                                ++num7;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[1] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[1] + ".gif", "./badges/" + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[1] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[1] + ".gif already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                        }
                                                    }
                                                }
                                                if (str3.StartsWith("badge_name_al_"))
                                                {
                                                    try
                                                    {
                                                        string[] strArray2 = str3.Split(new string[1]
                                                        {
                              "badge_name_"
                                                        }, StringSplitOptions.None)[1].Split(new string[1]
                                                        {
                              "="
                                                        }, StringSplitOptions.None)[0].Split(new string[1]
                                                        {
                              "al_"
                                                        }, StringSplitOptions.None);
                                                        if (!System.IO.File.Exists("./badges/" + strArray2[1] + ".gif"))
                                                        {
                                                            webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                            webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[1] + ".gif", "./badges/" + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Green;
                                                            Console.WriteLine("Downloading badge: " + strArray2[1] + ".gif");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                        else
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                            Console.WriteLine(strArray2[1] + ".gif already exists!");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    catch
                                                    {
                                                    }
                                                }
                                                if ((str3.StartsWith("badge_name_al") ? 1 : (str3.StartsWith("badge_name_fb") ? 1 : 0)) == 0)
                                                {
                                                    string[] strArray2 = str3.Split(new string[1]
                                                    {
                                                        "badge_name_"
                                                    }, StringSplitOptions.None)[1].Split(new string[1]
                                                    {
                                                        "="
                                                    }, StringSplitOptions.None);
                                                    if ((strArray2[0].Contains("_HHCA") ? 1 : (strArray2[0].Contains("_HHUK") ? 1 : 0)) == 0)
                                                    {
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray2[0] + ".gif", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray2[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray2[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }

                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray2[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray2[0] + ".png", "./badges/" + strArray2[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge PNG: " + strArray2[0] + ".png and converting to gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray2[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        string str5 = "";
                                                        if (strArray2[0].Contains("_HHCA"))
                                                        {
                                                            str5 = "_HHCA";
                                                        }
                                                        else if (strArray2[0].Contains("_HHUK"))
                                                        {
                                                            str5 = "_HHUK";
                                                        }
                                                        string[] strArray3 = strArray2[0].Split(new string[1]
                                                        {
                                                            str5
                                                        }, StringSplitOptions.None);
                                                        try
                                                        {
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("http://images-eussl.habbo.com/c_images/album1584/" + strArray3[0] + ".gif", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0] + ".gif");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            else
                                                            {
                                                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                                                                Console.WriteLine(strArray3[0] + " already exists!");
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                            if (!System.IO.File.Exists("./badges/" + strArray3[0] + ".gif"))
                                                            {
                                                                webClient6.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                                                                webClient6.DownloadFile("https://images.habbogroup.com/c_images/album1584/" + strArray3[0] + ".png", "./badges/" + strArray3[0] + ".gif");

                                                                Console.ForegroundColor = ConsoleColor.Green;
                                                                Console.WriteLine("Downloading badge: " + strArray3[0]);
                                                                Console.ForegroundColor = ConsoleColor.Gray;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine("Error while downloading badge " + strArray3[0] + " Lets continue...");
                                                            Console.ForegroundColor = ConsoleColor.Gray;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine();
                                Console.WriteLine("Downloading done!");
                                Console.WriteLine("We downloaded " + (object)(Directory.GetFiles("./badges", "*.*", SearchOption.AllDirectories).Length - length) + " badges!");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                goto temp;

                            default:
                                ConsoleCommandHandeling.unknownCommand(inputData);
                                goto readline;
                        }
                        if (!Directory.Exists("./hof_furni"))
                        {
                            Directory.CreateDirectory("./hof_furni");
                        }
                        if (!Directory.Exists("./hof_furni/icons"))
                        {
                            Directory.CreateDirectory("./hof_furni/icons");
                        }
                        if (!Directory.Exists("./temp"))
                        {
                            Directory.CreateDirectory("./temp");
                        }
                        string str7 = "./temp/furnidata.txt";
                        WebClient webClient7 = new WebClient();
                        webClient7.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                        webClient7.DownloadFile("https://habbo.com/gamedata/furnidata/1/", str7);
                        Console.WriteLine("Furnidata Downloaded...");
                        int num9 = 0;
                        System.IO.File.WriteAllLines(str7, Enumerable.ToArray<string>(Enumerable.Select<string, string>((IEnumerable<string>)System.IO.File.ReadAllLines(str7), (Func<string, string>)(line => string.Join("\n", line.Split(new string[1]
                      {
                          "],"
                      }, StringSplitOptions.RemoveEmptyEntries))))));
                        System.IO.File.WriteAllLines(str7, Enumerable.ToArray<string>(Enumerable.Select<string, string>((IEnumerable<string>)System.IO.File.ReadAllLines(str7), (Func<string, string>)(line => string.Join("\n", line.Split(new string[1]
                      {
                          "["
                      }, StringSplitOptions.RemoveEmptyEntries))))));
                        System.IO.File.WriteAllLines(str7, Enumerable.ToArray<string>(Enumerable.Select<string, string>((IEnumerable<string>)System.IO.File.ReadAllLines(str7), (Func<string, string>)(line => string.Join("", line.Split(new string[1]
                      {
                          "\""
                      }, StringSplitOptions.RemoveEmptyEntries))))));
                        using (StreamReader streamReader = new StreamReader(str7))
                        {
                            Console.WriteLine("Begin downloading...");
                            Thread.Sleep(3000);
                            string str3;
                            while ((str3 = streamReader.ReadLine()) != null)
                            {
                                string[] direcory_path = str3.Split(new string[1]
                                {
                                    ","
                                }, StringSplitOptions.None);
                                string[] furniture_name = direcory_path[2].Split(new string[1]
                                {
                                    "*"
                                }, StringSplitOptions.None);
                                if (!System.IO.File.Exists("./hof_furni/" + furniture_name[0] + ".swf"))
                                {
                                    try
                                    {
                                        string furnitureurl = System.Configuration.ConfigurationManager.AppSettings["furnitureurl"];
                                        Console.WriteLine(furnitureurl);
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine("Downloading: /" + direcory_path[3] + "/" + furniture_name[0] + ".swf");
                                        Console.WriteLine("Downloading: /" + direcory_path[3] + "/" + furniture_name[0] + ".png");
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                        webClient7.DownloadFile(furnitureurl + "/" + direcory_path[3] + "/" + furniture_name[0] + ".swf", "hof_furni/" + furniture_name[0] + ".swf");
                                        webClient7.DownloadFile(furnitureurl + "/" + direcory_path[3] + "/" + furniture_name[0] + "_icon.png", "hof_furni/icons/" + furniture_name[0] + "_icon.png");
                                    }
                                    catch
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("Error while downloading: " + furniture_name[0]);
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                        --num9;
                                    }
                                    ++num9;
                                }
                                // else
                                // {
                                // Console.ForegroundColor = ConsoleColor.DarkCyan;
                                // Console.WriteLine(strArray2[2] + ".swf already exists!");
                                // Console.ForegroundColor = ConsoleColor.Gray;
                                // }
                            }
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Downloading Furniture Done!");
                            Console.WriteLine("We've downloaded " + (object)num9 + " new furniture!");
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }

                    temp:
                        foreach (string path in Directory.GetFiles("./temp"))
                        {
                            System.IO.File.Delete(path);
                        }
                        Directory.Delete("./temp");
                        break;

                    case "help":
                        Console.Clear();
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("                       Tool Commands:                             ");
                        Console.WriteLine("                                                                  ");
                        Console.BackgroundColor = ConsoleColor.Gray;
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("-> Help     - This Command List                                   ");
                        Console.WriteLine("-> Version  - Show current SWF version on Habbo.com               ");
                        Console.WriteLine("-> About    - Show info about this tool                           ");
                        Console.WriteLine("-> clothes  - Download all clothes and XML                        ");
                        Console.WriteLine("-> customclothes  - Download all Custom clothes and XML           ");
                        Console.WriteLine("-> custfurni - Download all Custom furni                         ");
                        Console.WriteLine("-> Exit     - Exit the application                                ");
                        Console.WriteLine("                                                                  ");
                        Console.WriteLine("-> Download - all download commands:                              ");
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine("- effects - Download the XML off all effects.                     ");
                        Console.WriteLine("- furniture - Downloads All Habbo Furniture.                      ");
                        Console.WriteLine("- furnidata - Saves a local copy of the furnidata.                ");
                        Console.WriteLine("- productdata - Saves a local copy of the productdata             ");
                        Console.WriteLine("- texts - Saves all external_flash_texts.                         ");
                        Console.WriteLine("- variables - Saves all external_variables.                       ");
                        Console.WriteLine("- icons - Saves all catalogue icons.                              ");
                        Console.WriteLine("- mp3 - Saves all mp3 sounds.                                     ");
                        Console.WriteLine("- quests - Saves all quest images.                                ");
                        Console.WriteLine("- badges - Saves all badges.                                      ");
                        Console.WriteLine("- reception - Saves client background images                      ");
                        Console.WriteLine("                                                                  ");
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;

                    case "exit":
                        System.Environment.Exit(1);
                        break;

                    case "about":
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("All developers from RagZone but special credits to : Quackster and ofcourse all the rest. So stop wanking about credits!");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;

                    case "version":
                        string release_ver;
                        HttpClient httpClient_version_ver = new HttpClient();
                        httpClient_version_ver.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.86 Safari/537.36");

                        Task.Run(async () =>
                        {
                            try
                            {
                                HttpResponseMessage res = await httpClient_version_ver.GetAsync("https://www.habbo.com/gamedata/external_variables/1");
                                string source = (await res.Content.ReadAsStringAsync());

                                foreach (string Line in source.Split(Environment.NewLine.ToCharArray()))
                                {
                                    if (!Line.Contains("flash.client.url="))
                                    {
                                        continue;
                                    }
                                    release_ver = Line.Substring(0, Line.Length - 1).Split('/')[4];
                                    Console.WriteLine("Current habbo release: " + release_ver);

                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        });
                        break;

                    case "customclothes":

                        HttpClient httpClient_version1 = new HttpClient();
                        httpClient_version1.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.86 Safari/537.36");

                        Program program = new Program();
                        Task.Run(async () =>
                        {
                            try
                            {
                                string customvariables = System.Configuration.ConfigurationManager.AppSettings["customvariables"];
                                HttpResponseMessage res = await httpClient_version1.GetAsync(customvariables);
                                string source = (await res.Content.ReadAsStringAsync());
                                Console.WriteLine("step1 = OK");

                                foreach (string Line in source.Split(Environment.NewLine.ToCharArray()))
                                {
                                    string CurrentDirectory;
                                    string GordonDirectory;
                                    string figuremapfile;
                                    string DownloadDirectory;

                                    string customclothesdir = System.Configuration.ConfigurationManager.AppSettings["customclothesdir"];

                                    CurrentDirectory = Environment.CurrentDirectory;
                                    GordonDirectory = customclothesdir;
                                    DownloadDirectory = CurrentDirectory + @"\Custom_clothes\";

                                    WebClient customclothes = new WebClient();
                                    customclothes.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");

                                    try
                                    {
                                        Console.WriteLine("step2 = start\n");
                                        string customfiguredata1 = System.Configuration.ConfigurationManager.AppSettings["customfiguredata"];
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        customclothes.DownloadFile(customfiguredata1, DownloadDirectory + @"\figuredata.xml");
                                        Console.WriteLine("Downloaded figuredata.xml\n");
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                        Console.WriteLine("step2 = OK\n");
                                    }
                                    catch
                                    {
                                        string customfiguredata2 = System.Configuration.ConfigurationManager.AppSettings["customfiguredata"];
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("Error! Cant download file");
                                        Console.WriteLine();
                                        Console.WriteLine("figuredata.xml \ndownload url: " + customfiguredata2);
                                        Console.WriteLine();
                                        Console.WriteLine("Trying download figuremap.xml");
                                        Console.WriteLine();
                                    }

                                    try
                                    {
                                        Console.WriteLine("step2 = start\n");
                                        string customfiguremap1 = System.Configuration.ConfigurationManager.AppSettings["customfiguremap"];
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        customclothes.DownloadFile(customfiguremap1, DownloadDirectory + @"\figuremap.xml");
                                        Console.WriteLine("Downloaded figuremap.xml\n");
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                        Console.WriteLine("step2 = OK\n");
                                    }
                                    catch
                                    {
                                        string customfiguremap2 = System.Configuration.ConfigurationManager.AppSettings["customfiguremap"];
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("Error! Cant download file! Please check your Habbo_Downloader.exe.config is OK!");
                                        Console.WriteLine();
                                        Console.WriteLine("figuremap.xml download url: " + customfiguremap2);
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
                                                                    string customfileextension = System.Configuration.ConfigurationManager.AppSettings["customfileextension"];
                                                                    customclothes.DownloadFile(GordonDirectory + "/" + id + customfileextension, DownloadDirectory + id + customfileextension);
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
                                        break;
                                    }

                                    Console.WriteLine();
                                    Console.WriteLine("Al has been done!");
                                    Console.WriteLine();
                                    break;
                                }

                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        });
                        break;

                    case "clothes":

                        string release;
                        HttpClient httpClient_version2 = new HttpClient();
                        httpClient_version2.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.86 Safari/537.36");

                        Program program1 = new Program();
                        Task.Run(async () =>
                        {
                            try
                            {
                                HttpResponseMessage res = await httpClient_version2.GetAsync("https://www.habbo.com/gamedata/external_variables/1");
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
                                    WebClient.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");


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
                                    Console.WriteLine("Al has been done!");
                                    Console.WriteLine();
                                }

                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in command [" + inputData + "]: " + ((object)ex).ToString());
            }

        readline:
            Console.WriteLine();
            ConsoleCommandHandeling.InvokeCommand(Console.ReadLine());
        }

        private static void unknownCommand(string command)
        {
            Console.WriteLine(command + " is an unknown or unsupported command. Type help for more information");
        }
    }
}