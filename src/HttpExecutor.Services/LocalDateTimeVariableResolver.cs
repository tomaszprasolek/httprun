﻿using System;
using System.Text.RegularExpressions;
using HttpExecutor.Abstractions;

namespace HttpExecutor.Services
{
    public class LocalDateTimeVariableResolver : IDynamicVariableResolver
    {
        private readonly IDateTimeNowProvider _dateTimeNowProvider;
        private readonly IDatejsToDotNetFormatConverter _datejsToDotNetFormatConverter;
        private const string VariableRegex = "{{\\$localDatetime (rfc1123|iso8601|\".*\"|'.*')(?: (-?\\d*) ([smhdywM]|ms))?}}";

        public LocalDateTimeVariableResolver(IDateTimeNowProvider dateTimeNowProvider, IDatejsToDotNetFormatConverter datejsToDotNetFormatConverter)
        {
            _dateTimeNowProvider = dateTimeNowProvider ?? throw new ArgumentNullException(nameof(dateTimeNowProvider));
            _datejsToDotNetFormatConverter = datejsToDotNetFormatConverter ?? throw new ArgumentNullException(nameof(datejsToDotNetFormatConverter));
        }

        public bool Resolvable(string content)
        {
            var regex = new Regex(VariableRegex);
            return regex.IsMatch(content);
        }

        /*
         * Unit	    Shorthand	Description
           date	    D	Date of Month
           day	    d	Day of Week (Sunday as 0, Saturday as 6)
           month	M	Month (January as 0, December as 11)
           year	    y	Year
           hour	    h	Hour
           minute	m	Minute
           second	s	Second
           millisecond	ms	Millisecond
         */

        public string Resolve(string content)
        {
            var regex = new Regex(VariableRegex);
            var matches = regex.Match(content);

            var now = _dateTimeNowProvider.UtcNow();

            var date = now.ToLocalTime();

            if (matches.Groups.Count > 2 && !string.IsNullOrWhiteSpace(matches.Groups[2].Value))
            {
                var offset = int.Parse(matches.Groups[2].Value);
                var option = matches.Groups[3].Value;

                switch (option)
                {
                    case "ms":
                        date = now.AddMilliseconds(offset);
                        break;
                    case "s":
                        date = now.AddSeconds(offset);
                        break;
                    case "m":
                        date = now.AddMinutes(offset);
                        break;
                    case "h":
                        date = now.AddHours(offset);
                        break;

                    default:
                    case "d":
                        date = now.AddDays(offset);
                        break;

                    case "w":
                        date = now.AddDays(7 * offset);
                        break;

                    case "M":
                        date = now.AddMonths(offset);
                        break;

                    case "y":
                        date = now.AddYears(offset);
                        break;
                }
            }

            if (matches.Groups[1].Value == "iso8601")
            {
                return regex.Replace(content, date.ToString("O"), 1);
            }

            if (matches.Groups[1].Value == "rfc1123")
            {
                return regex.Replace(content, date.ToString("R"), 1);
            }

            string format;

            if (matches.Groups[1].Value.StartsWith('"'))
            {
                format = matches.Groups[1].Value.Trim('"');
            }
            else
            {
                format = matches.Groups[1].Value.Trim('\'');
            }

            return regex.Replace(content, date.ToString(_datejsToDotNetFormatConverter.Convert(format)), 1);
        }
    }
}