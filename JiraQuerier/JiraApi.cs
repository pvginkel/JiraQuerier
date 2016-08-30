using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using Jint.Native;
using Jint.Native.Object;
using JintDebugger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JiraQuerier
{
    public class JiraApi
    {
        private readonly string _site;
        private readonly string _userName;
        private readonly string _password;
        private readonly IStatusBarProvider _statusBarProvider;

        public JiraApi(string site, string userName, string password, IStatusBarProvider statusBarProvider)
        {
            if (site == null)
                throw new ArgumentNullException("site");
            if (userName == null)
                throw new ArgumentNullException("userName");
            if (password == null)
                throw new ArgumentNullException("password");
            if (statusBarProvider == null)
                throw new ArgumentNullException("statusBarProvider");

            if (site[site.Length - 1] == '/')
                site = site.Substring(0, site.Length - 1);

            _site = site;
            _userName = userName;
            _password = password;
            _statusBarProvider = statusBarProvider;
        }

        public string SearchJql(string jql, int startAt, int maxResults, bool validateQuery, string fields, string expand)
        {
            if (jql == null)
                throw new ArgumentNullException("jql");

            var parameters = new Dictionary<string, string>
            {
                { "jql", jql }
            };

            if (startAt != -1)
                parameters.Add("startAt", startAt.ToString(CultureInfo.InvariantCulture));
            if (maxResults != -1)
                parameters.Add("maxResults", maxResults.ToString(CultureInfo.InvariantCulture));
            if (validateQuery)
                parameters.Add("validateQuery", "true");
            if (fields != null)
                parameters.Add("fields", fields);
            if (expand != null)
                parameters.Add("expand", expand);

            return Request("rest/api/2/search", parameters, null);
        }

        //public JiraHistory[] GetIssueHistory(int issueId)
        //{
        //    var parameters = new Dictionary<string, string>
        //    {
        //        { "expand", "changelog" }
        //    };

        //    dynamic response = Request(
        //        String.Format("/rest/api/2/issue/{0}", issueId),
        //        parameters
        //    );

        //    var result = new List<JiraHistory>();

        //    foreach (var history in response.changelog.histories)
        //    {
        //        result.Add(new JiraHistory(history));
        //    }

        //    return result.ToArray();
        //}

        //public JiraStatus[] GetStatuses()
        //{
        //    dynamic response = Request("/rest/api/2/status");

        //    var result = new List<JiraStatus>();

        //    foreach (var status in response)
        //    {
        //        result.Add(new JiraStatus(status));
        //    }

        //    return result.ToArray();
        //}

        //public JiraTransition[] GetTransitions(int issueId, int? transitionId = null)
        //{
        //    var parameters = new Dictionary<string, string>();

        //    if (transitionId.HasValue)
        //        parameters.Add("transitionId", transitionId.Value.ToString());

        //    dynamic response = Request(
        //        String.Format("/rest/api/2/issue/{0}/transitions", issueId),
        //        parameters
        //    );

        //    var result = new List<JiraTransition>();

        //    foreach (var transition in response.transitions)
        //    {
        //        result.Add(new JiraTransition(transition));
        //    }

        //    return result.ToArray();
        //}

        //public void Transition(int issueId, int transitionId)
        //{
        //    Request(
        //        String.Format(
        //            "/rest/api/2/issue/{0}/transitions", issueId
        //        ),
        //        null,
        //        new JObject(
        //            new JProperty(
        //                "transition",
        //                new JObject(
        //                    new JProperty(
        //                        "id", transitionId
        //                    )
        //                )
        //            )
        //        )
        //    );
        //}

        //public void Comment(int issueId, string comment)
        //{
        //    Request(
        //        String.Format("/rest/api/2/issue/{0}/comment", issueId),
        //        null,
        //        new JObject(
        //            new JProperty("body", comment)
        //        )
        //    );
        //}

        //public void LogWork(int issueId, string timeSpent, DateTime dateStarted, AdjustRemainingMode adjustRemaining, string adjustRemainingBy, string workDescription)
        //{
        //    var parameters = new Dictionary<string, string>();

        //    switch (adjustRemaining)
        //    {
        //        case AdjustRemainingMode.AdjustAutomatically:
        //            parameters.Add("adjustEstimate", "auto");
        //            break;

        //        case AdjustRemainingMode.DonNotChange:
        //            parameters.Add("adjustEstimate", "leave");
        //            break;

        //        case AdjustRemainingMode.ReduceBy:
        //            parameters.Add("adjustEstimate", "manual");
        //            parameters.Add("reduceBy", adjustRemainingBy);
        //            break;

        //        case AdjustRemainingMode.SetTo:
        //            parameters.Add("adjustEstimate", "new");
        //            parameters.Add("newEstimate", adjustRemainingBy);
        //            break;
        //    }

        //    var obj = new JObject(
        //        new JProperty(
        //            "timeSpent", timeSpent
        //        )
        //    );

        //    if (workDescription != null)
        //        obj.Add(new JProperty("comment", workDescription));

        //    Request(
        //        String.Format(
        //            "/rest/api/2/issue/{0}/worklog", issueId
        //        ),
        //        parameters,
        //        obj
        //    );
        //}

        public string Request(string url, ObjectInstance parameters, string payload)
        {
            Dictionary<string, string> dictionary = null;

            if (parameters != null)
            {
                dictionary = new Dictionary<string, string>();

                foreach (var parameter in parameters.GetOwnProperties())
                {
                    dictionary[parameter.Key] = parameter.Value?.ToString();
                }
            }

            return Request(url, dictionary, payload);
        }

        private string Request(string url, Dictionary<string, string> parameters, string payload)
        {
            var sb = new StringBuilder();

            sb.Append(_site);

            if (url[0] != '/')
                url = "/" + url;

            sb.Append(url);

            if (parameters != null && parameters.Count > 0)
            {
                sb.Append('?');

                bool hadOne = false;

                foreach (var parameter in parameters)
                {
                    if (hadOne)
                        sb.Append('&');
                    else
                        hadOne = true;

                    sb.Append(Uri.EscapeDataString(parameter.Key));
                    sb.Append('=');
                    sb.Append(parameter.Value);
                }
            }

            var request = (HttpWebRequest)WebRequest.Create(sb.ToString());

            _statusBarProvider.SetStatus("Requesting " + request.RequestUri);

            request.ContentType = "application/json";
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(
                Encoding.UTF8.GetBytes(_userName + ":" + _password)
            ));

            if (payload != null)
            {
                request.Method = "POST";

                using (var stream = request.GetRequestStream())
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(payload);
                }
            }

            try
            {
                using (var response = request.GetResponse())
                {
                    if (!response.ContentType.Contains("json"))
                        return null;

                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Response == null || ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.Forbidden)
                    throw;

                using (var stream = ex.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var result = JToken.Load(jsonReader);

                    sb = new StringBuilder();

                    var errorMessages = result["errorMessages"];

                    if (errorMessages != null)
                    {
                        foreach (var errorMessage in errorMessages)
                        {
                            sb.AppendLine((string)errorMessage);
                        }
                    }

                    var errors = result["errors"];

                    if (errors != null)
                    {
                        foreach (var error in errors)
                        {
                            sb.AppendLine((string)error);
                        }
                    }

                    throw new JiraApiException(sb.ToString().TrimEnd());
                }
            }
            finally
            {
                _statusBarProvider.SetStatus(null);
            }
        }
    }
}
