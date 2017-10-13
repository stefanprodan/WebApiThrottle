using System;
using System.Net.Http;
using Microsoft.Owin.Hosting;

namespace WebApiThrottler.SelfHostOwinDemo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var baseAddress = "http://localhost:9000/";

            // Start OWIN host 
            using (WebApp.Start<Startup>(baseAddress))
            {
                // Create HttpCient and make a request to api/values 
                var client = new HttpClient();

                var response = client.GetAsync(baseAddress + "api/values").Result;

                Console.WriteLine(response);
                Console.WriteLine(response.Content.ReadAsStringAsync().Result);


                Console.ReadLine();
            }
        }
    }
}