using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace NotionUnity
{
    /// <summary>
    /// Notion database downloader
    /// </summary>
    /// <summary>https://developers.notion.com/docs/getting-started</summary>
    public class NotionDownloader : MonoBehaviour
    {
        private const string API_URL = "https://api.notion.com/v1";

        public string token;
        public string databaseID;
        public bool displayJSONResult = false;

        private async void Start()
        {
            var r = await GetTableAsync();

            for (int i = 0; i < r.Lines.Length; i++)
            {
                var line = r.Lines[i];

                Debug.Log(i + " ------------- ");

                foreach (var cell in line.Cells)
                {
                    Debug.Log(cell.Name + " (" + cell.Type + ") =" + cell.Value);
                }
            }
        }

        /// <summary>
        /// Download a table asynchronously
        /// </summary>
        /// <returns></returns>
        public async Task<TableResult> GetTableAsync()
        {
            TaskCompletionSource<TableResult> tcs = new TaskCompletionSource<TableResult>();

            GetTable((result) => tcs.SetResult(result));

            var table = await tcs.Task;
            return table;
        }

        /// <summary>
        /// Download a table
        /// </summary>
        /// <param name="callback"></param>
        public void GetTable(Action<TableResult> callback = null)
        {
            string route = $"{API_URL}/databases/{databaseID.Replace("-", "")}/query";

            StartCoroutine(GetRequest(route, (json) =>
            {
                if (json != null)
                {
                    var table = new TableResult(json);
                    callback?.Invoke(table);
                }
            }));
        }

        /// <summary>
        /// Make an API call and parse the JSON result on success
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private IEnumerator GetRequest(string uri, Action<JObject> callback)
        {
            Debug.Log("> Notion API call " + uri);
            using (UnityWebRequest webRequest = UnityWebRequest.Post(uri, string.Empty))
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {token}");
                webRequest.SetRequestHeader("Content-Type", "application/json");

                yield return webRequest.SendWebRequest();

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError("< Error: " + webRequest.error);
                        Debug.LogError(webRequest.downloadHandler.text);
                        break;
                }

                // Debug
                if (displayJSONResult) Debug.Log(webRequest.downloadHandler.text);

                try
                {
                    var json = JObject.Parse(webRequest.downloadHandler.text);
                    callback?.Invoke(json);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    callback?.Invoke(null);
                }
            }
        }
    }

    public class TableCell
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public object Value { get; set; }

        public TableCell(JProperty jProperty)
        {
            Name = jProperty.Name;
            Value = ParseLine(jProperty.Value);
        }

        public object ParseLine(JToken item)
        {
            if (item["type"] == null) return null;

            Type = item["type"].ToString();
            // Debug.Log("[" + row + "] " + columnName + " " + type + "\n" + item);
            switch (Type)
            {
                case "title":
                    return item["title"].Any() ? item["title"][0]["plain_text"].ToString() : string.Empty;

                case "text":
                    return item["text"].Any() ? item["text"][0]["text"]["content"].ToString() : string.Empty;

                case "multi_select":
                    return item["multi_select"].Select(m => m.ToString()).ToArray();

                case "select":
                    return item["select"]["name"].ToString();

                case "number":
                    return int.Parse(item["number"].ToString());

                case "date":
                    return DateTime.Parse(item["date"]["start"].ToString());

                case "checkbox":
                    return bool.Parse(item["checkbox"].ToString());

                case "people":
                    return item["people"].Select(m => m.ToString()).ToArray();

                case "url":
                    return item["url"].ToString();

                case "email":
                    return item["email"].ToString();

                case "phone_number":
                    return item["phone_number"].ToString();

                default:
                    throw new ArgumentException("Unknown/Unsupported item type: " + item["type"]);
            }
        }
    }

    public class TableLine
    {
        public TableCell[] Cells { get; set; }

        public TableLine(JToken token)
        {
            Cells = token.Select(t => new TableCell((JProperty) t)).ToArray();
        }
    }

    public class TableResult
    {
        public TableLine[] Lines { get; private set; }

        public TableResult(JObject json)
        {
            var objectType = json.GetValue("object");
            if (objectType == null || objectType.ToString() != "list")
            {
                throw new ArgumentException("API Result is not a list");
            }

            var results = json["results"];
            Lines = results.Reverse().Select(p => new TableLine(p["properties"])).ToArray();
        }

        /// <summary>
        /// Return the value content of a given column for a given line 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public object Value(int row, string columnName)
        {
            if (row >= Lines.Length)
            {
                throw new ArgumentException("Line number is greater than line count!");
            }

            return Lines[row].Cells.First(l => l.Name == columnName).Value;
        }
    }
}