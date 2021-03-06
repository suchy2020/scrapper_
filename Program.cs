﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Console = Colorful.Console;
using HtmlAgilityPack;
using ScrapySharp.Extensions;
using ScrapySharp.Network;

namespace Scrapper
{
    class Program
    {
        private int urlIndex = 0;
        static ScrapingBrowser _browser = new ScrapingBrowser();
        private static int scrapDelay = 100;
        public static string availableitemsstatus = "Dostępny";
        
        static void Main(string[] args)
        {
            EmailSender email = new EmailSender();
            email.LoadConfig();
            
            if (!File.Exists("linki.txt"))
                File.Create("linki.txt");
            
            string[] linkstxt = File.ReadAllLines("linki.txt");
            List<string> urls = new List<string>();
            List<string> availableitems = new List<string>();
            
            foreach (var s in linkstxt)
            {
                urls.Add(s);
            }
            
            while (true)
            {
                List<InfoAboutComponent> ReadyListToParse = new List<InfoAboutComponent>();
                ReadyListToParse.Clear();
                ReadyListToParse = GetPageDetails(urls);
                
                Console.WriteLine($"Załadowałem: {ReadyListToParse.Count} linków/link/linki",Color.Gray);
                Console.WriteLine();
                
                availableitems.Clear();
                
                foreach (InfoAboutComponent ToDisplayFromList in ReadyListToParse)
                {
                    InfoAboutComponent temp = ToDisplayFromList;

                    Console.Write(temp.shop + "\t\t", Color.White);
                    Console.Write(temp.status + TabCounter(temp.status), GetColor(temp.status.ToLower()));
                    Console.Write(temp.title + "\t\n", Color.White);

                    if (temp.status.ToLower() == availableitemsstatus.ToLower())
                        availableitems.Add($"{temp.shop} - {temp.title} | {temp.url}");
                }
                
                if(availableitems.Count > 0)
                    email.SendEmail($"{availableitems.Count}, tyle właśnie produktów jest dostępnych!", availableitems);
                
                Console.Title = "Następne sprawdzanie za " + scrapDelay + " sekund";
                Thread.Sleep(scrapDelay*1000);
            }
        }
        
        static HtmlNode GetHtml(string url)
        {
            WebPage webpage = _browser.NavigateToPage(new Uri(url));
            return webpage.Html;
        }
        static List<InfoAboutComponent> GetPageDetails(List<string> urls)
        {
            float tempindex = 0;
            var lstPageDetails = new List<InfoAboutComponent>();
            
            foreach (var url in urls)
            {
                tempindex++;
                Console.Title = $"{Math.Round((tempindex / urls.Count) * 100)}% Sprawdzam teraz -> " + url;
                
                if (url.Contains("x-kom"))
                {
                    var htmlNode = GetHtml(url);
                    var compnToWrite = new InfoAboutComponent();

                    compnToWrite.title = htmlNode.OwnerDocument.DocumentNode
                        .SelectSingleNode("//html/body/div/div/div/div/div/div/div/div/div/div/h1").InnerHtml;

                    compnToWrite.status = htmlNode.OwnerDocument.DocumentNode
                        .SelectSingleNode(
                            "//html/body/div/div/div/div/div/div/div/div/div/div/div/div/button/span/span/span")
                        .InnerText;

                    compnToWrite.url = url;
                    compnToWrite.shop = "x-kom";
                    lstPageDetails.Add(compnToWrite);
                }

                if (url.Contains("morele"))
                {
                    var htmlNode = GetHtml(url);
                    var compnToWrite = new InfoAboutComponent();

                    compnToWrite.title = htmlNode.OwnerDocument.DocumentNode
                        .SelectSingleNode("//html/body/main/div/section/div/div/div/div/div/div/div/div/h1").InnerHtml;

                    string status = htmlNode.OwnerDocument.DocumentNode
                        .SelectSingleNode(
                            "//html/body/main/div/section/div/div/aside/div/div/div")
                        .InnerHtml;

                    //trochę bruh ale działa
                    if (status.Length < 50)
                    {
                        compnToWrite.status = "Dostępny";
                        compnToWrite.available = true;
                    }
                    else
                    {
                        compnToWrite.status = "Niedostępny";
                        compnToWrite.available = false;
                    }

                    compnToWrite.url = url;
                    compnToWrite.shop = "morele";
                    lstPageDetails.Add(compnToWrite);
                }
            }

            return lstPageDetails;
        }
        
        static Color GetColor(string status)
        {
            switch (status)
            {
                case "powiadom o dostępności":
                    return Color.FromArgb(100,245,0,0);
                case "wycofany":
                    return Color.FromArgb(100, 255, 128, 0);
                case "czasowo niedostępny":
                    return Color.FromArgb(100,245,0,0);
                case "sprawdź inne produkty":
                    return Color.FromArgb(100,245,0,0);
                case "niedostępny":
                    return Color.FromArgb(100,245,0,0);
                case "dostępny":
                    return Color.FromArgb(100,0,245,0);
                default:
                    return Color.White;
            }
        }
        static string TabCounter(string tocount) => tocount.Length < 12 ? "\t\t" : "\t";
    }
    
    public class InfoAboutComponent {
        public string title { get; set; }
        public string status { get; set; }
        public string url { get; set; }
        public string shop { get; set; }
        public bool available { get; set; }
    }
}