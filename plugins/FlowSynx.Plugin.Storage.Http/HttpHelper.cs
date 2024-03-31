//using FlowSynx.Net;
//using System.Net;

//namespace FlowSynx.Plugin.Storage.Http;

//internal class HttpHelper
//{
//    public static string ReadHtmlContentFromUrl(string url)
//    {
//        var message = new HttpRequestMessage(HttpMethod.Get, new Uri(url));

//        //var request = (HttpWebRequest)WebRequest.Create(url);

//        using var response = (HttpWebResponse)request.GetResponse();
//        using var stream = response.GetResponseStream();
//        using var reader = new StreamReader(stream);
//        var html = reader.ReadToEnd();
//        return html;
//    }
//}