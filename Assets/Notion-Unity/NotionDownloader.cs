using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace NotionUnity
{
    /// <summary>
    /// Notion database downloader
    /// </summary>
    /// <summary>https://developers.notion.com/docs/getting-started</summary>
    public static class NotionDownloader
    {
        /// <summary>
        /// Download a notion table and return the parsed object
        /// </summary>
        /// <param name="databaseID"></param>
        public static async Task<TableResult> GetTableAsync(string databaseID)
        {
            string route = $"{Notion.API_URL}/databases/{databaseID.Replace("-", "")}/query";

            bool fetchMore = true;
            string cursor = string.Empty;
            TableResult table = new TableResult();

            Debug.Log("Downloading notion database [" + databaseID + "]...");

            while (fetchMore)
            {
                var json = await GetRequest(route, cursor, "POST");
                if (json != null)
                {
                    var pageTable = new TableResult(json);
                    table.Merge(pageTable);

                    // Pagination
                    fetchMore = json["has_more"] != null && ((bool)json["has_more"]);
                    if (fetchMore && json["next_cursor"] != null)
                    {
                        cursor = json["next_cursor"].ToString();
                    }
                }
            }

            Debug.Log("Download completed: " + table.Lines.Length + " found.");

            return table;
        }

        /// <summary>
        /// Make an API call and parse the JSON result on success
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cursor">Start cursor for paginated results</param>
        /// <param name="silent"></param>
        /// <returns></returns>
        private static async Task<JObject> GetRequest(string uri, string cursor, string method, bool silent = false)
        {
            string postData = string.Empty;
            if (string.IsNullOrEmpty(cursor) == false)
            {
                postData += "{" +
                            "   \"start_cursor\": \"" + cursor + "\"" +
                            "}";
            }

            if (!silent) Debug.Log("> Notion API call " + uri + "\n" + postData);

            string textResult = string.Empty;

            using (UnityWebRequest webRequest = new UnityWebRequest(uri, method))
            {
                // ⚠ Unity poor implementation of HTTP client forces us to use a HACK to send non-form JSON with a POST verb
                // https://forum.unity.com/threads/posting-json-through-unitywebrequest.476254/#post-4693241
                if (string.IsNullOrEmpty(postData) == false)
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
                    webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                }

                webRequest.downloadHandler = new DownloadHandlerBuffer();

                webRequest.SetRequestHeader("Authorization", $"Bearer {Notion.API_TOKEN}");
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Notion-Version", Notion.API_VERSION);

                var op = webRequest.SendWebRequest();
                await op;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError("< Error: " + webRequest.error);
                        Debug.LogError(webRequest.downloadHandler.text);
                        break;
                }

                textResult = webRequest.downloadHandler.text;
            }

            // Debug
#pragma warning disable CS0162
            if (Notion.DISPLAY_JSON_RESULT) Debug.Log(textResult);

            try
            {
                var json = JObject.Parse(textResult);
                return json;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return null;
        }

        /// <summary>
        /// Download a notion page METADATA 
        /// </summary>
        /// <param name="urlID"></param>
        /// <param name="silent"></param>
        public static async Task<PageResult> GetPageAsync(string urlID, bool silent = false)
        {
            string route = $"{Notion.API_URL}/pages/{urlID}";

            if (!silent) Debug.Log("Downloading notion page [" + urlID + "]...");

            var json = await GetRequest(route, null, "GET", silent);
            if (json != null)
            {
                return new PageResult(json);
            }

            if (!silent) Debug.Log("Download completed.");

            return null;
        }


        /// <summary>
        /// Download a notion page CONTENT 
        /// </summary>
        /// <param name="pageID"></param>
        public static async Task<PageContentResult> GetPageContentAsync(string pageID)
        {
            string route = $"{Notion.API_URL}/blocks/{pageID}/children";

            Debug.Log("Downloading notion page content [" + pageID + "]...");

            var json = await GetRequest(route, null, "GET");
            if (json != null)
            {
                return new PageContentResult(json);
            }

            Debug.Log("Download completed.");

            return null;
        }
    }

    public class TableCell
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public object Value { get; private set; }
        public string RawJSON { get; private set; }

        public TableCell(JProperty jProperty)
        {
            Name = jProperty.Name;
            Value = ParseLine(jProperty.Value);
            RawJSON = jProperty.ToString();
        }

        public object ParseLine(JToken item)
        {
            if (item["type"] == null) return null;

            Type = item["type"].ToString();
            // Debug.Log("[" + row + "] " + columnName + " " + type + "\n" + item);
            switch (Type)
            {
                case "title":
                    string titleContent = string.Empty;
                    foreach (var t in item["title"])
                    {
                        titleContent += t["plain_text"].ToString();
                    }

                    return titleContent;

                case "text":
                    string textContent = string.Empty;
                    foreach (var t in item["text"])
                    {
                        textContent += t["plain_text"].ToString();
                    }

                    return textContent;

                case "rich_text":
                    string richTextContent = string.Empty;
                    foreach (var t in item["rich_text"])
                    {
                        richTextContent += t["plain_text"].ToString();
                    }

                    return richTextContent;

                case "multi_select":
                    return item["multi_select"].Select(m => m["name"].ToString()).ToArray();

                case "select":
                    if (string.IsNullOrEmpty(item["select"].ToString()) == false)
                    {
                        return item["select"]["name"].ToString();
                    }

                    return string.Empty;

                case "number":
                    string numberString = item["number"].ToString();
                    return string.IsNullOrEmpty(numberString) == false ? float.Parse(numberString) : 0;

                case "date":
                    if (string.IsNullOrEmpty(item["date"].ToString()) == false)
                    {
                        return DateTime.Parse(item["date"]["start"].ToString());
                    }

                    return string.Empty;


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

                case "files":
                    return string.Empty;

                case "relation":
                    return item["relation"].Select(m => m["id"].ToString()).ToArray();

                case "rollup":
                    return string.Empty;

                case "formula":
                    if (item["formula"]["string"] != null)
                    {
                      return item["formula"]["string"].ToString();
                    }
                    else if (item["formula"]["number"] != null)
                    {
                      return item["formula"]["number"].ToString();
                    }
                    return string.Empty;

                default:
                    throw new ArgumentException("Unknown/Unsupported item type: " + item["type"]);
            }
        }

        public override string ToString()
        {
            return $"{Name}({Type}) = {Value}";
        }
    }

    public class TableLine
    {
        public string ID { get; private set; }
        public string URL { get; private set; }
        public TableCell[] Cells { get; private set; }

        public string RawJSON { get; private set; }

        public TableLine(string id, string url, JToken token)
        {
            ID = id;
            URL = url;
            RawJSON = token.ToString();
            Cells = token.Select(t => new TableCell((JProperty)t)).ToArray();
        }

        /// <summary>
        /// Get a cell using it's Notion name
        /// </summary>
        /// <param name="cellName"></param>
        /// <returns></returns>
        public TableCell Get(string cellName)
        {
            var cell = Cells.FirstOrDefault(c => c.Name == cellName);
            if (cell == null)
            {
#if !PROMPTER_VALIDATOR
                Debug.LogError("Missing cell [" + cellName + "]");
#endif
            }

            return cell;
        }

        /// <summary>
        /// Get a cell's value using it's Notion name
        /// </summary>
        /// <param name="cellName"></param>
        /// <returns></returns>
        public object GetValue(string cellName)
        {
            var cell = Get(cellName);
            if (cell != null) return cell.Value;
            return null;
        }

        public string GetValueString(string cellName)
        {
            var value = GetValue(cellName);
            if (value != null) return value.ToString();
            else return string.Empty;
        }

        public int GetValueInt(string cellName)
        {
            var value = GetValue(cellName);
            if (value != null) return int.Parse(value.ToString());
            else return 0;
        }

        public bool GetValueBool(string cellName)
        {
            var value = GetValue(cellName);
            if (value != null) return (bool)value;
            else return false;
        }

        public float GetValueFloat(string cellName)
        {
            var value = GetValue(cellName);
            if (value != null) return float.Parse(value.ToString());
            else return 0;
        }
    }

    public class TableResult
    {
        public TableLine[] Lines { get; private set; }

        public TableResult()
        {
            Lines = new TableLine[0];
        }

        public TableResult(JObject json)
        {
            var objectType = json.GetValue("object");
            if (objectType == null || objectType.ToString() != "list")
            {
                throw new ArgumentException("API Result is not a list");
            }

            var results = json["results"];
            Lines = results.Select(p => new TableLine(p["id"].ToString(), p["url"].ToString(), p["properties"])).ToArray();
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

        public void Merge(TableResult otherTable)
        {
            Lines = Lines.Union(otherTable.Lines).ToArray();
        }
    }

    public class PageResult
    {
        public TableLine Line { get; private set; }

        public PageResult(JObject p)
        {
            var objectType = p.GetValue("object");
            if (objectType == null || objectType.ToString() != "page")
            {
                throw new ArgumentException("API Result is not a page");
            }

            Line = new TableLine(p["id"].ToString(), p["url"].ToString(), p["properties"]);
        }
    }

    public class PageContentResult
    {
        public PageBlock[] Blocks { get; private set; }

        public string Value
        {
            get
            {
                StringBuilder s = new StringBuilder();
                foreach (var block in Blocks)
                {
                    string t = block.ToString();
                    if (t.EndsWith("\n") == false)
                    {
                      t += "\n";
                    }
                    s.Append(t);
                }

                return s.ToString();
            }
        }

        public PageContentResult(JObject json)
        {
            var objectType = json.GetValue("object");
            if (objectType == null || objectType.ToString() != "list")
            {
                throw new ArgumentException("API Result is not a list");
            }

            var results = json["results"];

            Blocks = results.Select(p => new PageBlock(p)).ToArray();
        }
    }

    public class PageBlock
  {
        public string ID { get; private set; }
        public string Type { get; private set; }
        public string Value { get; private set; }
        public string RawJSON { get; private set; }

        public PageBlock(JToken json)
        {
            Value = ParseLine(json);
            RawJSON = json.ToString();
        }

        public string ParseLine(JToken item)
        {
            ID = item["id"].ToString();

            if (item["type"] == null) return null;

            Type = item["type"].ToString();

            try
            {
                string r = string.Empty;
                if (item[Type]["text"] != null)
                {
                  foreach (var t in item[Type]["text"])
                  {
                    r += t["plain_text"];
                  }
                }

                return r;
            }
            catch (Exception e)
            {
                throw new ArgumentException("Unknown/Unsupported item type: " + item["type"] + ". " + e);
            }
        }

        public override string ToString()
        {
            return Value;
        }
    }
}