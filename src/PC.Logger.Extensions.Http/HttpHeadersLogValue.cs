using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace PC.Logger.Extensions.Http
{
    internal class HttpHeadersLogValue : IReadOnlyList<KeyValuePair<string, string[]>>
    {
        public enum Kind
        {
            Request,
            Response
        }

        private readonly Kind _kind;

        private string _formatted;
        private List<KeyValuePair<string, string[]>> _values;

        public HttpHeadersLogValue(Kind kind, HttpHeaders headers, HttpHeaders contentHeaders)
        {
            _kind = kind;

            Headers = headers;
            ContentHeaders = contentHeaders;
        }

        public HttpHeaders Headers { get; }

        public HttpHeaders ContentHeaders { get; }
        
        private List<KeyValuePair<string, string[]>> Values
        {
            get
            {
                if (_values != null)
                    return _values;

                var values = new List<KeyValuePair<string, string[]>>();
                foreach (var kvp in Headers)
                    values.Add(new KeyValuePair<string, string[]>(kvp.Key, kvp.Value?.ToArray()));

                if (ContentHeaders != null)
                    foreach (var kvp in ContentHeaders)
                        values.Add(new KeyValuePair<string, string[]>(kvp.Key, kvp.Value?.ToArray()));

                _values = values;

                return _values;
            }
        }

        public KeyValuePair<string, string[]> this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException(nameof(index));
                }

                return Values[index];
            }
        }

        public int Count => Values.Count;

        public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        public override string ToString()
        {
            if (_formatted != null)
                return _formatted;

            var builder = new StringBuilder();
            builder.AppendLine($"[{(_kind == Kind.Request ? "Request" : "Response")} Headers]");
            builder.Append(string.Join(";", Values.Select(kvp => $"{kvp.Key}:{string.Join(",", kvp.Value)}")));

            _formatted = builder.ToString();

            return _formatted;
        }

        public Dictionary<string, string> ToDictionary()
        {
            return Values.ToDictionary(kvp => kvp.Key, kvp => string.Join(",", kvp.Value));
        }
    }
}