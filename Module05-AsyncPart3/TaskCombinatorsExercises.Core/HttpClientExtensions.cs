using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace TaskCombinatorsExercises.Core
{
    public static class HttpClientExtensions
    {
        /*
         Write cancellable async method with timeout handling, that concurrently tries to get data from
         provided urls (first wins and its response is returned, rest is __cancelled__).
         
         Tips:
         * consider using HttpClient.GetAsync (as it is cancellable)
         * consider using Task.WhenAny
         * you may use urls like for testing https://postman-echo.com/delay/3
         * you should have problem with tasks cancellation -
            - how to merge tokens of operations (timeouts) with the provided token? 
            - Tip: you can link tokens with the help of CancellationTokenSource.CreateLinkedTokenSource(token)
         */
        public static async Task<string> ConcurrentDownloadAsync(this HttpClient httpClient,
            string[] urls,
            int millisecondsTimeout,
            CancellationToken token)
        {
            using var cts = new CancellationTokenSource(millisecondsTimeout);
            using var lcts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, token);

            var tasks = new List<Task<HttpResponseMessage>>();
            foreach (var url in urls)
            {
                var task = httpClient.GetAsync(url, lcts.Token);
                tasks.Add(task);
            }

            var response = await await Task.WhenAny(tasks);
            lcts.Cancel();

            return await response.Content.ReadAsStringAsync();
        }

        //public static async Task<string> ConcurrentDownloadAsync(this HttpClient httpClient,
        //    string[] urls,
        //    int millisecondsTimeout,
        //    CancellationToken token)
        //{
        //    using var cts = new CancellationTokenSource(millisecondsTimeout);
        //    using var lcts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, token);

        //    await foreach (var result in Enumerate(httpClient, urls, lcts.Token))
        //    {
        //        lcts.Cancel();
        //        return result;
        //    }

        //    return default;
        //}

        private static async IAsyncEnumerable<string> Enumerate(
            HttpClient httpClient, string[] urls, [EnumeratorCancellation] CancellationToken token)
        {
            foreach (var url in urls)
            {
                var response = await httpClient.GetAsync(url, token);
                yield return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
