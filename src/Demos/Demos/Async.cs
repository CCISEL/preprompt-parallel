using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Demos
{
    public class Async
    {
        [Ignore]
        public void Run()
        {
            var urls = new[] { "http://www.google.com", "http://prompt.cc.isel.ipl.pt" };
            foreach (var headerCount in MakeRequest(urls))
            {
                Console.WriteLine(headerCount);
            }
        }

        public IEnumerable<int> MakeRequest(params string[] urls)
        {
            var list = new List<int>();
            foreach (var url in urls)
            {
                var request = WebRequest.Create(url);
                var response = request.GetResponse();
                list.Add(response.Headers.Count);
            }
            return list;
        }

        public IEnumerable<int> MakeRequestBeginEnd(params string[] urls)
        {
            var list = new List<int>();
            var @event = new CountdownEvent(urls.Length);

            MakeRequestTpl(resp =>
            {
                list.Add(resp.Headers.Count);
                @event.Signal();
            }, e => Console.WriteLine(e.Message), urls);

            @event.Wait();
            return list;
        }

        public void MakeRequestApm(Action<WebResponse> result, Action<Exception> error, params string[] urls)
        {
            int index = 0;
            AsyncCallback cont = null;
            cont = iar =>
            {
                WebRequest webReq;
                if (iar != null)
                {
                    try
                    {
                        webReq = (WebRequest)iar.AsyncState;
                        result(webReq.EndGetResponse(iar));
                    }
                    catch (Exception e)
                    {
                        error(e);
                    }

                    if (index == urls.Length)
                    {
                        return;
                    }
                }

                webReq = WebRequest.Create(urls[index++]);
                webReq.BeginGetResponse(cont, webReq);
            };
            cont(null);
            return;
        }

        public void MakeRequestTpl(Action<WebResponse> result, Action<Exception> error, params string[] urls)
        {
            int index = 0;
            Action<Task<WebResponse>> continuation = null;
            continuation = prev =>
            {
                if (prev != null)
                {
                    try
                    {
                        result(prev.Result);
                    }
                    catch (Exception e)
                    {
                        error(e);
                    }

                    if (index == urls.Length)
                    {
                        return;
                    }
                }

                var request = WebRequest.Create(urls[index++]);
                Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null)
                    .ContinueWith(continuation);
            };
            continuation(null);
        }

        public IEnumerator<Task> MakeRequestAsyncEnumerator(params string[] urls)
        {
            var list = new List<int>();
            foreach (var url in urls)
            {
                var request = WebRequest.Create(url);
                var task = Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null);
                yield return task;
                list.Add(task.Result.Headers.Count);
            }

            var tcs = new TaskCompletionSource<IEnumerable<int>>();
            tcs.SetResult(list);
            yield return tcs.Task;
        }

        public async Task<IEnumerable<int>> MakeRequestAsync(params string[] urls)
        {
            var list = new List<int>();
            foreach (var url in urls)
            {
                var request = WebRequest.Create(url);
                var response = await request.GetResponseAsync();
                list.Add(response.Headers.Count);
            }
            return list;
        }
    }
}